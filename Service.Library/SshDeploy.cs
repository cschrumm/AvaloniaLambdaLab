namespace Service.Library;

public class SshDeploy
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _key;
    private readonly string _user;

    public event Action<string>? OnLogMessage;
    
    public SshDeploy(string host, string key, string user, int port = 22)
    {
        this._host = host;
        this._port = port;
        this._key = key;
        this._user = user;
    }
    public async Task CopyDirectory(string source)
    {
        var sshManager = new SshClientManager();
        try
        {
            sshManager.ConnectWithPrivateKey(_host, _port, _user, _key);
            sshManager.StartSftpSession();

            var nw = new DirectoryInfo(source);
            // get tempary directory

            OnLogMessage?.Invoke("CLEAR");
            OnLogMessage?.Invoke($"Uploading {nw.FullName} zipping..");
            var tmp_dir = Path.Combine(Path.GetTempPath(), "tmp_repo.zip");
            if (File.Exists(tmp_dir)) File.Delete(tmp_dir);

            System.IO.Compression.ZipFile.CreateFromDirectory(source, tmp_dir);
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
        
        await Task.CompletedTask;
    }
    
    public async Task DeployRepository(Repository repo, string git_hub_key)
    {
        var sshManager = new SshClientManager();
        
        var cmd_ln = $"echo  \"{git_hub_key}\" | gh auth login --with-token && gh repo clone {repo.Full_Name}";

        //OnInstanceLaunched?.Invoke("CLEAR");
        OnLogMessage?.Invoke("CLEAR");
        try
        {
            sshManager.ConnectWithPrivateKey(_host, _port, _user, _key);
            OnLogMessage?.Invoke($"Cloning Repo: {git_hub_key}");
            var rslt = sshManager.ExecuteCommand(cmd_ln);
            OnLogMessage?.Invoke(cmd_ln.Replace(git_hub_key, "key---****"));
            OnLogMessage?.Invoke(rslt);
        }
        finally
        {
            sshManager.Disconnect();
        }
        
        await Task.CompletedTask;
    }
    
    public async Task DeployRequirements(string token)
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
        
        var sshManager = new SshClientManager();
        try
        {
            sshManager.ConnectWithPrivateKey(_host, _port, _user, _key);

            var cmds = new List<string>
        {
            "sudo apt update", /* update package lists */
            "sudo apt install -y build-essential curl wget git", /* install all the development tools git */
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
            "dotnet restore ./AvaloniaLambdaLab/WebPerfmon/WebPerfmon.csproj",
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
            var rslt = sshManager.ExecuteCommand(cmd);
            await Task.Delay(100);
            OnLogMessage?.Invoke(rslt);
        }
            //await this.SetupRemoteServer(sshManager, token);
        }
        finally
        {
            sshManager.Disconnect();
        }
        
        await Task.CompletedTask;
    }
    
    
    
    
}