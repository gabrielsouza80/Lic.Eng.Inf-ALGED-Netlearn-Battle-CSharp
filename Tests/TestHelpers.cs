using NetLearnBattle.CSharp.Services;

namespace NetLearnBattle.CSharp.Tests;

public static class TestHelpers
{
    public static (string dir, JsonService json) CreateTempJsonService()
    {
        var dir = Path.Combine(Path.GetTempPath(), "NetLearnBattleTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var json = new JsonService(dir);
        return (dir, json);
    }

    public static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, true); }
            catch { }
        }
    }
}
