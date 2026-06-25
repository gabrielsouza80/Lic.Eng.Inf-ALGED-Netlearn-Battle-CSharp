namespace NetLearnBattle.CSharp.Tests;

public class IpServiceTests
{
    private readonly IpService _ip = new();

    // ---- IPv4 Network ID ----

    [Theory]
    [InlineData("192.168.1.10", 24, "192.168.1.0")]
    [InlineData("10.20.30.40", 8, "10.0.0.0")]
    [InlineData("172.16.5.10", 16, "172.16.0.0")]
    [InlineData("10.20.30.40", 16, "10.20.0.0")]
    public void CalculateIpv4NetworkId_ReturnsCorrect(string ip, int prefix, string expected)
    {
        var result = _ip.CalculateIpv4NetworkId(ip, prefix);
        Assert.Equal(expected, result);
    }

    // ---- IPv4 Broadcast ----

    [Theory]
    [InlineData("192.168.1.10", 24, "192.168.1.255")]
    [InlineData("10.0.0.1", 8, "10.255.255.255")]
    [InlineData("172.16.5.10", 16, "172.16.255.255")]
    public void CalculateIpv4Broadcast_StandardPrefixes(string ip, int prefix, string expected)
    {
        var result = _ip.CalculateIpv4Broadcast(ip, prefix);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("192.168.1.100", 25, "192.168.1.127")]
    [InlineData("192.168.1.100", 26, "192.168.1.127")]
    [InlineData("192.168.1.200", 27, "192.168.1.223")]
    [InlineData("10.20.30.40", 21, "10.20.31.255")]
    [InlineData("10.20.30.40", 22, "10.20.31.255")]
    [InlineData("10.20.30.40", 23, "10.20.31.255")]
    public void CalculateIpv4Broadcast_NonMultipleOf8(string ip, int prefix, string expected)
    {
        var result = _ip.CalculateIpv4Broadcast(ip, prefix);
        Assert.Equal(expected, result);
    }

    // ---- SameIpv4Network ----

    [Fact]
    public void SameIpv4Network_SameNetwork_ReturnsTrue()
    {
        Assert.True(_ip.SameIpv4Network("192.168.1.10", "192.168.1.20", 24));
    }

    [Fact]
    public void SameIpv4Network_DifferentNetwork_ReturnsFalse()
    {
        Assert.False(_ip.SameIpv4Network("192.168.1.10", "192.168.2.10", 24));
    }

    // ---- GenerateIpv4Question ----

    [Fact]
    public void GenerateIpv4Question_Level1_UsesCorrectPrefixes()
    {
        for (int i = 0; i < 30; i++)
        {
            var q = _ip.GenerateIpv4Question(1);
            Assert.Contains("IPv4", q.Topic);
            Assert.True(q.CorrectIndex >= 0 && q.CorrectIndex < q.Options.Count);
            Assert.True(q.PointsCorrect > 0);
            Assert.True(q.PointsWrong < 0);
        }
    }

    [Fact]
    public void GenerateIpv4Question_Level2_UsesSubnetPrefixes()
    {
        for (int i = 0; i < 30; i++)
        {
            var q = _ip.GenerateIpv4Question(2);
            Assert.Contains("Sub-redes", q.Topic);
            Assert.True(q.CorrectIndex >= 0 && q.CorrectIndex < q.Options.Count);
        }
    }

    [Fact]
    public void GenerateIpv4Question_Level3_UsesSupernetPrefixes()
    {
        for (int i = 0; i < 30; i++)
        {
            var q = _ip.GenerateIpv4Question(3);
            Assert.Contains("Super-redes", q.Topic);
            Assert.True(q.CorrectIndex >= 0 && q.CorrectIndex < q.Options.Count);
        }
    }

    [Fact]
    public void GenerateIpv4Question_OptionsAreNotEmpty()
    {
        var q = _ip.GenerateIpv4Question(1);
        Assert.NotEmpty(q.Options);
        foreach (var opt in q.Options)
            Assert.NotEmpty(opt);
    }

    [Fact]
    public void GenerateIpv4Question_CorrectIndexInRange()
    {
        for (int i = 0; i < 50; i++)
        {
            var q = _ip.GenerateIpv4Question(1);
            Assert.InRange(q.CorrectIndex, 0, q.Options.Count - 1);
        }
    }

    // ---- IPv6 ----

    [Fact]
    public void CalculateIpv6NetworkId_ReturnsCorrect()
    {
        var result = _ip.CalculateIpv6NetworkId("2001:db8:abcd:1234::1", 48);
        Assert.Contains("2001:db8:abcd", result.ToLowerInvariant());
    }

    [Fact]
    public void SameIpv6Network_SameNetwork_ReturnsTrue()
    {
        Assert.True(_ip.SameIpv6Network("2001:db8:abcd:1234::1", "2001:db8:abcd:5678::1", 48));
    }

    [Fact]
    public void SameIpv6Network_DifferentNetwork_ReturnsFalse()
    {
        Assert.False(_ip.SameIpv6Network("2001:db8:abcd:1234::1", "2001:db9:1234:5678::1", 48));
    }

    [Fact]
    public void GenerateIpv6Question_ReturnsValid()
    {
        for (int i = 0; i < 20; i++)
        {
            var q = _ip.GenerateIpv6Question();
            Assert.Equal(4, q.Level);
            Assert.Equal("IPv6", q.Topic);
            Assert.InRange(q.CorrectIndex, 0, q.Options.Count - 1);
            Assert.True(q.PointsCorrect > 0);
            Assert.True(q.PointsWrong < 0);
        }
    }

    [Fact]
    public void Ipv6BroadcastQuestion_StatesNoBroadcast()
    {
        // Force the broadcast concept question type by calling many times
        // and looking for the broadcast question
        bool found = false;
        for (int i = 0; i < 100; i++)
        {
            var q = _ip.GenerateIpv6Question();
            if (q.QuestionText.Contains("broadcast", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(0, q.CorrectIndex);
                Assert.Contains("não usa broadcast", q.Options[0].ToLowerInvariant());
                found = true;
                break;
            }
        }
        Assert.True(found, "Broadcast concept question should appear in 100 iterations");
    }
}
