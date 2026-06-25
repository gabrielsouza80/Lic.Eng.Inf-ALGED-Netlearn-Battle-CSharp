using System.Text.Json;

namespace NetLearnBattle.CSharp.Services;

public class JsonService
{
    private readonly string _dataDir;

    public JsonService(IWebHostEnvironment env)
    {
        _dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(_dataDir);
    }

    public JsonService(string basePath)
    {
        _dataDir = basePath;
        Directory.CreateDirectory(_dataDir);
    }

    public List<T> LoadList<T>(string fileName)
    {
        var path = GetPath(fileName);
        if (!File.Exists(path)) return new List<T>();
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();
        return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }

    public Dictionary<string, T> LoadDictionary<T>(string fileName)
    {
        var path = GetPath(fileName);
        if (!File.Exists(path)) return new Dictionary<string, T>();
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, T>();
        return JsonSerializer.Deserialize<Dictionary<string, T>>(json) ?? new Dictionary<string, T>();
    }

    public void Save<T>(string fileName, T data)
    {
        var path = GetPath(fileName);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private string GetPath(string fileName)
    {
        return Path.Combine(_dataDir, fileName);
    }
}
