using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Service.Library;

namespace Service.Library;


public class InstanceNameDesc
{
    public string Name { get; set; } = String.Empty;
    public InstanceTypesItem IntType { get; set; }
    public Region Region { get; set; }
    public override string ToString()
    {
        return $"{Name} - {Region.Name}";
    }
}
public class MainGuiBackend
{
    private readonly LambdaCloudClient _cloudClient;
    private readonly HttpClient _httpClient;
    private string _launched_url = "";
    
    public Action<string> OnLogMessage;
    public MainGuiBackend()
    {
        var apiKey = ""; // Load from secure storage or environment variable
        _httpClient = new HttpClient();
        _cloudClient = new LambdaCloudClient(apiKey,_httpClient);
        
    }
    
    
    
    public async Task<SystemStats?> RetrieveSystemStats()
    {
        string url = $"{_launched_url}/api/systemstats/system";
        var rslt = await _httpClient.GetFromJsonAsync<SystemStats>(url);
        return rslt;
    }
    
    // get a list of available servers you can create
    public async Task<List<InstanceNameDesc>> InStanceTypes()
    {
        var tmp = new List<InstanceNameDesc>();
        var rslt = await _cloudClient.ListInstanceTypesAsync();
        
        foreach (var item in rslt)
        {
            foreach (var region in item.Value.RegionsWithCapacityAvailable)
            {
                tmp.Add(new InstanceNameDesc
                {
                    Name = item.Key,
                    IntType = item.Value,
                    Region = region
                });
            }
        }
        
        return tmp;
        
    }
    // get a list of available file systems
    public async Task<List<Filesystem>> ListFileSystems()
    {
        
        var rslt = await _cloudClient.ListFilesystemsAsync();
        
        return rslt;


    }
    // get a list of ssh keys
    public async Task<List<SSHKey>> ListSshKeys()
    {
        var tmp = new List<SSHKey>();
        var rslt = await _cloudClient.ListSSHKeysAsync();
        
        return tmp;
    }
    // create a server
    public async Task CreateServer(string instanceType, string fileSystemId, string sshKeyId)
    {
        // var rslt = await _cloudClient.CreateInstanceAsync(instanceType, fileSystemId, sshKeyId);
       
    }
    // delete the server
    public async Task DeleteServer(string instanceType, string fileSystemId, string sshKeyId)
    {
        
    }
    
    private void SetupRemoteServer()
    {
        SshClientManager sshManager = new SshClientManager();
        
        /*
         *
         * sudo apt update
           sudo apt install git -y
           git config --global user.name "Your Name"
           git config --global user.email "your.email@example.com"
           git clone git@github.com:username/repository-name.git
           cd repository-name
           
           wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
           sudo dpkg -i packages-microsoft-prod.deb
           rm packages-microsoft-prod.deb
           sudo apt update
           sudo apt install dotnet-sdk-9.0 -y
           dotnet restore
           dotnet build
           uname -m # check architecture x86_64 or aarch64
           dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
           or
           dotnet publish -c Release -r linux-arm64 --self-contained -o ./publish
           cd publish
           
           sudo ufw allow 5000
           sudo ufw allow 5001
           run the app in the background
           nohup dotnet YourAppName --url http://localhost:7777 > app.log 2>&1 &
         */
        
        
        
    }
    
    
    
}