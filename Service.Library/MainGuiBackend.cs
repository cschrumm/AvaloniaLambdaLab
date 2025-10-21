using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Service.Library;

namespace Service.Library;

public class InstanceNameDesc
{
    public string Name { get; set; } = String.Empty;
    public InstanceTypesItem? IntType { get; set; }
    public Region Region { get; set; } = new();

    public override string ToString()
    {
        return $"{Name} - {Region.Name} -  ${IntType?.InstanceType?.PriceCentsPerHour / 100.0}/hr";
    }
}

public class DataForApp
{
    public string SelectedInstanceName { get; set; } = String.Empty;
    public string SelectedSshKey { get; set; } = String.Empty;
    public string SelectedImageId { get; set; } = String.Empty;
    public string PathToPrivateKey { get; set; } = String.Empty;
    public string SelectedFileSystemId { get; set; } = String.Empty;
    public string GuidToken { get; set; } = String.Empty;
}

public class MainGuiBackend : INotifyPropertyChanged
{
    // talks to the lambda cloud and manages the instance
    private readonly LambdaCloudClient _cloudClient;
    private readonly GitHubApiClient _gitHubClient;
    // used to talk to the instance api
    private readonly HttpClient _httpClient;
    //private string _api_url = "";
    private string _app_data_path = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                    "/AvaloniaLambdaLab";

    private DataForApp _dataForApp = new(); // load by front end
    public event Action<string>? OnLogMessage;
    public event Action<string>? OnInstanceLaunched;

    private List<Service.Library.Image> _allImages = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    // bound to the front end.
    public ObservableCollection<InstanceNameDesc> Instances { get; set; } = new();
    public ObservableCollection<Filesystem> Filesystems { get; set; } = new();
    public ObservableCollection<SSHKey> SshKeys { get; set; } = new();
    public ObservableCollection<Service.Library.Image> Images { get; set; } = new();
    public InstanceNameDesc SelectedInstance { get; set; } = new();
    public Filesystem SelectedFilesystem { get; set; } = new();
    public SSHKey SelectedSshKey { get; set; } = new();
    public Service.Library.Image SelectedImage { get; set; } = new();
    public ObservableCollection<Repository> GitRepos { get; set; } = new();
    public ObservableCollection<Instance> RunningInstances { get; set; } = new();
    public string PathToKey { get; set; } = "";
    public string LogViewMessage { get; set; } = "";
    public bool IsRunning { get; private set; }

    public MainGuiBackend()
    {
        OnLogMessage += MonitorLog;

        var apiKey =
            SecretManage
                .GetLambdaKey(); // System.Environment.GetEnvironmentVariable("LAMBDA_KEY"); // Load from secure storage or environment variable
        
        HttpClientHandler handler = new HttpClientHandler();
        // ignore ssl errors we are self signed
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        _httpClient = new HttpClient(handler);
        
        _httpClient.Timeout = TimeSpan.FromMinutes(5);


        if (apiKey is null || apiKey == String.Empty)
        {
            throw new Exception("LAMBDA_KEY environment variable not set");
        }

        _cloudClient = new LambdaCloudClient(apiKey, _httpClient);

        var gitToken = SecretManage.GetGitHubToken(); // System.Environment.GetEnvironmentVariable("GIT_SECRET");

        if (gitToken is null || gitToken == String.Empty)
        {
            throw new Exception("GIT_SECRET environment variable not set");
        }

        _gitHubClient = new GitHubApiClient(gitToken);

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

        //this.LoadAppData();
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

        if (_dataForApp.GuidToken is null || _dataForApp.GuidToken == String.Empty)
            _dataForApp.GuidToken = Guid.NewGuid().ToString();


        PathToKey = _dataForApp.PathToPrivateKey ?? String.Empty;

        if (!_dataForApp.SelectedFileSystemId.IsNullOrEmpty())
        {
            SelectedFilesystem = Filesystems.FirstOrDefault(f => f.Name == _dataForApp.SelectedFileSystemId)!;
        }

        if (!_dataForApp.SelectedInstanceName.IsNullOrEmpty())
        {
            SelectedInstance = Instances.Where(i => i.Name == _dataForApp.SelectedInstanceName).FirstOrDefault()!;
        }

        if (!_dataForApp.SelectedSshKey.IsNullOrEmpty())
        {
            SelectedSshKey = SshKeys.Where(s => s.Name == _dataForApp.SelectedSshKey).FirstOrDefault()!;
        }

        if (!_dataForApp.SelectedImageId.IsNullOrEmpty())
        {
            SelectedImage = Images.Where(i => i.Id == _dataForApp.SelectedImageId).FirstOrDefault()!;
        }


        OnPropertyChanged(nameof(PathToKey));
        OnPropertyChanged(nameof(SelectedFilesystem));
        OnPropertyChanged(nameof(SelectedInstance));
        OnPropertyChanged(nameof(SelectedSshKey));
        OnPropertyChanged(nameof(SelectedImage));
    }

