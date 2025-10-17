using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service.Library;

public class Repository
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Full_Name { get; set; } = string.Empty;
    public bool Private { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Html_Url { get; set; } = string.Empty;
    public DateTime Created_At { get; set; }
    public DateTime Updated_At { get; set; }
    public string Language { get; set; } = string.Empty;
    public int Stargazers_Count { get; set; }
    public int Forks_Count { get; set; }

    public override string ToString()
    {
        return Name + (Private ? " (Private)" : " (Public)") +
               $"Created At: {Created_At}\n" +
               $"Updated At: {Updated_At}";
    }
}


public class GitHubApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;

    public GitHubApiClient(string apiToken)
    {
        _apiToken = apiToken;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        
        // Set required headers
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("MyApp", "1.0"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiToken);
    }

    // Example: Get authenticated user info
    public async Task<string> GetAuthenticatedUserAsync()
    {
        var response = await _httpClient.GetAsync("user");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    // Example: Get a repository
    public async Task<string> GetRepositoryAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"repos/{owner}/{repo}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    // Example: Create a repository issue
    public async Task<string> CreateIssueAsync(string owner, string repo, 
        string title, string body)
    {
        var issueData = new
        {
            title = title,
            body = body
        };

        var json = JsonSerializer.Serialize(issueData);
        var content = new StringContent(json, 
            System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"repos/{owner}/{repo}/issues", content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<List<Repository>> GetAllMyRepositoriesAsync()
    {
        var allRepos = new List<Repository>();
        int page = 1;
        int perPage = 100;

        while (true)
        {
            var url = $"user/repos?type=all&sort=updated&per_page={perPage}&page={page}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var repos = JsonSerializer.Deserialize<List<Repository>>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (repos == null || repos.Count == 0)
                break;

            allRepos.AddRange(repos);
            
            if (repos.Count < perPage)
                break;

            page++;
        }

        return allRepos;
    }

}
