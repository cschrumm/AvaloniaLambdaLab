using System.Text.Json.Serialization;

namespace Service.Library;


[JsonSerializable(typeof(SystemStats))]
[JsonSerializable(typeof(CpuStats))]
[JsonSerializable(typeof(MemoryStats))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}


public class SystemStats
{
    public DateTime Timestamp { get; set; }
    public CpuStats CpuUsage { get; set; } = new();
    public MemoryStats MemoryUsage { get; set; } = new();
    public List<GpuStats> GpuStats { get; set; } = new();
}


public class CpuStats
{
    public double UsagePercentage { get; set; }
    public int CoreCount { get; set; }
    public string ProcessorName { get; set; } = string.Empty;
}


public class MemoryStats
{
    public double TotalMemoryMB { get; set; }
    public double AvailableMemoryMB { get; set; }
    public double UsedMemoryMB { get; set; }
    public double UsagePercentage { get; set; }
    public double ProcessWorkingSetMB { get; set; }
}

[JsonSerializable(typeof(GpuStats))]
public class GpuStats
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public double TemperatureCelsius { get; set; }
    public double UtilizationPercentage { get; set; }
    public double MemoryUtilizationPercentage { get; set; }
    public double TotalMemoryMB { get; set; }
    public double UsedMemoryMB { get; set; }
    public double FreeMemoryMB { get; set; }
    public double PowerDrawWatts { get; set; }
    public double PowerLimitWatts { get; set; }
    public string? ErrorMessage { get; set; }
}