    public async Task SaveAppData()
    {
        // Save application data to a file app.json
        var app_file = $"{_app_data_path}/app.json";

        //_dataForApp.PathToPrivateKey
        _dataForApp.PathToPrivateKey = PathToKey;
        _dataForApp.SelectedInstanceName = SelectedInstance?.Name ?? String.Empty;
        _dataForApp.SelectedSshKey = SelectedSshKey?.Name ?? String.Empty;
        _dataForApp.SelectedImageId = SelectedImage?.Id ?? String.Empty;
        _dataForApp.SelectedFileSystemId = SelectedFilesystem?.Name ?? String.Empty;
        _dataForApp.GuidToken = _dataForApp.GuidToken ?? Guid.NewGuid().ToString();
        var json = System.Text.Json.JsonSerializer.Serialize(_dataForApp);
        //File.WriteAllText(app_file, json);
        await File.WriteAllTextAsync(app_file, json);
    }


    public void InstallRepo(Repository repo, Instance instance, string kypath)
    {
        var sshManager = new SshClientManager();
        var git_hub_key = SecretManage.GetGitHubToken(); // System.Environment.GetEnvironmentVariable
        var cmd_ln = $"echo  \"{git_hub_key}\" | gh auth login --with-token && gh repo clone {repo.Full_Name}";

        OnInstanceLaunched?.Invoke("CLEAR");

        try
        {
            sshManager.ConnectWithPrivateKey(instance.Ip, 22, "ubuntu", kypath);
            OnLogMessage?.Invoke($"Cloning Repo: {git_hub_key}");
            var rslt = sshManager.ExecuteCommand(cmd_ln);
            OnLogMessage?.Invoke(cmd_ln.Replace(git_hub_key, "key---****"));
            OnLogMessage?.Invoke(rslt);
        }
        finally
        {
            sshManager.Disconnect();
        }
    }

    public void MonitorLog(string msg)
    {
        if ("CLEAR" == msg)
        {
            ClearLog();
            return;
        }

        LogViewMessage += msg + "\n";
        OnPropertyChanged(nameof(LogViewMessage));
    }

    private void ClearLog()
    {
        LogViewMessage = "";
        OnPropertyChanged(nameof(LogViewMessage));
    }

    public void Startup()
    {
        Task.Run(async () =>
        {
            await this.LoadLambdaData();
            await this.LoadGitHubData();
            this.LoadAppData();

            var insts = await _cloudClient.ListInstancesAsync();

            foreach (var inst in insts)
            {
                RunningInstances.Add(inst);
            }

            OnPropertyChanged(nameof(RunningInstances));
        });
    }

    // get a list of available servers you can create
    public async Task<List<InstanceNameDesc>> InStanceTypes()
    {
        var tmp = new List<InstanceNameDesc>();
        var rslt = await _cloudClient.ListInstanceTypesAsync();

        foreach (var item in rslt)
        {
            if (item.Value.RegionsWithCapacityAvailable is null) continue;

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

    public async Task<SystemStats?> GetInstanceData(Instance instance)
    {
        // add token to header apikey

        try
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("apikey"))
                _httpClient.DefaultRequestHeaders.Add("apikey", _dataForApp.GuidToken);
            var asp_url = $"https://{instance.Ip}:7777/api/SystemStats/system";
            var rslt = await _httpClient.GetFromJsonAsync<SystemStats>(asp_url);
            return rslt;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // throw;
        }

        return null;
    }

