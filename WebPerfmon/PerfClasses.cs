using ServiceData;

namespace WebApplication1;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Management;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class SystemStatsController : ControllerBase
{
    private readonly IApiKeyValidationService _apiKeyValidationService;

    public SystemStatsController(IApiKeyValidationService apiKeyValidationService)
    {
        _apiKeyValidationService = apiKeyValidationService;
        
    }
    [HttpGet("system")]
    public async Task<ActionResult<SystemStats>> GetSystemStats()
    {
        if (!await ValidateApiKey())
            return Unauthorized(new { error = "Invalid or missing API key" });

        try
        {
            var stats = new SystemStats
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = await GetCpuUsageAsync(),
                MemoryUsage = GetMemoryUsage(),
                GpuStats = await GetGpuStatsAsync()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("cpu")]
    public async Task<ActionResult<CpuStats>> GetCpuStats()
    {
        if (!await ValidateApiKey())
            return Unauthorized(new { error = "Invalid or missing API key" });

        try
        {
            var cpuStats = await GetCpuUsageAsync();
            return Ok(cpuStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("memory")]
    public async Task<ActionResult<MemoryStats>> GetMemoryStats()
    {
        if (!await ValidateApiKey())
            return Unauthorized(new { error = "Invalid or missing API key" });

        try
        {
            var memoryStats = GetMemoryUsage();
            return Ok(memoryStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("gpu")]
    public async Task<ActionResult<List<GpuStats>>> GetGpuStats()
    {
        if (!await ValidateApiKey())
            return Unauthorized(new { error = "Invalid or missing API key" });

        try
        {
            var gpuStats = await GetGpuStatsAsync();
            return Ok(gpuStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<bool> ValidateApiKey()
    {
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(apiKey))
            return false;

        return await _apiKeyValidationService.IsValidApiKeyAsync(apiKey);
    }

    private async Task<CpuStats> GetCpuUsageAsync()
    {
        double cpuUsage = 0;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            cpuUsage = await GetWindowsCpuUsageAsync();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            cpuUsage = await GetLinuxCpuUsageAsync();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            cpuUsage = await GetMacOsCpuUsageAsync();
        }

        return new CpuStats
        {
            UsagePercentage = Math.Round(cpuUsage, 2),
            CoreCount = Environment.ProcessorCount,
            ProcessorName = GetProcessorName()
        };
    }

    private async Task<double> GetWindowsCpuUsageAsync()
    {
        try
        {
            // Use WMI for Windows
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            var cpuUsages = new List<double>();
            
            foreach (ManagementObject obj in searcher.Get())
            {
                var usage = Convert.ToDouble(obj["LoadPercentage"]);
                cpuUsages.Add(usage);
            }
            
            return cpuUsages.Any() ? cpuUsages.Average() : 0;
        }
        catch
        {
            // Fallback to process-based calculation
            return await GetProcessBasedCpuUsageAsync();
        }
    }

    private async Task<double> GetLinuxCpuUsageAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = await ReadLinuxCpuUsageAsync();
            
            await Task.Delay(1000); // Wait 1 second
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = await ReadLinuxCpuUsageAsync();
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage) / 1000; // Convert to milliseconds
            var totalTimeMs = (endTime - startTime).TotalMilliseconds;
            var cpuUsagePercentage = (cpuUsedMs / (Environment.ProcessorCount * totalTimeMs)) * 100;
            
            return Math.Max(0, Math.Min(100, cpuUsagePercentage));
        }
        catch
        {
            return await GetProcessBasedCpuUsageAsync();
        }
    }

    private async Task<long> ReadLinuxCpuUsageAsync()
    {
        try
        {
            var stat = await System.IO.File.ReadAllTextAsync("/proc/stat");
            var line = stat.Split('\n')[0]; // First line contains overall CPU stats
            var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (values.Length > 4)
            {
                // user + nice + system + idle + iowait + irq + softirq
                var totalTime = 0L;
                for (int i = 1; i < Math.Min(8, values.Length); i++)
                {
                    if (long.TryParse(values[i], out var val))
                        totalTime += val;
                }
                return totalTime;
            }
        }
        catch { }
        
        return 0;
    }

    private async Task<double> GetMacOsCpuUsageAsync()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "top";
            process.StartInfo.Arguments = "-l 1 -n 0";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse CPU usage from top command output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("CPU usage:"))
                {
                    // Extract percentage from line like "CPU usage: 12.34% user, 5.67% sys, 81.99% idle"
                    var parts = line.Split(',');
                    var userPart = parts[0].Replace("CPU usage:", "").Replace("%", "").Replace("user", "").Trim();
                    var sysPart = parts[1].Replace("%", "").Replace("sys", "").Trim();
                    
                    if (double.TryParse(userPart, out var user) && double.TryParse(sysPart, out var sys))
                    {
                        return user + sys;
                    }
                }
            }
        }
        catch { }

