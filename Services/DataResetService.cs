namespace NetLearnBattle.CSharp.Services;

public class DataResetService
{
    private readonly string _dataDir;

    // [M65] Reset remove apenas dados locais gerados pela aplicação.
    private static readonly string[] LocalFiles =
    {
        "users.json",
        "scores.json",
        "attempts.json",
        "sessions.json",
        "users.json.tmp",
        "scores.json.tmp",
        "attempts.json.tmp",
        "sessions.json.tmp",
    };

    public DataResetService(IWebHostEnvironment env)
    {
        _dataDir = Path.Combine(env.ContentRootPath, "Data");
    }

    public DataResetService(string basePath)
    {
        _dataDir = basePath;
    }

    public List<string> ResetLocalData()
    {
        // [M67] Lista fechada: apenas estes ficheiros locais podem ser apagados.
        var removed = new List<string>();

        foreach (var fileName in LocalFiles)
        {
            var path = Path.Combine(_dataDir, fileName);
            if (!File.Exists(path))
                continue;

            File.Delete(path);
            removed.Add($"Data/{fileName}");
        }

        // [M62] questions.json, acls.json e examples nunca entram no reset.
        return removed;
    }
}
