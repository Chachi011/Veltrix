namespace Veltrix.Infrastructure.Assistant;

/// <summary>
/// The "Assistant" section of appsettings. The webhook URL and the secret are machine-specific, so set them in
/// user-secrets (dotnet user-secrets set "Assistant:WebhookUrl" "..."), never in Git.
/// </summary>
public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// <summary>The n8n Webhook node's Production URL, e.g. http://localhost:5678/webhook/veltrix-assistant. Empty = assistant off.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Sent as the X-Veltrix-Secret header; the Webhook node's Header Auth checks it, so only Veltrix can call the workflow.</summary>
    public string? Secret { get; set; }

    /// <summary>How long to wait for the AI (5–120 seconds).</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>How many earlier messages go with a question, so the AI understands follow-ups (0–20).</summary>
    public int HistoryMessages { get; set; } = 10;
}
