using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
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
    private LambdaCloudClient? _cloudClient;

    private GitHubApiClient? _gitHubClient;

    // used to talk to the instance api
    private readonly HttpClient _httpClient;

    //private string _api_url = "";
    // better cross platform way to get app data path
    private string _app_data_path =
        Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            (Process.GetCurrentProcess().ProcessName ?? "LambdaAppData"));
    
    

    public ISeries[] Series { get; set; } = Array.Empty<ISeries>();

    private Dictionary<string, List<float>> _chart_data = new();

    private DataForApp _dataForApp = new(); // load by front end
    public event Action<string>? OnLogMessage;
    public event Action<string>? OnInstanceLaunched;

    private List<Service.Library.Image> _allImages = new();
    
    private List<Filesystem> _allFilesystems = new();
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
    

    public MainGuiBackend()
    {
        OnLogMessage += MonitorLog;

        if (Directory.Exists(_app_data_path) == false)
        {
            Directory.CreateDirectory(_app_data_path);
        }

      

        HttpClientHandler handler = new HttpClientHandler();
        // ignore ssl errors we are self signed
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        _httpClient = new HttpClient(handler);

        _httpClient.Timeout = TimeSpan.FromMinutes(5);


        

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

    public async Task LoadAppData()
    {
        // Load any saved application data from a file app.json
        var app_file = $"{_app_data_path}/app.json";
        bool data_changed = false;
        
        if (File.Exists(app_file))
        {
            var json = File.ReadAllText(app_file);
            _dataForApp = System.Text.Json.JsonSerializer.Deserialize<DataForApp>(json) ?? new DataForApp();
        }
        else
        {
             data_changed = true;
            _dataForApp = new DataForApp();
        }
        
        
        if (_dataForApp.GuidToken is null || _dataForApp.GuidToken == String.Empty)
        {
            data_changed = true;
            _dataForApp.GuidToken = Guid.NewGuid().ToString();
        }
            


        PathToKey = _dataForApp.PathToPrivateKey ?? String.Empty;

        

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
            
            if(SelectedImage is not null && SelectedImage.Region.Name != SelectedInstance.Region.Name)
            {
                var imgs = this.CompantibleImages(SelectedInstance);
                
                //SelectedImage = imgs.FirstOrDefault(i => i.Id == _dataForApp.SelectedImageId)!;
                Images.Clear();
                
                foreach (var x in imgs)
                {
                    Images.Add(x);
                }

                if (Images.Any())
                {
                    SelectedImage = Images.Last();
                }
                data_changed = true; 
            }
        }
        
        if (!_dataForApp.SelectedFileSystemId.IsNullOrEmpty())
        {
            SelectedFilesystem = Filesystems.FirstOrDefault(f => f.Name == _dataForApp.SelectedFileSystemId)!;

            if (SelectedFilesystem is not null)
            {
                // not the same region need to update
                if (SelectedFilesystem.Region != SelectedInstance.Region)
                {
                    var fls = this.CompatibleFilesystems(SelectedInstance);
                    
                    //SelectedFilesystem = fls.FirstOrDefault(f => f.Name == _dataForApp.SelectedFileSystemId)!;
                    Filesystems.Clear();
                    
                    foreach (var x in fls)
                    {
                        Filesystems.Add(x);
                    }

                    if (Filesystems.Any())
                    {
                        SelectedFilesystem = Filesystems.First();
                    }
                    
                    data_changed = true; 
                }
                
            }
        }

        if (data_changed)
        {
           await SaveAppData();
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


    public async Task InstallRepo(Repository repo, Instance instance, string kypath)
    {
        
        SshDeploy dply = new SshDeploy(instance.Ip, kypath, "ubuntu", 22);
        dply.OnLogMessage += (msg) => OnLogMessage?.Invoke(msg);
        await dply.DeployRepository(repo, SecretManage.GetGitHubToken());
        
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
        
        var apiKey = SecretManage.GetLambdaKey(); 
        
        if(SecretManage.IsSecretAvailable() == false)
        {
            OnLogMessage?.Invoke("Warning: Secret management not available. Make sure to set SECRET_PATH environment variable.");
        }
        
        if (apiKey is null || apiKey == String.Empty)
        {
            OnLogMessage?.Invoke($"No API Key provided. Please provide a valid API Key.");
            //throw new Exception("LAMBDA_KEY environment variable not set");
        }
        else
        {
            _cloudClient = new LambdaCloudClient(apiKey, _httpClient);
        }

        

        var gitToken = SecretManage.GetGitHubToken(); // System.Environment.GetEnvironmentVariable("GIT_SECRET");

        if (gitToken is null || gitToken == String.Empty)
        {
            //  throw new Exception("GIT_SECRET environment variable not set");
            OnLogMessage?.Invoke("Warning: git secret variable not set. GitHub features will be disabled.");
            
        }
        else
        {
            _gitHubClient = new GitHubApiClient(gitToken);
        }

        
        
        Task.Run(async () =>
        {   
            await this.LoadLambdaData();
            await this.LoadGitHubData();
            await this.LoadAppData();
        }).Wait();
    }

    // get a list of available servers you can create
    public async Task<List<InstanceNameDesc>> InStanceTypes()
    {
        var tmp = new List<InstanceNameDesc>();
        
        if (_cloudClient is null)
            return tmp;
        
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
        if(_cloudClient is null)
            return new List<Filesystem>();
        var rslt = await _cloudClient.ListFilesystemsAsync();
        return rslt;
    }

    /*
    public async Task<List<Instance>> ListInstances()
    {
        var rslt = await _cloudClient.ListInstancesAsync();
        return rslt;
    }
    */

    // get a list of ssh keys
    public async Task<List<SSHKey>> ListSshKeys()
    {
        if(_cloudClient is null)
            return new List<SSHKey>();
        
        var rslt = await _cloudClient.ListSSHKeysAsync();
        return rslt;
    }

    public async Task<SystemStats?> GetInstanceData(Instance instance)
    {
        // add token to header apikey
        var asp_url = $"https://{instance.Ip}:7777/api/SystemStats/system";
        try
        {
            if (_dataForApp.GuidToken.IsNullOrEmpty())
            {
                throw new Exception("Guid token is null or empty");
            }
                
            if (!_httpClient.DefaultRequestHeaders.Contains("apikey"))
                _httpClient.DefaultRequestHeaders.Add("apikey", _dataForApp.GuidToken);
            
            var rslt = await _httpClient.GetFromJsonAsync<SystemStats>(asp_url);
            return rslt;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting instance data from {asp_url}");
            Console.WriteLine(e);
            // throw;
        }

        return null;
    }

    public async Task UpdateSeries()
    {
        List<ISeries> series = new List<ISeries>();
        
        if(_cloudClient is null)
            return;
        var insts =  await _cloudClient.ListInstancesAsync();
        
        //var running_ids = insts.Select(i => i.Id).ToList();
        
        //var collection = new Collection<Instance>(insts);
        
        var changed = false;
        var inst_changed =this.RunningInstances.SyncronizeCollections(insts,(a,b)=> a.Id == b.Id,
            (dest,src) =>
            {
                if(dest.Status != src.Status)
                    changed = true;
                
                if(dest.Id != src.Id)
                    changed = true;
                dest.Ip = src.Ip;
                dest.Status = src.Status;
                dest.JupyterUrl = src.JupyterUrl;
            });

        if (changed || inst_changed)
        {
            OnPropertyChanged(nameof(RunningInstances));
        }
            

        var mss = _chart_data.Keys.Where(x => !this.RunningInstances.Any(i => i.Id == x)).ToList();

        foreach (var ms in mss)
        {
            _chart_data.Remove(ms);
        }

        var to_remove = new List<Instance>();
        foreach (var i in this.RunningInstances)
        {
            if (i.Status != "active" || i.Id is null || i.Id == "")
                continue;

            var sts = await this.GetInstanceData(i);

            if (!_chart_data.ContainsKey(i.Id))
            {
                _chart_data.Add(i.Id, new List<float>());
            }

            if (sts is null)
                return;
            
            var lst = _chart_data[i.Id];

            var ttl = (float)sts.GpuStats.Sum(g => g.UtilizationPercentage) /
                      (sts.GpuStats.Count == 0 ? 1 : sts.GpuStats.Count);
            //var tst = (float)(new Random()).NextDouble() * 100;
            lst.Add(ttl);
            if (lst.Count > 40)
            {
                lst.RemoveAt(0);
            }

            series.Add(new LineSeries<float>
            {
                Name = i.Name,
                Values = lst,
                Fill = null
            });
        }

        this.Series = series.ToArray();

        OnPropertyChanged(nameof(Series));
    }

    public async Task LoadLambdaData()
    {
        var intypes = this.InStanceTypes();
        var filesys = this.ListFileSystems();
        var keys = this.ListSshKeys();
        var images = this.ListImages();
        await Task.WhenAll(intypes, filesys, keys, images);

        _allImages = images.Result;
        _allFilesystems = filesys.Result;

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
        if (_gitHubClient is null)
            return;
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

    public List<Filesystem> CompatibleFilesystems(InstanceNameDesc instance)
    {
        var ins = _allFilesystems.Where(i => i.Region.Name == instance.Region.Name).ToList();

        ins.Sort((x, y) => x.Name.CompareTo(y.Name));

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
        
        var fsys = this.CompatibleFilesystems(instance);
        
        Filesystems.Clear();
        foreach (var x in fsys)
        {
            Filesystems.Add(x);
        }

        OnPropertyChanged(nameof(Filesystems));
        OnPropertyChanged(nameof(Images));

        if (this.Images.Any())
        {
            this.SelectedImage = this.Images[^1];
            OnPropertyChanged(nameof(SelectedImage));
        }

        if (this.Filesystems.Any())
        {
            this.SelectedFilesystem = this.Filesystems[^1];
            OnPropertyChanged(nameof(SelectedFilesystem));
        }
    }

    public async Task<List<Image>> ListImages()
    {
        if(_cloudClient is null)
            return new List<Image>();
        
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

        if(_cloudClient is null) throw new Exception("Cloud client not initialized");

        if (SelectedInstance is null) throw new Exception("No instance selected");
        if (SelectedInstance?.IntType is null) throw new Exception("Selected instance has no instance type");
        if (SelectedSshKey is null) throw new Exception("No SSH Key selected");
        if (SelectedImage is null) throw new Exception("No Image selected");
        if (PathToKey.IsNullOrEmpty()) throw new Exception("No path to private key specified");
        if (!File.Exists(PathToKey)) throw new Exception("Path to private key does not exist");

        /* Crude naming convention */
        var cntr = RunningInstances.Count + 1;
        var instName = $"{SelectedInstance.Name}-{cntr}";
        OnLogMessage?.Invoke($"Staring.. Please be patient this may take several minutes...");

        var flsys_name = (SelectedFilesystem is null || SelectedFilesystem?.Name == "")
            ? null
            : SelectedFilesystem?.Name;

        var insts = await CreateServer(instName, SelectedInstance.IntType.InstanceType.Name,
            SelectedInstance.Region.Name, SelectedSshKey.Name, SelectedImage.Id, flsys_name);

        if (insts is null || !insts.Any()) throw new Exception("Failed to create instance");
        OnLogMessage?.Invoke($"Waiting for setup");
        await Task.Delay(10000); // give it a second to show up in the list

        var instance =
            await _cloudClient
                .GetInstanceAsync(insts[0]); //(await _cloudClient.ListInstancesAsync()).Find(i => i.Id == insts[0]);
        
        int tries = 0;
        
        while (instance.Status.ToLower() != "active"
               && instance.Status.ToLower() != "running")
        {
            await Task.Delay(8000);
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
        
        if(_cloudClient is null) throw new Exception("Cloud client not initialized");

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
        
        // the timer will remove deleted instances from the list
        OnLogMessage?.Invoke($"CLEAR");
        OnLogMessage?.Invoke($"Terminating Instance {instance.Name}...");
        
        if(_cloudClient is null)
            return;
        
        var rslt = await _cloudClient.TerminateInstancesAsync(new InstanceTerminateRequest()
            { InstanceIds = new List<string>() { instance.Id } });
        OnLogMessage?.Invoke($"Terminated instance {instance.Name}.");

        //_launched_url = String.Empty;

        
        
        OnInstanceLaunched?.Invoke($"Instance Terminated");
    }

    public async Task SShSetup(Instance instance, string kypath, string token)
    {
        SshDeploy dply = new SshDeploy(instance.Ip, kypath, "ubuntu", 22);
        dply.OnLogMessage += (msg) => OnLogMessage?.Invoke(msg);
        await dply.DeployRequirements(token);
    }


    public async Task ZipAndUpload(string path, Instance instance)
    {
        
        SshDeploy dply = new SshDeploy(instance.Ip, PathToKey, "ubuntu", 22);
        dply.OnLogMessage += (msg) => OnLogMessage?.Invoke(msg);
        await dply.CopyDirectory(path);
        
    }

    public async Task<Instance> GetInstance(string instanceId)
    {
        if(_cloudClient is null) throw new Exception("Cloud client not initialized");
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
                FileName = "firefox",
                Arguments = "-private-window " + instance.JupyterUrl,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            OnLogMessage?.Invoke($"Failed to launch browser: {ex.Message}");
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