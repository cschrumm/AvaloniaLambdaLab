using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
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
        return $"{Name} - {Region.Name} -  ${IntType.InstanceType.PriceCentsPerHour/100.0}/hr";
    }
}

public class DataForApp
{
    public string SelectedInstanceName { get; set; } = String.Empty;
    public string SelectedInstanceRegion { get; set; } = String.Empty;
    public string SelectedSshKey { get; set; } = String.Empty;
    public string SelectedImageId { get; set; } = String.Empty;
    public string PathToPrivateKey { get; set; } = String.Empty;
    public string? SelectedFileSystemId { get; set; } = null;
    public string GuidToken { get; set; } = String.Empty;
    
}


public class MainGuiBackend
{
    // talks to the lambda cloud and manages the instance
    private readonly LambdaCloudClient _cloudClient;
    // used to talk to the instance api
    private readonly HttpClient _httpClient;
    // launched url for jupyter lab
    private string _launched_url = "";
    private string _api_url = "";
    private string _app_data_path = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/AvaloniaLambdaLab";
    private DataForApp _dataForApp;
    public event Action<string> OnLogMessage;
    public event Action <string> OnInstanceLaunched;
    
    public bool IsRunning { get; private set; }
    public MainGuiBackend()
    {
        var apiKey = System.Environment.GetEnvironmentVariable("LAMBDA_KEY"); // Load from secure storage or environment variable
        _httpClient = new HttpClient();
        _cloudClient = new LambdaCloudClient(apiKey,_httpClient);

        if (!Directory.Exists(_app_data_path))
        {
            try
            {
                Directory.CreateDirectory(_app_data_path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
               // throw;
            }
        }
        
        this.LoadAppData();
        
    }

    public void LoadAppData()
    {
        // Load any saved application data from a file app.json
        var app_file = $"{_app_data_path}/app.json";
        if (File.Exists(app_file))
        {
            var json = File.ReadAllText(app_file);
            _dataForApp = System.Text.Json.JsonSerializer.Deserialize<DataForApp>(json) ?? new DataForApp();
            
        }
        else
        {
            _dataForApp = new DataForApp();
        }
        
        if(_dataForApp.GuidToken is null || _dataForApp.GuidToken == String.Empty)
            _dataForApp.GuidToken = Guid.NewGuid().ToString();
        
    }
    
    public void SaveAppData()
    {
        // Save application data to a file app.json
        var app_file = $"{_app_data_path}/app.json";
        var json = System.Text.Json.JsonSerializer.Serialize(_dataForApp);
        File.WriteAllText(app_file, json);
    }
    

    public void Startup()
    {
        Task.Run(async () =>
        {
            var insts = await _cloudClient.ListInstancesAsync();

            if (insts == null || !insts.Any())
            {
                InitalizeInstance(insts[0]);
            }
        });

    }

    private async Task InitalizeInstance(Instance instance)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        
        
        while(instance.Status.ToLower()!="running")
        {
            Task.Delay(5000).Wait();
            instance = (await _cloudClient.ListInstancesAsync()).Find(i => i.Id == instance.Id);
            OnLogMessage?.Invoke($"Instance Status: {instance.Status}");
        }
        
        OnLogMessage?.Invoke($"Instance is running at IP: {instance.Ip}");
        
        _launched_url = instance.JupyterUrl;
        
        _api_url = $"https://{instance.Ip}:7777/api/system";

        IsRunning = true;
        OnInstanceLaunched?.Invoke("Instance Launched");
    }
    
    public async Task<SystemStats?> RetrieveSystemStats()
    {
        var rslt = await _httpClient.GetFromJsonAsync<SystemStats>(_api_url);
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
    
    public List<Image> CompantibleImages(InstanceNameDesc instance, List<Image> images)
    {
        var ins = images.Where(i => i.Region.Name == instance.Region.Name).ToList();
        
        ins.Sort((x, y) => x.Version.CompareTo(y.Version));
        
        return ins;
    }
    
    public async Task<List<Image>> ListImages()
    {
        var rslt = await _cloudClient.ListImagesAsync();
        
        return rslt;
    }
    // create a server
    public async Task<List<string>> CreateServer(string InstName,string instanceType, string Region,string sshKeyId,
        string image_id,
        string? fileSystemId)
    {
        
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
        instance.Image = new ImageId(){ Id = image_id };
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

        _api_url = String.Empty;
        _launched_url = String.Empty;
        
        IsRunning = false;
        
        OnInstanceLaunched?.Invoke($"Instance Terminated");
    }
    
    public void SShSetup(Instance instance, string kypath, string token)
    {
        var sshManager = new SshClientManager();

        sshManager.ConnectWithPrivateKey(instance.Ip, 22, "ubuntu", kypath);
        

        this.SetupRemoteServer(sshManager, token);

    }
    
    public void LaunchBrowser()
    {
        try
        {
            // command to launch the default browser
            // google-chrome "http://localhost:7777
            // For .NET Core Process.Start on URLs
            var psi = new ProcessStartInfo
            {
                FileName = "google-chrome",
                Arguments = _launched_url,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            OnLogMessage?.Invoke($"Failed to launch browser: {ex.Message}");
        }
    }
    
    private void SetupRemoteServer(SshClientManager manager, string token)
    {
        /*
         *  Sets up and runs the remote server....
         */
        var cmds = new List<string>
        {
            "sudo apt update",  /* update package lists */
            "sudo apt install git -y", /* install git */
            "sudo add-apt-repository ppa:dotnet/backports", /* add dotnet repo */
            "sudo apt update", /* update package lists again */
            "sudo apt-get install -y dotnet-sdk-9.0", /* install dotnet sdk */
            "sudo apt-get install -y aspnetcore-runtime-9.0", /* install dotnet runtime */
            "dotnet dev-certs https --trust" /* trust the dev certs */,
            "rm -r AvaloniaLambdaLab", /* remove any existing repo */
            "rm -r publish", /* remove any existing publish directory */
            "git clone https://github.com/cschrumm/AvaloniaLambdaLab.git", /* clone your repo */
            "cd ./AvaloniaLambdaLab/WebPerfmon", /* change to your project directory */
            "pwd", /* print working directory for verification */
            "dotnet build ./AvaloniaLambdaLab/WebPerfmon/WebPerfmon.csproj -c Release -o ./publish", /* publish the project */
            "sudo ufw allow 7777", /* open the port in the firewall */
            $"./publish/WebPerfmon --token {token} > app.log 2>&1 &", /* run the app in the background */
        };
        
        foreach (var cmd in cmds)
        {
            // get current datetime
            var inpt = $"{System.DateTime.Now} - Executing: {cmd}";
            OnLogMessage?.Invoke(inpt);
            var rslt = manager.ExecuteCommand(cmd);
            System.Threading.Thread.Sleep(10);
            OnLogMessage?.Invoke(rslt);
        }

    }

    public async void Shutdown()
    {
        this.SaveAppData();
    }
    
    
    
}