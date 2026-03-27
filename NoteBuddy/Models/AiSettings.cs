namespace NoteBuddy.Models;

/// <summary>
/// Stores the user's AI configuration for connecting to an Azure OpenAI (Foundry) deployment.
/// Persisted alongside other corkboard settings in corkboard.json.
/// </summary>
public class AiSettings
{
    /// <summary>Azure OpenAI endpoint URL (e.g., https://myresource.openai.azure.com/).</summary>
    public string? Endpoint { get; set; }

    /// <summary>Deployed model name / deployment ID (e.g., gpt-4o).</summary>
    public string? DeploymentName { get; set; }

    /// <summary>Returns true when both endpoint and deployment name are provided.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(DeploymentName);
}
