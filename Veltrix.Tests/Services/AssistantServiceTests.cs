using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Assistant;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Assistant;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Tests.Services;

/// <summary>The AI assistant service, with a fake AI (Moq) instead of the n8n workflow.</summary>
public class AssistantServiceTests : IDisposable
{
    private readonly ServiceTestContext _ctx = new();
    private readonly Mock<IAssistantClient> _ai = new();
    private readonly Mock<ILimitsService> _limits = new();
    private readonly List<AssistantRequest> _sent = new();

    public AssistantServiceTests()
    {
        _ai.Setup(a => a.AskAsync(It.IsAny<AssistantRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AssistantRequest, CancellationToken>((request, _) => _sent.Add(request))
            .ReturnsAsync("Cash out before the crash.");
        CanPlayNow(true);
    }

    public void Dispose() => _ctx.Dispose();

    private AssistantService Service(int historyMessages = 10) => new(
        _ctx.Db,
        _ctx.CurrentUser.Object,
        _ai.Object,
        _limits.Object,
        Options.Create(new AssistantOptions { HistoryMessages = historyMessages }),
        _ctx.Time,
        NullLogger<AssistantService>.Instance);

    private void CanPlayNow(bool canPlay, bool selfExcluded = false) =>
        _limits.Setup(l => l.GetMyLimitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerLimitsDto { CanPlayNow = canPlay, IsSelfExcluded = selfExcluded });

    private static AskAssistantRequest Ask(string message) => new() { Message = message };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Status_TellsWhetherTheAiIsSetUp(bool configured)
    {
        _ai.Setup(a => a.IsConfigured).Returns(configured);

        Assert.Equal(configured, Service().GetStatus().Configured);
    }

    [Fact]
    public async Task Ask_SavesTheQuestionAndTheAnswer()
    {
        var player = _ctx.AddUser("curious");
        _ctx.ActAs(player);

        var reply = await Service().AskAsync(Ask("  How does Crash work?  "));

        Assert.Equal(AssistantMessageRole.User, reply.Question.Role);
        Assert.Equal("How does Crash work?", reply.Question.Text); // trimmed
        Assert.Equal(AssistantMessageRole.Assistant, reply.Answer.Role);
        Assert.Equal("Cash out before the crash.", reply.Answer.Text);

        using var db = _ctx.Fresh();
        Assert.Equal(2, db.AssistantMessages.Count(m => m.UserId == player.Id));
    }

    [Fact]
    public async Task Ask_SendsTheRules_TheLanguage_AndTheRecentConversation()
    {
        var player = _ctx.AddUser("follow_up");
        _ctx.ActAs(player);

        await Service().AskAsync(Ask("What is provably fair?"));
        await Service().AskAsync(Ask("And how do I check a round?"));

        var second = _sent[1];
        Assert.Equal($"veltrix-user-{player.Id}", second.SessionId);
        Assert.Equal("ka", second.Language); // Georgian is the default UI language
        Assert.Contains(AssistantPrompts.NoStrategyRule, second.SystemPrompt);
        Assert.Contains(AssistantPrompts.NoInvitationRule, second.SystemPrompt);
        Assert.Equal("And how do I check a round?", second.Message);

        // The first question and its answer go along, oldest first, so the follow-up makes sense
        Assert.Equal(new[] { "user", "assistant" }, second.History.Select(t => t.Role));
        Assert.Equal("What is provably fair?", second.History[0].Text);
    }

    [Fact]
    public async Task Ask_UsesTheLanguageFromTheSettings()
    {
        var player = _ctx.AddUser("english_speaker");
        _ctx.Db.UserPreferences.Single(p => p.UserId == player.Id).Language = "en";
        _ctx.Db.SaveChanges();
        _ctx.ActAs(player);

        await Service().AskAsync(Ask("Hi"));

        Assert.Equal("en", _sent.Single().Language);
        Assert.Contains("Answer in English.", _sent.Single().SystemPrompt);
    }

    [Fact]
    public async Task Ask_SendsOnlyTheConfiguredNumberOfEarlierMessages()
    {
        var player = _ctx.AddUser("chatty");
        _ctx.ActAs(player);
        for (var i = 0; i < 3; i++)
            await Service(historyMessages: 2).AskAsync(Ask($"Question {i}"));

        // Before the third question there were 4 messages; only the latest 2 (question 1 and its answer) are sent
        var third = _sent[2];
        Assert.Equal(2, third.History.Count);
        Assert.Equal("Question 1", third.History[0].Text);
    }

    [Fact]
    public async Task Ask_WhileBettingIsPaused_TellsTheAiNotToInvitePlay()
    {
        // Responsible play: a player who cannot bet right now sees nothing that asks them to play, from the AI either
        var player = _ctx.AddUser("paused");
        _ctx.ActAs(player);
        CanPlayNow(false, selfExcluded: true);

        await Service().AskAsync(Ask("When can I play again?"));

        Assert.Contains(AssistantPrompts.BettingPausedRule, _sent.Single().SystemPrompt);
        Assert.Contains(AssistantPrompts.SelfExcludedNote, _sent.Single().SystemPrompt);
    }

    [Fact]
    public async Task Ask_WhenThePlayerCanPlay_HasNoPausedRule()
    {
        var player = _ctx.AddUser("free");
        _ctx.ActAs(player);

        await Service().AskAsync(Ask("Hello"));

        Assert.DoesNotContain(AssistantPrompts.BettingPausedRule, _sent.Single().SystemPrompt);
    }

    [Fact]
    public async Task Ask_WhenTheAiCannotAnswer_SavesNothing()
    {
        var player = _ctx.AddUser("unlucky");
        _ctx.ActAs(player);
        _ai.Setup(a => a.AskAsync(It.IsAny<AssistantRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException(N8nAssistantClient.UnavailableMessage));

        var error = await Assert.ThrowsAsync<ServiceUnavailableException>(() => Service().AskAsync(Ask("Anyone there?")));

        Assert.Equal(503, error.StatusCode);
        using var db = _ctx.Fresh();
        Assert.Empty(db.AssistantMessages);
    }

    [Fact]
    public async Task History_IsMineOnly_OldestFirst()
    {
        var me = _ctx.AddUser("me_player");
        var other = _ctx.AddUser("other_player");
        _ctx.Db.AssistantMessages.AddRange(
            AssistantMessage.Question(other.Id, "Not yours", ServiceTestContext.Now),
            AssistantMessage.Question(me.Id, "Second", ServiceTestContext.Now.AddMinutes(2)),
            AssistantMessage.Question(me.Id, "First", ServiceTestContext.Now.AddMinutes(1)));
        _ctx.Db.SaveChanges();
        _ctx.ActAs(me);

        var history = await Service().GetMyHistoryAsync();

        Assert.Equal(new[] { "First", "Second" }, history.Select(m => m.Text));
    }

    [Fact]
    public async Task Clear_DeletesOnlyMyConversation()
    {
        var me = _ctx.AddUser("clearer");
        var other = _ctx.AddUser("keeper");
        _ctx.Db.AssistantMessages.AddRange(
            AssistantMessage.Question(me.Id, "Mine", ServiceTestContext.Now),
            AssistantMessage.Answer(me.Id, "Answer", ServiceTestContext.Now),
            AssistantMessage.Question(other.Id, "Theirs", ServiceTestContext.Now));
        _ctx.Db.SaveChanges();
        _ctx.ActAs(me);

        var deleted = await Service().ClearMyHistoryAsync();

        Assert.Equal(2, deleted);
        using var db = _ctx.Fresh();
        Assert.Equal("Theirs", db.AssistantMessages.Single().Text);
    }

    [Fact]
    public void A_very_long_answer_is_cut_to_the_column_size()
    {
        var answer = AssistantMessage.Answer(1, new string('a', AssistantMessage.MaxTextLength + 50), ServiceTestContext.Now);

        Assert.Equal(AssistantMessage.MaxTextLength, answer.Text.Length);
    }
}
