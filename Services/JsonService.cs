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
        // [M11] Lê JSON de forma segura; se falhar, devolve lista vazia.
        var path = GetPath(fileName);
        if (!File.Exists(path)) return new List<T>();
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch (JsonException)
        {
            // [M11] JSON malformado não deve derrubar páginas públicas.
            return new List<T>();
        }
    }

    public Dictionary<string, T> LoadDictionary<T>(string fileName)
    {
        // [M11] Dicionários vazios evitam erro quando scores/users não existem.
        var path = GetPath(fileName);
        if (!File.Exists(path)) return new Dictionary<string, T>();
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, T>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, T>>(json) ?? new Dictionary<string, T>();
        }
        catch (JsonException)
        {
            // [M11] JSON malformado não deve causar erro 500.
            return new Dictionary<string, T>();
        }
    }

    public void Save<T>(string fileName, T data)
    {
        // [M11] Grava primeiro em .tmp para reduzir risco de ficheiro corrompido.
        var path = GetPath(fileName);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, true);
    }

    private string GetPath(string fileName)
    {
        return Path.Combine(_dataDir, fileName);
    }
}
