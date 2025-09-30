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
        var apiKey = System.Environment.GetEnvironmentVariable("LAMBDA_KEY"); // Load from secure storage or environment variable
        _httpClient = new HttpClient();
        _cloudClient = new LambdaCloudClient(apiKey,_httpClient);
        
    }
    
    public event Action<string> SshLogMessage
    {
        add { OnLogMessage += value; }
        remove { OnLogMessage -= value; }
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
    
    public async Task<List<Instance>> ListInstances()
    {
        var rslt = await _cloudClient.ListInstancesAsync();
        
        return rslt;
        
    }
    // get a list of ssh keys
    public async Task<List<SSHKey>> ListSshKeys()
    {
        
        var rslt = await _cloudClient.ListSSHKeysAsync();
        
        return rslt;
    }
    
    public async Task<List<Image>> ListImages()
    {
        var rslt = await _cloudClient.ListImagesAsync();
        
        return rslt;
    }
    // create a server
    public async Task<List<string>> CreateServer(string InstName,string instanceType, string Region,string sshKeyId, string? fileSystemId)
    {
        
        /*
         *
         * curl --request POST --url 'https://cloud.lambda.ai/api/v1/instance-operations/launch' \
                --header 'accept: application/json' \
                --user '<YOUR-API-KEY>:' \
                --data '{
             "region_name": "europe-central-1",
             "instance_type_name": "gpu_8x_a100",
             "ssh_key_names": [
               "my-public-key"
             ],
             "file_system_names": [
               "my-filesystem"
             ],
             "file_system_mounts": [
               {
                 "mount_point": "/data/custom-mount-point",
                 "file_system_id": "398578a2336b49079e74043f0bd2cfe8"
               }
             ],
             "hostname": "headnode1",
             "name": "My Instance",
             "image": {
               "id": "string"
             },
             "user_data": "string",
             "tags": [
               {
                 "key": "key1",
                 "value": "value1"
               }
             ],
             "firewall_rulesets": [
               {
                 "id": "c4d291f47f9d436fa39f58493ce3b50d"
               }
             ]
           }'
         */
        
        /*
         *  Required:
         *  1. Region
         *  2. Instance Type
         *  3. SSH Key
         * 
         */
        
        var instance = new InstanceLaunchRequest();
        instance.RegionName = Region;
        instance.InstanceTypeName = instanceType;
        instance.SshKeyNames = new List<string>() { sshKeyId };
        instance.FileSystemNames = (fileSystemId is null) ? null : new List<string>(){ fileSystemId};
        instance.Name = InstName;

        var nm =await _cloudClient.LaunchInstanceAsync(instance);

        return nm.InstanceIds;

        // var rslt = await _cloudClient.CreateInstanceAsync(instanceType, fileSystemId, sshKeyId);

    }
    // delete the server
    public async Task DeleteServer(string instanceId)
    {
        var rslt = await _cloudClient.TerminateInstancesAsync(new InstanceTerminateRequest(){ InstanceIds = new List<string>() { instanceId } });
    }
    
    public void SShSetup(Instance instance, string kypath)
    {
        var sshManager = new SshClientManager();

        sshManager.ConnectWithPrivateKey(instance.Ip, 22, "ubuntu", kypath);
        // Example file upload
        // sshManager.UploadFile("localfile.txt", "/root/remotefile.txt");

        // Example file download
        // sshManager.DownloadFile("/root/remotefile.txt", "localfile.txt");

        // Disconnect when done
        //sshManager.Disconnect();

        this.SetupRemoteServer(sshManager);

    }
    
    private void SetupRemoteServer(SshClientManager manager)
    {
        var cmds = new List<string>
        {
            "sudo apt update",
            "sudo apt install git -y",
            "wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb",
            "sudo dpkg -i packages-microsoft-prod.deb",
            "rm packages-microsoft-prod.deb",
            "sudo apt update",
            "sudo apt install dotnet-sdk-9.0 -y",
            "sudo ufw allow 7777",
        };
        
        foreach (var cmd in cmds)
        {
            var rslt = manager.ExecuteCommand(cmd);
            System.Threading.Thread.Sleep(1);
            OnLogMessage?.Invoke(rslt);
        }

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