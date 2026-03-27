using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using NoteBuddy.Models;

namespace NoteBuddy.Services;

/// <summary>
/// Singleton service that provides AI chat capabilities using Azure OpenAI via
/// the Microsoft.Extensions.AI IChatClient abstraction. Authenticates with
/// DefaultAzureCredential (interactive browser login — no API keys).
/// </summary>
public class AiService
{
    private IChatClient? _chatClient;
    private AiSettings _settings = new();
    private readonly object _lock = new();

    /// <summary>Whether the service has been configured with valid settings.</summary>
    public bool IsConfigured => _settings.IsConfigured && _chatClient != null;

    /// <summary>The current AI settings.</summary>
    public AiSettings Settings => _settings;

    /// <summary>
    /// Configures (or reconfigures) the AI client with the given settings.
    /// Creates an AzureOpenAIClient with DefaultAzureCredential and wraps it as IChatClient.
    /// </summary>
    public void Configure(AiSettings settings)
    {
        lock (_lock)
        {
            _settings = settings;

            if (!settings.IsConfigured)
            {
                _chatClient = null;
                return;
            }

            var azureClient = new AzureOpenAIClient(
                new Uri(settings.Endpoint!),
                new DefaultAzureCredential());

            _chatClient = azureClient
                .GetChatClient(settings.DeploymentName!)
                .AsIChatClient();
        }
    }

    /// <summary>
    /// Sends a message to the AI and returns the response text.
    /// Throws InvalidOperationException if not configured.
    /// </summary>
    public async Task<string> GetResponseAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
            throw new InvalidOperationException("AI is not configured. Please set up your Azure OpenAI endpoint in Settings.");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        return response.Text ?? string.Empty;
    }

    /// <summary>
    /// Tests the AI connection by sending a simple ping message.
    /// Returns null on success, or the error message on failure.
    /// </summary>
    public async Task<string?> TestConnectionAsync()
    {
        if (_chatClient == null)
            return "AI is not configured.";

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "Respond with exactly: OK")
            };

            var response = await _chatClient.GetResponseAsync(messages);
            return null; // success
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