    public async Task LoadLambdaData()
    {
        var intypes = this.InStanceTypes();
        var filesys = this.ListFileSystems();
        var keys = this.ListSshKeys();
        var images = this.ListImages();
        await Task.WhenAll(intypes, filesys, keys, images);

        _allImages = images.Result;

        Instances.Clear();
        Filesystems.Clear();
        SshKeys.Clear();
        Images.Clear();

        foreach (var x in intypes.Result)
        {
            Instances.Add(x);
        }

        foreach (var x in filesys.Result)
        {
            Filesystems.Add(x);
        }

        foreach (var x in keys.Result)
        {
            SshKeys.Add(x);
        }

        foreach (var x in images.Result)
        {
            Images.Add(x);
        }

        OnPropertyChanged(nameof(Instances));
        OnPropertyChanged(nameof(Filesystems));
        OnPropertyChanged(nameof(Images));
        OnPropertyChanged(nameof(SshKeys));
    }

    public async Task LoadGitHubData()
    {
        var repos = await _gitHubClient.GetAllMyRepositoriesAsync();

        GitRepos.Clear();

        foreach (var repo in repos)
        {
            GitRepos.Add(repo);
        }

        OnPropertyChanged(nameof(GitRepos));
    }

    public List<Image> CompantibleImages(InstanceNameDesc instance)
    {
        var ins = _allImages.Where(i => i.Region.Name == instance.Region.Name).ToList();

        ins.Sort((x, y) => x.Version.CompareTo(y.Version));

        return ins;
    }

    public void MakeImageSelection(InstanceNameDesc instance)
    {
        var ins = this.CompantibleImages(instance);

        Images.Clear();

        foreach (var x in ins)
        {
            Images.Add(x);
        }


        OnPropertyChanged(nameof(Images));

        if (this.Images.Count > 0)
        {
            this.SelectedImage = this.Images[^1];
            OnPropertyChanged(nameof(SelectedImage));
        }
    }

    public async Task<List<Image>> ListImages()
    {
        var rslt = await _cloudClient.ListImagesAsync();

        return rslt;
    }
    // create a server

    public async Task StartInstance()
    {
        /*
         * What we need to do create an instance....
         *
         */


        if (SelectedInstance is null) throw new Exception("No instance selected");
        if (SelectedInstance?.IntType is null) throw new Exception("Selected instance has no instance type");
        if (SelectedSshKey is null) throw new Exception("No SSH Key selected");
        if (SelectedImage is null) throw new Exception("No Image selected");
        if (PathToKey.IsNullOrEmpty()) throw new Exception("No path to private key specified");
        if (!File.Exists(PathToKey)) throw new Exception("Path to private key does not exist");

        /* Crude naming convention */
        var cntr = RunningInstances.Count + 1;
        var instName = $"{SelectedInstance.Name}-{cntr}";
        OnLogMessage?.Invoke($"Staring..");

        var flsys_name = (SelectedFilesystem is null || SelectedFilesystem?.Name == "")
            ? null
            : SelectedFilesystem?.Name;

        var insts = await CreateServer(instName, SelectedInstance.IntType.InstanceType.Name,
            SelectedInstance.Region.Name, SelectedSshKey.Name, SelectedImage.Id, flsys_name);

        if (insts is null || !insts.Any()) throw new Exception("Failed to create instance");
        OnLogMessage?.Invoke($"Waiting for setup");
        await Task.Delay(20000); // give it a second to show up in the list

        var instance =
            await _cloudClient
                .GetInstanceAsync(insts[0]); //(await _cloudClient.ListInstancesAsync()).Find(i => i.Id == insts[0]);

        RunningInstances.Add(instance);

        OnPropertyChanged(nameof(RunningInstances));

        int tries = 0;
        while (instance.Status.ToLower() != "active"
               && instance.Status.ToLower() != "running")
        {
            await Task.Delay(10000);
            tries++;
            // print on mod 10
            if (tries % 10 == 0)
            {
                OnLogMessage?.Invoke($"Still waiting to run... {tries * 10} seconds elapsed {instance.Status}");
            }

            //OnLogMessage?.Invoke($"Waiting to run..: {instance.Status}");
            instance = await _cloudClient
                .GetInstanceAsync(insts[0]); //(await _cloudClient.ListInstancesAsync()).Find(i => i.Id == insts[0]);
        }

        await this.SShSetup(instance, PathToKey, _dataForApp.GuidToken);
    }