        return await GetProcessBasedCpuUsageAsync();
    }

    private async Task<double> GetProcessBasedCpuUsageAsync()
    {
        var startTime = DateTime.UtcNow;
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        
        await Task.Delay(500);
        
        var endTime = DateTime.UtcNow;
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        
        return cpuUsageTotal * 100;
    }

    private MemoryStats GetMemoryUsage()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var totalMemoryBytes = gcMemoryInfo.TotalAvailableMemoryBytes;
        var totalMemoryMB = totalMemoryBytes / (1024.0 * 1024.0);

        // Get working set (current process memory usage)
        var currentProcess = Process.GetCurrentProcess();
        var workingSetMB = currentProcess.WorkingSet64 / (1024.0 * 1024.0);

        // Try to get system-wide memory info
        var systemMemory = GetSystemMemoryInfo();
        
        return new MemoryStats
        {
            TotalMemoryMB = Math.Round(systemMemory.TotalMB > 0 ? systemMemory.TotalMB : totalMemoryMB, 2),
            AvailableMemoryMB = Math.Round(systemMemory.AvailableMB, 2),
            UsedMemoryMB = Math.Round(systemMemory.TotalMB - systemMemory.AvailableMB, 2),
            UsagePercentage = Math.Round(systemMemory.TotalMB > 0 ? ((systemMemory.TotalMB - systemMemory.AvailableMB) / systemMemory.TotalMB) * 100 : 0, 2),
            ProcessWorkingSetMB = Math.Round(workingSetMB, 2)
        };
    }

    private (double TotalMB, double AvailableMB) GetSystemMemoryInfo()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemoryInfo();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemoryInfo();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOsMemoryInfo();
            }
        }
        catch { }

        return (0, 0);
    }

    private (double TotalMB, double AvailableMB) GetWindowsMemoryInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, AvailablePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalKB = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                var availableKB = Convert.ToDouble(obj["AvailablePhysicalMemory"]);
                return (totalKB / 1024, availableKB / 1024);
            }
        }
        catch { }

        return (0, 0);
    }

    private (double TotalMB, double AvailableMB) GetLinuxMemoryInfo()
    {
        try
        {
            var memInfo = System.IO.File.ReadAllText("/proc/meminfo");
            var lines = memInfo.Split('\n');
            
            double totalKB = 0, availableKB = 0, freeKB = 0, buffersKB = 0, cachedKB = 0;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                    double.TryParse(line.Split()[1], out totalKB);
                else if (line.StartsWith("MemAvailable:"))
                    double.TryParse(line.Split()[1], out availableKB);
                else if (line.StartsWith("MemFree:"))
                    double.TryParse(line.Split()[1], out freeKB);
                else if (line.StartsWith("Buffers:"))
                    double.TryParse(line.Split()[1], out buffersKB);
                else if (line.StartsWith("Cached:"))
                    double.TryParse(line.Split()[1], out cachedKB);
            }
            
            // If MemAvailable is not available, estimate it
            if (availableKB == 0)
                availableKB = freeKB + buffersKB + cachedKB;
            
            return (totalKB / 1024, availableKB / 1024);
        }
        catch { }

        return (0, 0);
    }

    private (double TotalMB, double AvailableMB) GetMacOsMemoryInfo()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "vm_stat";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse vm_stat output to get memory info
            // This is a simplified version - you might want to enhance this
            return (0, 0); // Placeholder - implement proper parsing
        }
        catch { }

        return (0, 0);
    }

    private async Task<List<GpuStats>> GetGpuStatsAsync()
    {
        var gpuStatsList = new List<GpuStats>();
        
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "nvidia-smi";
            process.StartInfo.Arguments = "--query-gpu=index,name,temperature.gpu,utilization.gpu,utilization.memory,memory.total,memory.used,memory.free,power.draw,power.limit --format=csv,noheader,nounits";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"nvidia-smi failed with exit code {process.ExitCode}: {error}");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return gpuStatsList;
            }

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var values = line.Split(',').Select(v => v.Trim()).ToArray();
                
                if (values.Length >= 10)
                {
                    var gpuStats = new GpuStats
                    {
                        Index = int.TryParse(values[0], out var index) ? index : 0,
                        Name = values[1],
                        TemperatureCelsius = double.TryParse(values[2], out var temp) ? temp : 0,
                        UtilizationPercentage = double.TryParse(values[3], out var util) ? util : 0,
                        MemoryUtilizationPercentage = double.TryParse(values[4], out var memUtil) ? memUtil : 0,
                        TotalMemoryMB = double.TryParse(values[5], out var totalMem) ? totalMem : 0,
                        UsedMemoryMB = double.TryParse(values[6], out var usedMem) ? usedMem : 0,
                        FreeMemoryMB = double.TryParse(values[7], out var freeMem) ? freeMem : 0,
                        PowerDrawWatts = double.TryParse(values[8], out var power) ? power : 0,
                        PowerLimitWatts = double.TryParse(values[9], out var powerLimit) ? powerLimit : 0
                    };
                    
                    gpuStatsList.Add(gpuStats);
                }
            }
        }
        catch (Exception ex)
        {
            gpuStatsList.Add(new GpuStats
            {
                Name = "Error",
                ErrorMessage = $"Failed to get GPU stats: {ex.Message}"
            });
        }

        return gpuStatsList;
    }

    private string GetProcessorName()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString() ?? "Unknown";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var cpuInfo = System.IO.File.ReadAllText("/proc/cpuinfo");
                var modelLine = cpuInfo.Split('\n').FirstOrDefault(line => line.StartsWith("model name"));
                return modelLine?.Split(':')[1].Trim() ?? "Unknown";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "-n machdep.cpu.brand_string",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                
                if (process != null)
                {
                    var result = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    return string.IsNullOrEmpty(result) ? "Unknown" : result;
                }
            }
        }
        catch { }

        return "Unknown";
    }
}
