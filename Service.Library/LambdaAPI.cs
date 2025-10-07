namespace Service.Library;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// Models
public class Instance
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("ip")]
    public string Ip { get; set; } = String.Empty;

    [JsonPropertyName("private_ip")]
    public string PrivateIp { get; set; } = String.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = String.Empty;

    [JsonPropertyName("ssh_key_names")]
    public List<string>? SshKeyNames { get; set; }

    [JsonPropertyName("file_system_names")]
    public List<string>? FileSystemNames { get; set; }

    [JsonPropertyName("file_system_mounts")]
    public List<FilesystemMountEntry>? FileSystemMounts { get; set; }

    [JsonPropertyName("region")]
    public required Region Region { get; set; }

    [JsonPropertyName("instance_type")]
    public required InstanceType InstanceType { get; set; }

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = String.Empty;

    [JsonPropertyName("jupyter_token")]
    public string JupyterToken { get; set; } = String.Empty;

    [JsonPropertyName("jupyter_url")]
    public string JupyterUrl { get; set; } = String.Empty;

    [JsonPropertyName("actions")] public InstanceActionAvailability Actions { get; set; } = null!;

    [JsonPropertyName("tags")]
    public List<TagEntry>? Tags { get; set; }

    [JsonPropertyName("firewall_rulesets")]
    public List<FirewallRulesetEntry>? FirewallRulesets { get; set; }

    public override string ToString()
    {
        return $"{Name} - {Status} - {InstanceType.Name} - {Region.Name}";
    }
}

public class Region
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = String.Empty;
    
    // override ToString for easier display
    public override string ToString()
    {
        return Name ?? "";
    }
}

public class InstanceType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = String.Empty;

    [JsonPropertyName("gpu_description")]
    public string GpuDescription { get; set; }  = String.Empty;

    [JsonPropertyName("price_cents_per_hour")]
    public int PriceCentsPerHour { get; set; }

    [JsonPropertyName("specs")]
    public InstanceTypeSpecs Specs { get; set; }
}

public class InstanceTypeSpecs
{
    [JsonPropertyName("vcpus")]
    public int VCpus { get; set; }

    [JsonPropertyName("memory_gib")]
    public int MemoryGib { get; set; }

    [JsonPropertyName("storage_gib")]
    public int StorageGib { get; set; }

    [JsonPropertyName("gpus")]
    public int Gpus { get; set; }
}

public class FilesystemMountEntry
{
    [JsonPropertyName("mount_point")]
    public string MountPoint { get; set; } = String.Empty;

    [JsonPropertyName("file_system_id")]
    public string FileSystemId { get; set; } = String.Empty;
}

public class InstanceActionAvailability
{
    [JsonPropertyName("migrate")]
    public InstanceActionAvailabilityDetails? Migrate { get; set; }

    [JsonPropertyName("rebuild")]
    public InstanceActionAvailabilityDetails? Rebuild { get; set; }

    [JsonPropertyName("restart")]
    public InstanceActionAvailabilityDetails? Restart { get; set; }

    [JsonPropertyName("cold_reboot")]
    public InstanceActionAvailabilityDetails? ColdReboot { get; set; }

    [JsonPropertyName("terminate")]
    public InstanceActionAvailabilityDetails? Terminate { get; set; }
}

public class InstanceActionAvailabilityDetails
{
    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonPropertyName("reason_code")]
    public string ReasonCode { get; set; } = String.Empty;

    [JsonPropertyName("reason_description")]
    public string ReasonDescription { get; set; } = String.Empty;
}

public class TagEntry
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = String.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = String.Empty;
}

public class FirewallRulesetEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;
}

public class SSHKey
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = String.Empty;

    public override string ToString()
    {
        return Name ?? "";
    }
}

public class GeneratedSSHKey : SSHKey
{
    [JsonPropertyName("private_key")]
    public string PrivateKey { get; set; } = String.Empty;
}

public class Filesystem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("mount_point")]
    public string MountPoint { get; set; } = String.Empty;

    [JsonPropertyName("workspace_id")]
    public string WorkspaceId { get; set; } = String.Empty;

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.MinValue;

    [JsonPropertyName("created_by")]
    public User CreatedBy { get; set; } = new User();

    [JsonPropertyName("is_in_use")]
    public bool IsInUse { get; set; } = false;

    [JsonPropertyName("region")] public Region Region { get; set; } = null!;

    [JsonPropertyName("bytes_used")] public long? BytesUsed { get; set; } = 0;
    
    // override ToString for easier display
    public override string ToString()
    {
        return (Name ?? "") + " (" + Id + ")" + Region.Name ?? "";
    }
}

public class User
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = String.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = String.Empty;
}

public class Image
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = String.Empty;

    [JsonPropertyName("family")]
    public string Family { get; set; } = String.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = String.Empty;

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = String.Empty;

    [JsonPropertyName("region")] public Region Region { get; set; } = new();
    
    public override string ToString()
    {
        return (Name ?? "" + (Description ?? "") + " " + (Family ?? "") + " " + (Version ?? "") + " " + (Architecture ?? ""));
    }
}

