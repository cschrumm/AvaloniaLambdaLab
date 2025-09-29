namespace WebApplication1;

// API Key validation service interface
public interface IApiKeyValidationService
{
    Task<bool> IsValidApiKeyAsync(string apiKey);

    void AddKey(string key);
}

// API Key validation service implementation
public class ApiKeyValidationService : IApiKeyValidationService
{
    private readonly IConfiguration _configuration;
    private readonly HashSet<string> _validApiKeys;

    public ApiKeyValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Load valid API keys from configuration
        var apiKeys = _configuration.GetSection("ApiKeys").Get<string[]>() ?? new string[0];
        _validApiKeys = new HashSet<string>(apiKeys, StringComparer.OrdinalIgnoreCase);
        
        // If no keys configured, add a default one for development
        if (_validApiKeys.Count == 0)
        {
            _validApiKeys.Add(Environment.GetEnvironmentVariable("myappkey") ?? "app1234");//  "your-default-api-key-here");
        }
    }

    public async Task<bool> IsValidApiKeyAsync(string apiKey)
    {
        // For simple implementation, just check against the configured keys
        // In production, you might want to check against a database or external service
        await Task.CompletedTask; // Placeholder for async operations if needed
        
        return _validApiKeys.Contains(apiKey);
    }

    public void AddKey(string key)
    {
        _validApiKeys.Add(key);
    }
}