    public async Task<List<string>> CreateServer(string InstName, string instanceType, string Region, string sshKeyId,
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
        instance.Image = new ImageId() { Id = image_id };
        instance.FileSystemNames = (fileSystemId is null) ? new List<string>() : new List<string>() { fileSystemId };
        instance.Name = InstName;

        var nm = await _cloudClient.LaunchInstanceAsync(instance);

        return nm.InstanceIds;

        // var rslt = await _cloudClient.CreateInstanceAsync(instanceType, fileSystemId, sshKeyId);
    }

    // delete the server
    public async Task DeleteServer(Instance instance)
    {
        var rslt = await _cloudClient.TerminateInstancesAsync(new InstanceTerminateRequest()
            { InstanceIds = new List<string>() { instance.Id } });


        //_launched_url = String.Empty;

        IsRunning = false;
        RunningInstances.Remove(instance);
        OnPropertyChanged(nameof(RunningInstances));
        OnInstanceLaunched?.Invoke($"Instance Terminated");
    }

    public async Task SShSetup(Instance instance, string kypath, string token)
    {
        var sshManager = new SshClientManager();

        try
        {
            sshManager.ConnectWithPrivateKey(instance.Ip, 22, "ubuntu", kypath);

            await this.SetupRemoteServer(sshManager, token);
        }
        finally
        {
            sshManager.Disconnect();
        }
    }


    public void ZipAndUpload(string path, Instance instance)
    {
        var sshManager = new SshClientManager();
        try
        {
            sshManager.ConnectWithPrivateKey(instance.Ip, 22, "ubuntu", PathToKey);
            sshManager.StartSftpSession();

            var nw = new DirectoryInfo(path);
            // get tempary directory

            OnLogMessage?.Invoke("CLEAR");
            OnLogMessage?.Invoke($"Uploading {nw.FullName} zipping..");
            var tmp_dir = Path.Combine(Path.GetTempPath(), "tmp_repo.zip");
            if (File.Exists(tmp_dir)) File.Delete(tmp_dir);

            System.IO.Compression.ZipFile.CreateFromDirectory(path, tmp_dir);
            OnLogMessage?.Invoke($"Zipped {nw.Name} sending...");
            sshManager.UploadFile(tmp_dir, nw.Name + ".zip");
            OnLogMessage?.Invoke($"Upload complete unzipping on server...");
            var rslt = sshManager.ExecuteCommand($"unzip -o {nw.Name}.zip -d {nw.Name}");
            OnLogMessage?.Invoke(rslt);
        }
        finally
        {
            sshManager.Disconnect();
        }
    }

    public async Task<Instance> GetInstance(string instanceId)
    {
        var ins = await _cloudClient.GetInstanceAsync(instanceId);
        return ins;
    }

    public void LaunchBrowser(Instance instance)
    {
        try
        {
            // command to launch the default browser
            // google-chrome "http://localhost:7777
            // For .NET Core Process.Start on URLs
            var psi = new ProcessStartInfo
            {
                FileName = "google-chrome",
                Arguments = "--new-window " + instance.JupyterUrl,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            OnLogMessage?.Invoke($"Failed to launch browser: {ex.Message}");
        }
    }

    private async Task SetupRemoteServer(SshClientManager manager, string token)
    {
        /*
         *  Sets up and runs the remote server....
         *
         *  1. Install git
         *  2. Install wget
         *  3. Install github cli
         *  4. Install dotnet sdk
         *  5. Clone your repo
         *  6. Build and run your project
         *  7. Open the port in the firewall
         *  8. Run the app in the background
         */
        var cmds = new List<string>
        {
            "sudo apt update", /* update package lists */
            "sudo apt install git -y", /* install git */
            "sudo apt install wget -y", /* install wget */
            "sudo mkdir -p -m 755 /etc/apt/keyrings", /* create keyrings directory */
            "wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null",
            "sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg",
            "echo \"deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main\" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null",
            "sudo apt update", /* update package lists again */
            "sudo apt install gh -y",
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

        OnLogMessage?.Invoke("CLEAR");
        foreach (var cmd in cmds)
        {
            // get current datetime
            var inpt = $"{System.DateTime.Now} - Executing: {cmd}";
            OnLogMessage?.Invoke(inpt);
            await Task.Delay(100);
            var rslt = manager.ExecuteCommand(cmd);
            await Task.Delay(100);
            OnLogMessage?.Invoke(rslt);
        }
    }

    public async void Shutdown()
    {
        await this.SaveAppData();
    }

    public void setKeyPath(string path)
    {
        PathToKey = path;
        OnPropertyChanged(nameof(PathToKey));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}