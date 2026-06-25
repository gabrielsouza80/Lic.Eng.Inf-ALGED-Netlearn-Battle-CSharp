namespace NetLearnBattle.CSharp.Tests;

public class JsonServiceTests
{
    [Fact]
    public void LoadList_WhenFileDoesNotExist_ReturnsEmptyList()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var result = json.LoadList<string>("nonexistent.json");
            Assert.Empty(result);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void SaveAndLoadList_RoundTrip()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var data = new List<string> { "alfa", "beta", "gama" };
            json.Save("testlist.json", data);
            var loaded = json.LoadList<string>("testlist.json");
            Assert.Equal(3, loaded.Count);
            Assert.Contains("alfa", loaded);
            Assert.Contains("beta", loaded);
            Assert.Contains("gama", loaded);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void LoadDictionary_WhenFileDoesNotExist_ReturnsEmpty()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var result = json.LoadDictionary<int>("missing.json");
            Assert.Empty(result);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void SaveAndLoadDictionary_RoundTrip()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            var data = new Dictionary<string, int> { { "alice", 100 }, { "bob", 50 } };
            json.Save("scores.json", data);
            var loaded = json.LoadDictionary<int>("scores.json");
            Assert.Equal(2, loaded.Count);
            Assert.Equal(100, loaded["alice"]);
            Assert.Equal(50, loaded["bob"]);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void Save_CreatesFileInDataDir()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            json.Save("test.txt", "content");
            var filePath = Path.Combine(dir, "test.txt");
            Assert.True(File.Exists(filePath));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void LoadList_WhenFileIsEmpty_ReturnsEmptyList()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        try
        {
            File.WriteAllText(Path.Combine(dir, "empty.json"), string.Empty);
            Assert.Empty(json.LoadList<string>("empty.json"));
        }
        finally { TestHelpers.Cleanup(dir); }
    }
}
