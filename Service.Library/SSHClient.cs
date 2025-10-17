using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
namespace Service.Library;

public class SshClientManager
    {
        private SshClient _sshClient = null!;
        private SftpClient _sftpClient = null!;

        // Basic SSH connection with password authentication
        public bool ConnectWithPassword(string host, int port, string username, string password)
        {
            try
            {
                _sshClient = new SshClient(host, port, username, password);
                
                _sshClient.Connect();
                
                Console.WriteLine($"Connected to {host}:{port} as {username}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                return false;
            }
        }

        // SSH connection with private key authentication
        public bool ConnectWithPrivateKey(string host, int port, string username, string privateKeyPath, string passphrase = "")
        {
            try
            {
                var keyFile = string.IsNullOrEmpty(passphrase) 
                    ? new PrivateKeyFile(privateKeyPath)
                    : new PrivateKeyFile(privateKeyPath, passphrase);

                var keyFiles = new[] { keyFile };
                
                
                _sshClient = new SshClient(host, port, username, keyFiles);
                _sshClient.Connect();
                
                Console.WriteLine($"Connected to {host}:{port} as {username} using private key");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                return false;
            }
        }

        // Execute a single command
        public string ExecuteCommand(string command)
        {
            if (_sshClient == null || !_sshClient.IsConnected)
            {
                throw new InvalidOperationException("SSH client is not connected");
            }

            try
            {
                using (var cmd = _sshClient.CreateCommand(command))
                {
                    
                    var result = cmd.Execute();
                    
                    if (!string.IsNullOrEmpty(cmd.Error))
                    {
                        Console.WriteLine($"Error: {cmd.Error}");
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command execution failed: {ex.Message}");
                return String.Empty;
            }
        }

        // Execute command asynchronously
        public async Task<string> ExecuteCommandAsync(string command)
        {
            if (_sshClient == null || !_sshClient.IsConnected)
            {
                throw new InvalidOperationException("SSH client is not connected");
            }

            try
            {
                using (var cmd = _sshClient.CreateCommand(command))
                {
                    var asyncResult = cmd.BeginExecute();
                    
                    while (!asyncResult.IsCompleted)
                    {
                        await Task.Delay(100);
                    }
                    
                    var result = cmd.EndExecute(asyncResult);
                    
                    if (!string.IsNullOrEmpty(cmd.Error))
                    {
                        Console.WriteLine($"Error: {cmd.Error}");
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command execution failed: {ex.Message}");
                return String.Empty;
            }
        }

        // Start SFTP session for file operations
        public bool StartSftpSession()
        {
            if (_sshClient == null || !_sshClient.IsConnected)
            {
                throw new InvalidOperationException("SSH client is not connected");
            }

            try
            {
                var connectionInfo = _sshClient.ConnectionInfo;
                _sftpClient = new SftpClient(connectionInfo);
                _sftpClient.Connect();
                
                Console.WriteLine("SFTP session started");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SFTP connection failed: {ex.Message}");
                return false;
            }
        }

        // Upload file via SFTP
        public bool UploadFile(string localPath, string remotePath)
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
            {
                throw new InvalidOperationException("SFTP client is not connected");
            }

            try
            {
                using (var fileStream = new FileStream(localPath, FileMode.Open))
                {
                    // change to 100mb
                    _sftpClient.BufferSize = 100_000_00; // 4KB buffer
                    _sftpClient.UploadFile(fileStream, remotePath);
                }
                
                Console.WriteLine($"File uploaded: {localPath} -> {remotePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File upload failed: {ex.Message}");
                return false;
            }
        }

        // Download file via SFTP
        public bool DownloadFile(string remotePath, string localPath)
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
            {
                throw new InvalidOperationException("SFTP client is not connected");
            }

            try
            {
                using (var fileStream = File.Create(localPath))
                {
                    _sftpClient.DownloadFile(remotePath, fileStream);
                }
                
                Console.WriteLine($"File downloaded: {remotePath} -> {localPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File download failed: {ex.Message}");
                return false;
            }
        }

        // List directory contents
        public void ListDirectory(string path = ".")
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
            {
                throw new InvalidOperationException("SFTP client is not connected");
            }

            try
            {
                var files = _sftpClient.ListDirectory(path);
                
                Console.WriteLine($"\nDirectory listing for: {path}");
                Console.WriteLine("Type\tSize\tName");
                Console.WriteLine("----\t----\t----");
                
                foreach (var file in files)
                {
                    var type = file.IsDirectory ? "DIR" : "FILE";
                    var size = file.IsDirectory ? "-" : file.Length.ToString();
                    Console.WriteLine($"{type}\t{size}\t{file.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Directory listing failed: {ex.Message}");
            }
        }

        // Port forwarding example (local port forwarding)
        public ForwardedPortLocal? CreatePortForward(uint localPort, string remoteHost, uint remotePort)
        {
            if (_sshClient == null || !_sshClient.IsConnected)
            {
                throw new InvalidOperationException("SSH client is not connected");
            }

            var portForward = new ForwardedPortLocal("127.0.0.1", localPort, remoteHost, remotePort);
            _sshClient.AddForwardedPort(portForward);
            
            try
            {
                portForward.Start();
                Console.WriteLine($"Port forwarding started: localhost:{localPort} -> {remoteHost}:{remotePort}");
                return portForward;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Port forwarding failed: {ex.Message}");
                return null;
            }
        }

        // Disconnect and cleanup
        public void Disconnect()
        {
            try
            {
                _sftpClient?.Disconnect();
                _sftpClient?.Dispose();
                
                _sshClient?.Disconnect();
                _sshClient?.Dispose();
                
                Console.WriteLine("Disconnected from SSH server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect error: {ex.Message}");
            }
        }

        // Check if connected
        public bool IsConnected => _sshClient?.IsConnected ?? false;
    }
