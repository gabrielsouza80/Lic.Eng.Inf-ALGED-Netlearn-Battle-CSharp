using System.Net;
using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class AclService
{
    private readonly JsonService _json;
    private static readonly Random Rng = new();
    private int _nextQuestionType;

    public AclService(JsonService json)
    {
        _json = json;
    }

    public bool RuleMatchesPacket(AclRule rule, Packet packet)
    {
        return ProtocolMatches(rule.Protocol, packet.Protocol)
            && IpMatches(rule.Src, packet.SrcIp)
            && IpMatches(rule.Dst, packet.DstIp)
            && PortMatches(rule.Port, packet.Port);
    }

    public AclEvaluation EvaluateAclDetails(List<AclRule> rules, Packet packet)
    {
        for (var index = 0; index < rules.Count; index++)
        {
            if (rules[index] == null)
                continue;

            if (RuleMatchesPacket(rules[index], packet))
            {
                return new AclEvaluation
                {
                    Action = rules[index].Action,
                    MatchedRule = rules[index],
                    MatchedRuleIndex = index
                };
            }
        }

        return new AclEvaluation();
    }

    public string EvaluateAcl(List<AclRule> rules, Packet packet)
    {
        return EvaluateAclDetails(rules, packet).Action ?? "deny";
    }

    public Question GenerateAclQuestion()
    {
        var scenarios = _json.LoadList<AclScenario>("acls.json");
        var type = _nextQuestionType;
        _nextQuestionType = (_nextQuestionType + 1) % 5;

        return type switch
        {
            0 => GeneratePermitDenyQuestion(scenarios),
            1 => GenerateFirstMatchQuestion(scenarios),
            2 => GenerateOrderQuestion(),
            3 => GenerateMissingAceQuestion(),
            _ => GenerateServerAclQuestion(),
        };
    }

    private Question GeneratePermitDenyQuestion(List<AclScenario> scenarios)
    {
        var scenario = GetScenario(scenarios);
        var evaluation = EvaluateAclDetails(scenario.Rules, scenario.Packet);
        var correct = string.Equals(evaluation.Action, "permit", StringComparison.OrdinalIgnoreCase)
            ? "Permit" : "Deny";

        var rulesText = FormatRules(scenario.Rules);
        var text = $"Com base nesta ACL e neste pacote, o tráfego será permitido ou bloqueado?\n\n" +
                   $"ACL:\n{rulesText}\n\nPacote: {FormatPacket(scenario.Packet)}";

        return CreateQuestion("ACL permit/deny", text,
            new List<string> { "Permit", "Deny" }, correct);
    }

    private Question GenerateFirstMatchQuestion(List<AclScenario> scenarios)
    {
        var scenario = GetScenario(scenarios);
        var evaluation = EvaluateAclDetails(scenario.Rules, scenario.Packet);
        var options = scenario.Rules.Select((rule, index) =>
                $"Regra {index + 1} — {FormatRule(rule)}")
            .ToList();

        const string defaultDeny = "Nenhuma regra corresponde; aplica-se deny padrão.";
        options.Add(defaultDeny);

        while (options.Count < 4)
            options.Add($"Regra extra — permit udp any 192.168.99.{options.Count} port 53");

        var correct = evaluation.MatchedRuleIndex >= 0
            ? $"Regra {evaluation.MatchedRuleIndex + 1} — {FormatRule(evaluation.MatchedRule!)}"
            : defaultDeny;

        var text = $"Qual é a primeira regra da ACL que corresponde ao pacote?\n\n" +
                   $"ACL:\n{FormatRules(scenario.Rules)}\n\nPacote: {FormatPacket(scenario.Packet)}";

        return CreateQuestion("ACL primeira regra", text, options, correct);
    }

    private Question GenerateOrderQuestion()
    {
        const string permitHttp = "permit tcp any 192.168.1.10/32 port 80";
        const string denyAll = "deny ip any any";
        const string wrongPort = "permit tcp any 192.168.1.10/32 port 22";

        var correct = $"1. {permitHttp}\n2. {denyAll}";
        var options = new List<string>
        {
            correct,
            $"1. {denyAll}\n2. {permitHttp}",
            $"1. {wrongPort}\n2. {denyAll}",
            $"1. {permitHttp}\n2. permit ip any any"
        };

        var text = "Qual é a ordem correta das regras para permitir HTTP para " +
                   "192.168.1.10 e bloquear o restante tráfego?\n\n" +
                   "A ACL aplica a primeira regra compatível.";

        return CreateQuestion("ACL ordenação", text, options, correct);
    }

    private Question GenerateMissingAceQuestion()
    {
        const string correct = "permit tcp any 192.168.1.10/32 port 80";
        var options = new List<string>
        {
            correct,
            "permit udp any 192.168.1.10/32 port 53",
            "deny tcp any 192.168.1.10/32 port 80",
            "permit tcp any 192.168.1.20/32 port 80"
        };

        var text = "Objetivo: permitir acesso HTTP ao servidor 192.168.1.10 e " +
                   "depois bloquear o restante.\n\nACL incompleta:\n1. deny ip any any\n\n" +
                   "Qual ACE deve ser adicionada antes do deny?";

        return CreateQuestion("ACL ACE em falta", text, options, correct);
    }

    private Question GenerateServerAclQuestion()
    {
        const string correct = "1. permit tcp any 192.168.1.10/32 port 80\n2. deny ip any any";
        var options = new List<string>
        {
            correct,
            "1. deny ip any any\n2. permit tcp any 192.168.1.10/32 port 80",
            "1. permit udp any 192.168.1.10/32 port 80\n2. deny ip any any",
            "1. permit tcp any 192.168.1.20/32 port 80\n2. deny ip any any"
        };

        var text = "Qual ACL permite HTTP (porta 80) para o servidor 192.168.1.10 " +
                   "e bloqueia o restante tráfego?";

        return CreateQuestion("ACL servidor", text, options, correct);
    }

    private static Question CreateQuestion(string topic, string text,
        List<string> options, string correct)
    {
        Shuffle(options);

        return new Question
        {
            Level = 5,
            Topic = topic,
            QuestionText = text,
            Options = options,
            CorrectIndex = options.IndexOf(correct),
            PointsCorrect = 50,
            PointsWrong = -25,
            QuestionType = topic
        };
    }

    private static AclScenario GetScenario(List<AclScenario> scenarios)
    {
        if (scenarios.Count > 0)
        {
            var scenario = scenarios[Rng.Next(scenarios.Count)] ?? new AclScenario();
            scenario.Packet ??= new Packet();
            scenario.Rules ??= new List<AclRule>();
            return scenario;
        }

        return new AclScenario
        {
            Packet = new Packet
            {
                SrcIp = "10.0.0.5",
                DstIp = "192.168.1.10",
                Protocol = "tcp",
                Port = 80
            },
            Rules = new List<AclRule>
            {
                new() { Id = "R1", Action = "permit", Protocol = "tcp", Src = "any", Dst = "192.168.1.10/32", Port = "80" },
                new() { Id = "R2", Action = "deny", Protocol = "ip", Src = "any", Dst = "any", Port = "any" }
            }
        };
    }

    private static string FormatRules(List<AclRule> rules)
    {
        if (rules.Count == 0)
            return "(sem regras; aplica-se deny padrão)";

        return string.Join("\n", rules.Select((rule, index) =>
            $"{index + 1}. {FormatRule(rule)}"));
    }

    private static string FormatRule(AclRule rule)
    {
        var port = string.IsNullOrWhiteSpace(rule.Port) || rule.Port == "0"
            ? "any" : rule.Port;
        return $"{rule.Action} {rule.Protocol} {rule.Src} {rule.Dst} port {port}";
    }

    private static string FormatPacket(Packet packet)
    {
        return $"{packet.Protocol} de {packet.SrcIp} para {packet.DstIp}, porta {packet.Port}";
    }

    private static bool ProtocolMatches(string ruleProtocol, string packetProtocol)
    {
        var rule = (ruleProtocol ?? string.Empty).Trim().ToLowerInvariant();
        var packet = (packetProtocol ?? string.Empty).Trim().ToLowerInvariant();
        return rule is "any" or "ip" || rule == packet;
    }

    private static bool IpMatches(string ruleIp, string packetIp)
    {
        ruleIp = (ruleIp ?? string.Empty).Trim().ToLowerInvariant();

        if (ruleIp == "any")
            return true;

        if (!IPAddress.TryParse(packetIp, out var packetAddress))
            return false;

        if (!ruleIp.Contains('/'))
            return ruleIp == packetIp.ToLowerInvariant();

        var parts = ruleIp.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ruleAddress)
            || !int.TryParse(parts[1], out var prefix))
            return false;

        var ruleBytes = ruleAddress.GetAddressBytes();
        var packetBytes = packetAddress.GetAddressBytes();

        if (ruleBytes.Length != packetBytes.Length || prefix < 0 || prefix > ruleBytes.Length * 8)
            return false;

        var mask = CreateMask(ruleBytes.Length, prefix);
        for (var i = 0; i < ruleBytes.Length; i++)
        {
            if ((ruleBytes[i] & mask[i]) != (packetBytes[i] & mask[i]))
                return false;
        }

        return true;
    }

    private static bool PortMatches(string rulePort, int packetPort)
    {
        var port = (rulePort ?? string.Empty).Trim().ToLowerInvariant();

        if (port is "any" or "" or "0")
            return true;

        if (int.TryParse(port, out var portNumber))
            return portNumber == packetPort;

        if (port.Contains('-'))
        {
            var range = port.Split('-');
            if (range.Length == 2 && int.TryParse(range[0], out var low)
                && int.TryParse(range[1], out var high))
                return packetPort >= low && packetPort <= high;
        }

        return false;
    }

    private static byte[] CreateMask(int length, int prefix)
    {
        var mask = new byte[length];
        for (var i = 0; i < length; i++)
        {
            if (prefix >= 8)
            {
                mask[i] = 255;
                prefix -= 8;
            }
            else if (prefix > 0)
            {
                mask[i] = (byte)(255 << (8 - prefix) & 255);
                prefix = 0;
            }
        }
        return mask;
    }

    private static void Shuffle<T>(List<T> items)
    {
        for (var index = items.Count - 1; index > 0; index--)
        {
            var other = Rng.Next(index + 1);
            (items[index], items[other]) = (items[other], items[index]);
        }
    }
}
