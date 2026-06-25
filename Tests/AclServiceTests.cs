namespace NetLearnBattle.CSharp.Tests;

// [M37] Testes das regras ACL.
public class AclServiceTests
{
    private static (string dir, JsonService json) CreateWithAcls()
    {
        var (dir, json) = TestHelpers.CreateTempJsonService();
        var acls = new List<object>
        {
            new
            {
                id = "test1",
                description = "HTTP permit scenario",
                packet = new { src_ip = "10.0.0.5", dst_ip = "192.168.1.10", protocol = "tcp", port = 80 },
                rules = new[]
                {
                    new { id = "R1", action = "permit", protocol = "tcp", src = "any", dst = "192.168.1.10/32", port = "80" },
                    new { id = "R2", action = "deny", protocol = "ip", src = "any", dst = "any", port = "any" }
                }
            },
            new
            {
                id = "test2",
                description = "SSH permit scenario",
                packet = new { src_ip = "10.0.0.5", dst_ip = "192.168.1.10", protocol = "tcp", port = 22 },
                rules = new[]
                {
                    new { id = "R1", action = "permit", protocol = "tcp", src = "any", dst = "192.168.1.10/32", port = "22" },
                    new { id = "R2", action = "deny", protocol = "ip", src = "any", dst = "any", port = "any" }
                }
            }
        };
        var jsonStr = System.Text.Json.JsonSerializer.Serialize(acls);
        File.WriteAllText(Path.Combine(dir, "acls.json"), jsonStr);
        return (dir, json);
    }

    // ---- RuleMatchesPacket ----

    [Fact]
    public void RuleMatchesPacket_AnyProtocol_Matches()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "any", Src = "any", Dst = "any", Port = "any" };
            var packet = new Packet { Protocol = "tcp", SrcIp = "10.0.0.1", DstIp = "192.168.1.1", Port = 80 };
            Assert.True(acl.RuleMatchesPacket(rule, packet));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void RuleMatchesPacket_IpProtocol_MatchesTcp()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "ip", Src = "any", Dst = "any", Port = "any" };
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "tcp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 80 }));
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "udp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 53 }));
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "icmp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 0 }));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void RuleMatchesPacket_TcpDoesNotMatchUdp()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "tcp", Src = "any", Dst = "any", Port = "any" };
            Assert.False(acl.RuleMatchesPacket(rule, new Packet { Protocol = "udp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 53 }));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void RuleMatchesPacket_SourceAny_MatchesAny()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "ip", Src = "any", Dst = "192.168.1.10/32", Port = "any" };
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "tcp", SrcIp = "99.99.99.99", DstIp = "192.168.1.10", Port = 80 }));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void RuleMatchesPacket_DestinationAny_MatchesAny()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "ip", Src = "any", Dst = "any", Port = "any" };
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "tcp", SrcIp = "1.1.1.1", DstIp = "9.9.9.9", Port = 99 }));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void RuleMatchesPacket_ExactPort_Matches()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "tcp", Src = "any", Dst = "any", Port = "80" };
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "tcp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 80 }));
            Assert.False(acl.RuleMatchesPacket(rule, new Packet { Protocol = "tcp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 8080 }));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void RuleMatchesPacket_AnyPort_Matches()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "tcp", Src = "any", Dst = "any", Port = "any" };
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "tcp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 9999 }));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void RuleMatchesPacket_EmptyPort_Matches()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rule = new AclRule { Action = "permit", Protocol = "tcp", Src = "any", Dst = "any", Port = "" };
            Assert.True(acl.RuleMatchesPacket(rule, new Packet { Protocol = "tcp", SrcIp = "1.1.1.1", DstIp = "2.2.2.2", Port = 1234 }));
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    // ---- EvaluateAcl ----

    [Fact]
    public void EvaluateAcl_FirstMatchApplied()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rules = new List<AclRule>
            {
                new() { Id = "R1", Action = "permit", Protocol = "tcp", Src = "any", Dst = "192.168.1.10/32", Port = "80" },
                new() { Id = "R2", Action = "deny", Protocol = "ip", Src = "any", Dst = "any", Port = "any" }
            };
            var packet = new Packet { Protocol = "tcp", SrcIp = "10.0.0.5", DstIp = "192.168.1.10", Port = 80 };
            var result = acl.EvaluateAcl(rules, packet);
            Assert.Equal("permit", result);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void EvaluateAcl_DefaultDeny_WhenNoRuleMatches()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rules = new List<AclRule>
            {
                new() { Id = "R1", Action = "permit", Protocol = "tcp", Src = "any", Dst = "192.168.1.10/32", Port = "80" }
            };
            var packet = new Packet { Protocol = "udp", SrcIp = "10.0.0.5", DstIp = "192.168.1.10", Port = 53 };
            var result = acl.EvaluateAcl(rules, packet);
            Assert.Equal("deny", result);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void EvaluateAcl_RuleOrder_Matters()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var rules = new List<AclRule>
            {
                new() { Id = "R1", Action = "deny", Protocol = "ip", Src = "any", Dst = "any", Port = "any" },
                new() { Id = "R2", Action = "permit", Protocol = "tcp", Src = "any", Dst = "192.168.1.10/32", Port = "80" }
            };
            var packet = new Packet { Protocol = "tcp", SrcIp = "10.0.0.5", DstIp = "192.168.1.10", Port = 80 };
            var result = acl.EvaluateAcl(rules, packet);
            Assert.Equal("deny", result);
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    // ---- GenerateAclQuestion ----

    [Fact]
    public void GenerateAclQuestion_GeneratesPermitDeny()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            // Call multiple times to cycle through all 5 types
            for (int i = 0; i < 20; i++)
            {
                var q = acl.GenerateAclQuestion();
                Assert.Equal(5, q.Level);
                Assert.Contains("ACL", q.Topic);
                Assert.InRange(q.CorrectIndex, 0, q.Options.Count - 1);
                Assert.True(q.PointsCorrect > 0);
                Assert.True(q.PointsWrong < 0);
            }
        }
        finally { TestHelpers.Cleanup(dir); }
    }

    [Fact]
    public void GenerateAclQuestion_All5TypesAppear()
    {
        var (dir, json) = CreateWithAcls();
        try
        {
            var acl = new AclService(json);
            var types = new HashSet<string>();
            for (int i = 0; i < 10; i++)
            {
                var q = acl.GenerateAclQuestion();
                types.Add(q.QuestionType ?? q.Topic);
            }
            // Should have at least 4 different types in 10 iterations
            Assert.True(types.Count >= 4, $"Expected at least 4 types, got {types.Count}");
        }
        finally { TestHelpers.Cleanup(dir); }
    }
}
