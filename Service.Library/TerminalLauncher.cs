using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Service.Library;

public class TerminalLauncher
{
    private string? sshCommand = null;
    private string host = String.Empty;
    private string user = String.Empty;
    private int port = 0;
    private string keyPath = String.Empty;

    public TerminalLauncher(string host, int port, string user, string keyPath)
    {
        
        this.host = host;
        this.port = port;
        this.keyPath = keyPath;
        this.user = user;
        
    }
    public void Run(string[] args)
    {
        if (!ParseArguments(args))
        {
            PrintUsage();
            Environment.Exit(1);
        }

        BuildSshCommand();

        Console.WriteLine($"Detected platform: {GetPlatformName()}");
        if (!string.IsNullOrEmpty(sshCommand))
        {
            Console.WriteLine($"SSH command: {sshCommand}");
        }

        try
        {
            bool success = false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                success = LaunchTerminalLinux();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                success = LaunchTerminalMacOS();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                success = LaunchTerminalWindows();
            }
            else
            {
                Console.WriteLine("Error: Unsupported platform");
                Environment.Exit(1);
            }

            if (success)
            {
                Console.WriteLine("Terminal launched successfully!");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Failed to launch terminal");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching terminal: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private bool ParseArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--ssh":
                    if (i + 1 < args.Length)
                    {
                        sshCommand = args[++i];
                    }

                    break;
                case "--host":
                    if (i + 1 < args.Length)
                    {
                        host = args[++i];
                    }

                    break;
                case "--user":
                    if (i + 1 < args.Length)
                    {
                        user = args[++i];
                    }

                    break;
                case "--port":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int p))
                    {
                        port = p;
                        i++;
                    }

                    break;
                case "--key":
                case "-i":
                    if (i + 1 < args.Length)
                    {
                        keyPath = args[++i];
                    }

                    break;
                case "--help":
                case "-h":
                    return false;
            }
        }

        return true;
    }

    private void BuildSshCommand()
    {
        if (string.IsNullOrEmpty(sshCommand)==false)
        {
            // SSH command provided directly
            string cmd = $"ssh {sshCommand}";
            
            cmd += $" -p {port}";
           

            if (!string.IsNullOrEmpty(keyPath))
            {
                cmd += $" -i {keyPath}";
            }

            sshCommand = cmd;
        }
        else if (string.IsNullOrEmpty(host)==false)
        {
            if (string.IsNullOrEmpty(user))
            {
                Console.WriteLine("Error: --user is required when using --host");
                Environment.Exit(1);
            }

            string cmd = $"ssh {user}@{host}";
            
            cmd += $" -p {port}";
            

            if (!string.IsNullOrEmpty(keyPath))
            {
                cmd += $" -i {keyPath}";
            }

            sshCommand = cmd;
        }
    }

    private bool LaunchTerminalLinux()
    {
        var terminals = new[]
        {
            new { Name = "gnome-terminal", Args = "--" },
            new { Name = "konsole", Args = "-e" },
            new { Name = "xfce4-terminal", Args = "-e" },
            new { Name = "xterm", Args = "-e" },
            new { Name = "terminator", Args = "-e" }
        };

        foreach (var terminal in terminals)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = terminal.Name,
                    UseShellExecute = false
                };

                if (!string.IsNullOrEmpty(sshCommand))
                {
                    startInfo.Arguments = $"{terminal.Args} bash -c \"{sshCommand}; exec bash\"";
                }
                else
                {
                    startInfo.Arguments = $"{terminal.Args} bash";
                }

                Process.Start(startInfo);
                Console.WriteLine($"Launched {terminal.Name} successfully");
                return true;
            }
            catch
            {
                continue;
            }
        }

        Console.WriteLine("Error: No supported terminal emulator found");
        return false;
    }

    private bool LaunchTerminalMacOS()
    {
        try
        {
            string script;
            if (!string.IsNullOrEmpty(sshCommand))
            {
                // Escape quotes in the SSH command for AppleScript
                string escapedCommand = sshCommand.Replace("\"", "\\\"");
                script = $@"tell application ""Terminal""
    activate
    do script ""{escapedCommand}""
end tell";
            }
            else
            {
                script = @"tell application ""Terminal""
    activate
end tell";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            Console.WriteLine("Launched Terminal.app successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching macOS terminal: {ex.Message}");
            return false;
        }
    }

    private bool LaunchTerminalWindows()
    {
        // Try Windows Terminal first
        if (TryLaunchWindowsTerminal())
        {
            Console.WriteLine("Launched Windows Terminal successfully");
            return true;
        }

        // Try PowerShell
        if (TryLaunchPowerShell())
        {
            Console.WriteLine("Launched PowerShell successfully");
            return true;
        }

        // Try Command Prompt
        if (TryLaunchCmd())
        {
            Console.WriteLine("Launched Command Prompt successfully");
            return true;
        }

        Console.WriteLine("Error: Could not launch any terminal");
        return false;
    }

    private bool TryLaunchWindowsTerminal()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wt.exe",
                UseShellExecute = true
            };

            if (!string.IsNullOrEmpty(sshCommand))
            {
                startInfo.Arguments = $"new-tab -- powershell -NoExit -Command \"{sshCommand}\"";
            }

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryLaunchPowerShell()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true
            };

            if (!string.IsNullOrEmpty(sshCommand))
            {
                startInfo.Arguments = $"-NoExit -Command \"{sshCommand}\"";
            }

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryLaunchCmd()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = true
            };

            if (!string.IsNullOrEmpty(sshCommand))
            {
                startInfo.Arguments = $"/k {sshCommand}";
            }

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetPlatformName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        return "Unknown";
    }
    
    private void PrintUsage()
    {
        Console.WriteLine(@"
Cross-Platform Terminal Launcher with SSH

Usage:
  TerminalLauncher [options]

Options:
  --ssh <connection>    SSH connection string (e.g., user@hostname)
  --host <hostname>     SSH host
  --user <username>     SSH username
  --port <port>         SSH port (default: 22)
  -p <port>             SSH port (short form)
  --key <path>          Path to SSH private key
  -i <path>             Path to SSH private key (short form)
  --help, -h            Show this help message

Examples:
  TerminalLauncher
  TerminalLauncher --ssh user@hostname
  TerminalLauncher --ssh user@hostname -p 2222
  TerminalLauncher --host example.com --user myuser --port 22
  TerminalLauncher --ssh user@host --key ~/.ssh/id_rsa
");
    }

}