public class FirewallRule
{
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = String.Empty;

    [JsonPropertyName("port_range")]
    public List<int>? PortRange { get; set; }

    [JsonPropertyName("source_network")]
    public string SourceNetwork { get; set; } = String.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = String.Empty;
}

public class FirewallRuleset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("region")]
    public Region Region { get; set; }

    [JsonPropertyName("rules")]
    public List<FirewallRule> Rules { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("instance_ids")]
    public List<string> InstanceIds { get; set; }
}

public class ImageId
{
    public string Id { get; set; } = String.Empty;
}

// Request Models
public class InstanceLaunchRequest
{
    [JsonPropertyName("region_name")]
    public string RegionName { get; set; } = String.Empty;

    [JsonPropertyName("instance_type_name")]
    public string InstanceTypeName { get; set; } = String.Empty;

    [JsonPropertyName("ssh_key_names")]
    public List<string> SshKeyNames { get; set; } = new();

    [JsonPropertyName("file_system_names")]
    public List<string> FileSystemNames { get; set; } = null!;

    [JsonPropertyName("file_system_mounts")]
    public List<RequestedFilesystemMountEntry>? FileSystemMounts { get; set; }

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("image")] public ImageId Image { get; set; } = null!;

    [JsonPropertyName("user_data")]
    public string? UserData { get; set; }

    [JsonPropertyName("tags")]
    public List<RequestedTagEntry>? Tags { get; set; }

    [JsonPropertyName("firewall_rulesets")]
    public List<FirewallRulesetEntry>? FirewallRulesets { get; set; }
}

public class RequestedFilesystemMountEntry
{
    [JsonPropertyName("mount_point")]
    public string MountPoint { get; set; } = String.Empty;

    [JsonPropertyName("file_system_id")]
    public string FileSystemId { get; set; } = String.Empty;
}

public class RequestedTagEntry
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = String.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = String.Empty;
}

public class InstanceRestartRequest
{
    [JsonPropertyName("instance_ids")]
    public List<string>? InstanceIds { get; set; }
}

public class InstanceTerminateRequest
{
    [JsonPropertyName("instance_ids")]
    public List<string>? InstanceIds { get; set; }
}

public class AddSSHKeyRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = String.Empty;
}

public class FilesystemCreateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("region")]
    public string Region { get; set; } = String.Empty;
}

// Response Models
public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}

public class ApiError
{
    [JsonPropertyName("error")]
    public ErrorDetails Error { get; set; }
}

public class ErrorDetails
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = String.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = String.Empty;

    [JsonPropertyName("suggestion")]
    public string Suggestion { get; set; } = String.Empty;
}

public class InstanceLaunchResponse
{
    [JsonPropertyName("instance_ids")]
    public List<string> InstanceIds { get; set; }
}

public class InstanceRestartResponse
{
    [JsonPropertyName("restarted_instances")]
    public List<Instance> RestartedInstances { get; set; }
}

public class InstanceTerminateResponse
{
    [JsonPropertyName("terminated_instances")]
    public List<Instance> TerminatedInstances { get; set; }
}

// Custom Exceptions
public class LambdaCloudApiException : Exception
{
    public string Code { get; }
    public string Suggestion { get; }

    public LambdaCloudApiException(string code, string message, string suggestion = null) 
        : base(message)
    {
        Code = code;
        Suggestion = suggestion;
    }
}

