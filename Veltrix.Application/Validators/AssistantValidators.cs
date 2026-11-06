using FluentValidation;
using Veltrix.Application.DTOs.Assistant;
using Veltrix.Domain.Entities;

namespace Veltrix.Application.Validators;

public sealed class AskAssistantRequestValidator : AbstractValidator<AskAssistantRequest>
{
    public AskAssistantRequestValidator()
    {
        RuleFor(x => x.Message)
            .Must(m => !string.IsNullOrWhiteSpace(m)).WithMessage("Write a question first.")
            .MaximumLength(AssistantMessage.MaxQuestionLength)
            .WithMessage($"A question can be at most {AssistantMessage.MaxQuestionLength} characters.");
    }
}
