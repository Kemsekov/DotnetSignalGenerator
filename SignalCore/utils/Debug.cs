using System.Text.Json;

public static class DebugExtensions
{
    static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public static void Dump(this object obj)
    {
        // Serializes the object/collection to a pretty-printed JSON string and prints it
        Console.WriteLine(obj.json());
    }

    public static string json(this object obj)
    {
        // Serializes the object/collection to a pretty-printed JSON string and prints it
        return JsonSerializer.Serialize(obj, _options);
    }
}