// Main API Client
public class LambdaCloudClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string BaseUrl = "https://cloud.lambda.ai/api/v1";

    public LambdaCloudClient(string apiKey, HttpClient httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(BaseUrl);
        
        
        // Set up authentication header
        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:"));
        _httpClient.DefaultRequestVersion = new Version(1, 0);
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        // application/json
        _httpClient.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
        _httpClient.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("ISO-8859-1"));
        //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
        
        //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    // Instance Methods
    public async Task<List<Instance>> ListInstancesAsync(string clusterId = null)
    {
        var url = "/instances";
        if (!string.IsNullOrEmpty(clusterId))
        {
            url += $"?cluster_id={clusterId}";
        }

        var response = await GetAsync<List<Instance>>(url);
        return response;
    }

    public async Task<Instance> GetInstanceAsync(string instanceId)
    {
        return await GetAsync<Instance>($"/instances/{instanceId}");
    }

    public async Task<Instance> UpdateInstanceAsync(string instanceId, string name)
    {
        var request = new { name };
        return await PostAsync<Instance>($"/instances/{instanceId}", request);
    }

    public async Task<Dictionary<string, InstanceTypesItem>> ListInstanceTypesAsync()
    {
        return await GetAsync<Dictionary<string, InstanceTypesItem>>("/instance-types");
    }

    public async Task<InstanceLaunchResponse> LaunchInstanceAsync(InstanceLaunchRequest request)
    {
        return await PostAsync<InstanceLaunchResponse>("/instance-operations/launch", request);
    }

    public async Task<InstanceRestartResponse> RestartInstancesAsync(InstanceRestartRequest request)
    {
        return await PostAsync<InstanceRestartResponse>("/instance-operations/restart", request);
    }

    public async Task<InstanceTerminateResponse> TerminateInstancesAsync(InstanceTerminateRequest request)
    {
        return await PostAsync<InstanceTerminateResponse>("/instance-operations/terminate", request);
    }

    // SSH Key Methods
    public async Task<List<SSHKey>> ListSSHKeysAsync()
    {
        return await GetAsync<List<SSHKey>>("/ssh-keys");
    }

    public async Task<object> AddSSHKeyAsync(AddSSHKeyRequest request)
    {
        // Returns either SSHKey or GeneratedSSHKey depending on whether public_key was provided
        return await PostAsync<object>("/ssh-keys", request);
    }

    public async Task DeleteSSHKeyAsync(string sshKeyId)
    {
        await DeleteAsync($"/ssh-keys/{sshKeyId}");
    }

    // Filesystem Methods
    public async Task<List<Filesystem>> ListFilesystemsAsync()
    {
        return await GetAsync<List<Filesystem>>("/file-systems");
    }

    public async Task<Filesystem> CreateFilesystemAsync(FilesystemCreateRequest request)
    {
        return await PostAsync<Filesystem>("/filesystems", request);
    }

    public async Task DeleteFilesystemAsync(string filesystemId)
    {
        await DeleteAsync($"/filesystems/{filesystemId}");
    }

    // Image Methods
    public async Task<List<Image>> ListImagesAsync()
    {
        return await GetAsync<List<Image>>("/images");
    }

    // Firewall Methods
    public async Task<List<FirewallRule>> ListFirewallRulesAsync()
    {
        return await GetAsync<List<FirewallRule>>("/firewall-rules");
    }

    public async Task<List<FirewallRule>> SetFirewallRulesAsync(List<FirewallRule> rules)
    {
        var request = new { data = rules };
        return await PutAsync<List<FirewallRule>>("/firewall-rules", request);
    }

    public async Task<List<FirewallRuleset>> ListFirewallRulesetsAsync()
    {
        return await GetAsync<List<FirewallRuleset>>("/firewall-rulesets");
    }

    public async Task<FirewallRuleset> CreateFirewallRulesetAsync(string name, string region, List<FirewallRule> rules)
    {
        var request = new { name, region, rules };
        return await PostAsync<FirewallRuleset>("/firewall-rulesets", request);
    }

    public async Task<FirewallRuleset> GetFirewallRulesetAsync(string rulesetId)
    {
        return await GetAsync<FirewallRuleset>($"/firewall-rulesets/{rulesetId}");
    }

    public async Task<FirewallRuleset> UpdateFirewallRulesetAsync(string rulesetId, string name = null, List<FirewallRule> rules = null)
    {
        var request = new Dictionary<string, object>();
        if (name != null) request["name"] = name;
        if (rules != null) request["rules"] = rules;

        return await PatchAsync<FirewallRuleset>($"/firewall-rulesets/{rulesetId}", request);
    }

    public async Task DeleteFirewallRulesetAsync(string rulesetId)
    {
        await DeleteAsync($"/firewall-rulesets/{rulesetId}");
    }

    // Helper Methods
    private async Task<T> GetAsync<T>(string endpoint)
    {
        // Glaude Foo
        var response = await _httpClient.GetAsync(BaseUrl + endpoint);

        var rq = response.RequestMessage;
       
        return await HandleResponseAsync<T>(response);
    }

    private async Task<T> PostAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        // Claude Foo
        var response = await _httpClient.PostAsync(BaseUrl + endpoint, content);
        return await HandleResponseAsync<T>(response);
    }

    private async Task<T> PutAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        // Claude Foo
        var response = await _httpClient.PutAsync(BaseUrl + endpoint, content);
        return await HandleResponseAsync<T>(response);
    }

    private async Task<T> PatchAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        // Claude Foo
        var request = new HttpRequestMessage(HttpMethod.Patch, BaseUrl + endpoint) { Content = content };
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    private async Task DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        await HandleResponseAsync<object>(response);
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object) && string.IsNullOrEmpty(content))
                return default(T)!;

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            return apiResponse.Data;
        }
        else
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiError>(content, _jsonOptions);
                throw new LambdaCloudApiException(
                    errorResponse.Error.Code,
                    errorResponse.Error.Message,
                    errorResponse.Error.Suggestion
                );
            }
            catch (JsonException)
            {
                throw new LambdaCloudApiException(
                    "unknown_error",
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                );
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Helper class for instance types response
public class InstanceTypesItem
{
    [JsonPropertyName("instance_type")]
    public required InstanceType InstanceType { get; set; }

    [JsonPropertyName("regions_with_capacity_available")]
    public List<Region>? RegionsWithCapacityAvailable { get; set; }
}