using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WebApplication1;

public class UptimeInfo
{
    public TimeSpan Uptime { get; set; }
    public int UserCount { get; set; }
    public double LoadAverage1Min { get; set; }
    public double LoadAverage5Min { get; set; }
    public double LoadAverage15Min { get; set; }
    public DateTime CurrentTime { get; set; }

    public override string ToString()
    {
        return $"Current Time: {CurrentTime:HH:mm:ss}\n" +
               $"Uptime: {Uptime.Days} days, {Uptime.Hours}:{Uptime.Minutes:D2}\n" +
               $"Users: {UserCount}\n" +
               $"Load Average: {LoadAverage1Min:F2}, {LoadAverage5Min:F2}, {LoadAverage15Min:F2}";
    }
}

public class UptimeParser
{
    public static TimeSpan FetchUptime()
    {
        var output = ExecuteUptime();
        var info = Parse(output);
        return info.Uptime;
    }

    public static string ExecuteUptime()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "uptime",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process == null) throw new Exception("Failed to start uptime process");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception($"uptime command failed with exit code {process.ExitCode}: {error}");

                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error executing uptime command: {ex.Message}", ex);
        }
    }

    public static UptimeInfo Parse(string uptimeOutput)
    {
        if (string.IsNullOrWhiteSpace(uptimeOutput))
            throw new ArgumentException("Uptime output cannot be null or empty");

        var result = new UptimeInfo();

        // Parse current time (e.g., "14:23:45")
        var timeMatch = Regex.Match(uptimeOutput, @"(\d{1,2}):(\d{2}):(\d{2})");

        // Parse uptime - handle various formats
        int days = 0, hours = 0, minutes = 0;

        if (timeMatch.Success)
        {
            hours = int.Parse(timeMatch.Groups[1].Value);
            minutes = int.Parse(timeMatch.Groups[2].Value);
            var seconds = int.Parse(timeMatch.Groups[3].Value);
            result.CurrentTime = DateTime.Today.Add(new TimeSpan(hours, minutes, seconds));
        }


        // Match "X days" or "X day"
        var daysMatch = Regex.Match(uptimeOutput, @"up\s+(\d+)\s+days?");
        if (daysMatch.Success) days = int.Parse(daysMatch.Groups[1].Value);

        // Match "X:YY" (hours:minutes) after "up" and optional days
        var timeUpMatch = Regex.Match(uptimeOutput, @"up\s+(?:\d+\s+days?,\s+)?(\d+):(\d+)");
        if (timeUpMatch.Success)
        {
            hours = int.Parse(timeUpMatch.Groups[1].Value);
            minutes = int.Parse(timeUpMatch.Groups[2].Value);
        }
        // Handle "up X min" format (when uptime is less than an hour)
        else
        {
            var minMatch = Regex.Match(uptimeOutput, @"up\s+(\d+)\s+min");
            if (minMatch.Success) minutes = int.Parse(minMatch.Groups[1].Value);
        }

        result.Uptime = new TimeSpan(days, hours, minutes, 0);

        // Parse user count
        var usersMatch = Regex.Match(uptimeOutput, @"(\d+)\s+users?");
        if (usersMatch.Success) result.UserCount = int.Parse(usersMatch.Groups[1].Value);

        // Parse load averages
        var loadMatch = Regex.Match(uptimeOutput, @"load average:\s*([\d.]+),\s*([\d.]+),\s*([\d.]+)");
        if (loadMatch.Success)
        {
            result.LoadAverage1Min = double.Parse(loadMatch.Groups[1].Value);
            result.LoadAverage5Min = double.Parse(loadMatch.Groups[2].Value);
            result.LoadAverage15Min = double.Parse(loadMatch.Groups[3].Value);
        }

        return result;
    }
}