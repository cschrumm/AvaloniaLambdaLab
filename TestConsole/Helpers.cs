using System.Text.Json;

namespace TestConsole;

class Data
{
    public string InstanceType { get; set; } = "";
}
class Helpers
{
    public static void SaveData(Data d)
    {
        // use json serialization to save the data to a file.
        var jsn = JsonSerializer.Serialize(d);
        System.IO.File.WriteAllText("data.json", jsn);
    }
}