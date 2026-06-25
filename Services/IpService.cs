using System.Net;
using System.Net.Sockets;
using NetLearnBattle.CSharp.Models;

namespace NetLearnBattle.CSharp.Services;

public class IpService
{
    private static readonly Random _rng = new();

    public string CalculateIpv4NetworkId(string ip, int prefix)
    {
        // [M47] Network ID é calculado aplicando a máscara ao IPv4.
        var bytes = IPAddress.Parse(ip).GetAddressBytes();
        var mask = CreateIpv4Mask(prefix);
        for (int i = 0; i < 4; i++)
            bytes[i] &= mask[i];
        return new IPAddress(bytes).ToString();
    }

    public string CalculateIpv4Broadcast(string ip, int prefix)
    {
        // [M48] Broadcast IPv4 é o último endereço da rede.
        var bytes = IPAddress.Parse(ip).GetAddressBytes();
        var mask = CreateIpv4Mask(prefix);
        for (int i = 0; i < 4; i++)
            bytes[i] = (byte)((bytes[i] & mask[i]) | (byte)~mask[i]);
        return new IPAddress(bytes).ToString();
    }

    public bool SameIpv4Network(string ip1, string ip2, int prefix)
    {
        // [M49] Dois IPv4 estão no mesmo segmento se tiverem o mesmo Network ID.
        return CalculateIpv4NetworkId(ip1, prefix) == CalculateIpv4NetworkId(ip2, prefix);
    }

    public Question GenerateIpv4Question(int level)
    {
        // [M46] Gera perguntas IPv4 para níveis 1, 2 e 3.
        var (prefix, pointsCorrect, pointsWrong) = level switch
        {
            1 => (GetRandomPrefix(new[] { 8, 16, 24 }), 10, -5),
            2 => (GetRandomPrefix(new[] { 25, 26, 27 }), 20, -10),
            3 => (GetRandomPrefix(new[] { 21, 22, 23 }), 30, -15),
            _ => (24, 10, -5)
        };

        var (ip, network) = GenerateRandomIpv4(prefix);
        var type = _rng.Next(3);

        return type switch
        {
            0 => MakeNetworkIdQuestion(ip, prefix, network, pointsCorrect, pointsWrong, level),
            1 => MakeBroadcastQuestion(ip, prefix, pointsCorrect, pointsWrong, level),
            _ => MakeSameNetworkQuestion(prefix, pointsCorrect, pointsWrong, level),
        };
    }

    public string CalculateIpv6NetworkId(string ip, int prefix)
    {
        // [M50] IPv6 usa prefixo de rede, mas não broadcast tradicional.
        var bytes = IPAddress.Parse(ip).GetAddressBytes();
        var mask = CreateIpv6Mask(prefix);
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] &= mask[i];
        return new IPAddress(bytes).ToString();
    }

    public bool SameIpv6Network(string ip1, string ip2, int prefix)
    {
        // [M50] Mesmo segmento IPv6: compara o prefixo de rede.
        return CalculateIpv6NetworkId(ip1, prefix) == CalculateIpv6NetworkId(ip2, prefix);
    }

    public Question GenerateIpv6Question()
    {
        // [M50] Nível 4 mistura Network ID, mesmo segmento, sub-redes e conceito.
        var pointsCorrect = 40;
        var pointsWrong = -20;
        var type = _rng.Next(4);

        return type switch
        {
            0 => MakeIpv6NetworkIdQuestion(pointsCorrect, pointsWrong),
            1 => MakeIpv6SameNetworkQuestion(pointsCorrect, pointsWrong),
            2 => MakeIpv6SubnetQuestion(pointsCorrect, pointsWrong),
            _ => MakeIpv6BroadcastConceptQuestion(pointsCorrect, pointsWrong),
        };
    }

    private static byte[] CreateIpv4Mask(int prefix)
    {
        var mask = new byte[4];
        for (int i = 0; i < 4; i++)
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
            else
            {
                mask[i] = 0;
            }
        }
        return mask;
    }

    private static byte[] CreateIpv6Mask(int prefix)
    {
        var mask = new byte[16];
        for (int i = 0; i < 16; i++)
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
            else
            {
                mask[i] = 0;
            }
        }
        return mask;
    }

    private static int GetRandomPrefix(int[] options)
    {
        return options[_rng.Next(options.Length)];
    }

    private (string ip, string network) GenerateRandomIpv4(int prefix)
    {
        // [M46] Usa redes privadas para manter exemplos académicos e seguros.
        var networks = new List<string[]> {
            new[] { "10", "0", "0", "0" },
            new[] { "172", "16", "0", "0" },
            new[] { "192", "168", "0", "0" },
        };

        var baseNet = networks[_rng.Next(networks.Count)];
        var netBytes = baseNet.Select(byte.Parse).ToArray();
        var hostBytes = new byte[4];
        Array.Copy(netBytes, hostBytes, 4);

        var mask = CreateIpv4Mask(prefix);
        for (int i = 0; i < 4; i++)
            hostBytes[i] = (byte)(netBytes[i] & mask[i]);

        var networkIp = new IPAddress(hostBytes).ToString();

        var invertedMask = new byte[4];
        for (int i = 0; i < 4; i++)
            invertedMask[i] = (byte)~mask[i];

        for (int i = 0; i < 4; i++)
        {
            if (invertedMask[i] != 0)
            {
                int maxHost = invertedMask[i];
                hostBytes[i] = (byte)(hostBytes[i] + _rng.Next(1, maxHost));
            }
        }

        var ip = new IPAddress(hostBytes).ToString();
        return (ip, networkIp);
    }

    private static int GetThirdOctet(string networkIp)
    {
        var parts = networkIp.Split('.');
        return int.Parse(parts[2]);
    }

    private Question MakeNetworkIdQuestion(string ip, int prefix, string network,
        int pointsCorrect, int pointsWrong, int level)
    {
        var correct = network;
        var distractors = GenerateIpv4Distractors(network, prefix, 3);
        var options = new List<string> { correct };
        options.AddRange(distractors);
        Shuffle(options);

        return new Question
        {
            Level = level,
            Topic = GetIpv4Topic(level),
            QuestionText = $"Qual é o Network ID de {ip}/{prefix}?",
            Options = options,
            CorrectIndex = options.IndexOf(correct),
            PointsCorrect = pointsCorrect,
            PointsWrong = pointsWrong,
        };
    }

    private Question MakeBroadcastQuestion(string ip, int prefix,
        int pointsCorrect, int pointsWrong, int level)
    {
        var correct = CalculateIpv4Broadcast(ip, prefix);
        var distractors = GenerateIpv4BroadcastDistractors(ip, prefix, 3);
        var options = new List<string> { correct };
        options.AddRange(distractors);
        Shuffle(options);

        return new Question
        {
            Level = level,
            Topic = GetIpv4Topic(level),
            QuestionText = $"Qual é o Broadcast de {ip}/{prefix}?",
            Options = options,
            CorrectIndex = options.IndexOf(correct),
            PointsCorrect = pointsCorrect,
            PointsWrong = pointsWrong,
        };
    }

    private Question MakeSameNetworkQuestion(int prefix,
        int pointsCorrect, int pointsWrong, int level)
    {
        var networks = new List<string[]> {
            new[] { "10", "0", "0", "0" },
            new[] { "172", "16", "0", "0" },
            new[] { "192", "168", "0", "0" },
        };

        var baseNet = networks[_rng.Next(networks.Count)];
        var netBytes = baseNet.Select(byte.Parse).ToArray();
        var mask = CreateIpv4Mask(prefix);

        var hostPart = new byte[4];
        Array.Copy(netBytes, hostPart, 4);
        for (int i = 0; i < 4; i++)
            hostPart[i] = (byte)(netBytes[i] & mask[i]);

        var invertedMask = new byte[4];
        for (int i = 0; i < 4; i++)
            invertedMask[i] = (byte)~mask[i];

        var ip1Bytes = new byte[4];
        Array.Copy(hostPart, ip1Bytes, 4);
        var ip2Bytes = new byte[4];
        Array.Copy(hostPart, ip2Bytes, 4);

        bool sameNetwork = _rng.Next(2) == 0;

        // Primeiro criamos dois IPs válidos dentro da mesma rede.
        for (int i = 0; i < 4; i++)
        {
            if (invertedMask[i] != 0)
            {
                int maxHost = invertedMask[i];
                ip1Bytes[i] = (byte)(hostPart[i] + _rng.Next(1, maxHost + 1));
                ip2Bytes[i] = (byte)(hostPart[i] + _rng.Next(1, maxHost + 1));
                break;
            }
        }

        if (!sameNetwork)
        {
            // Alterar um bit que pertence à rede garante outro segmento.
            // Esta abordagem também funciona para /8, /16 e /24.
            var networkOctet = Array.FindLastIndex(mask, value => value != 0);
            var networkBit = 1;

            while ((mask[networkOctet] & networkBit) == 0)
                networkBit <<= 1;

            ip2Bytes[networkOctet] ^= (byte)networkBit;
        }

        var ip1 = new IPAddress(ip1Bytes).ToString();
        var ip2 = new IPAddress(ip2Bytes).ToString();
        var correct = sameNetwork;
        var correctText = correct ? "Sim" : "Não";

        var options = new List<string> { "Sim", "Não" };

        return new Question
        {
            Level = level,
            Topic = GetIpv4Topic(level),
            QuestionText = $"Os IPs {ip1}/{prefix} e {ip2}/{prefix} estão no mesmo segmento?",
            Options = options,
            CorrectIndex = options.IndexOf(correctText),
            PointsCorrect = pointsCorrect,
            PointsWrong = pointsWrong,
        };
    }

    private List<string> GenerateIpv4Distractors(string networkIp, int prefix, int count)
    {
        var parts = networkIp.Split('.').Select(byte.Parse).ToArray();
        var result = new List<string>();
        var used = new HashSet<string> { networkIp };

        var candidates = new List<string>();
        if (prefix <= 8)
        {
            for (int i = 0; i < 5; i++)
            {
                var b = (byte)(parts[0] + _rng.Next(1, 10));
                candidates.Add($"{b}.0.0.0");
            }
        }
        else if (prefix <= 16)
        {
            for (int i = 0; i < 5; i++)
            {
                var b = (byte)(parts[1] + _rng.Next(1, 10));
                candidates.Add($"{parts[0]}.{b}.0.0");
            }
        }
        else if (prefix <= 24)
        {
            for (int i = 0; i < 5; i++)
            {
                var b = (byte)(parts[2] + _rng.Next(1, 10));
                candidates.Add($"{parts[0]}.{parts[1]}.{b}.0");
            }

            if (prefix < 24)
            {
                var step = 1 << (24 - prefix);
                var baseVal = parts[2] / step * step;
                for (int i = 1; i <= 4; i++)
                {
                    var val = (byte)(baseVal + step * ((parts[2] / step + i) % (256 / step)));
                    candidates.Add($"{parts[0]}.{parts[1]}.{val}.0");
                }
            }
        }
        else
        {
            var step = 1 << (32 - prefix);
            var base24 = (parts[2] << 8) | parts[3];
            var baseNet = base24 / step * step;
            for (int i = 1; i <= 4; i++)
            {
                var val = baseNet + step * ((base24 / step + i) % (256 * 256 / step));
                candidates.Add($"{parts[0]}.{parts[1]}.{val >> 8}.{val & 255}");
            }
        }

        foreach (var c in candidates)
        {
            if (result.Count >= count) break;
            if (used.Add(c)) result.Add(c);
        }

        while (result.Count < count)
        {
            var fallback = $"{_rng.Next(1, 224)}.{_rng.Next(0, 256)}.{_rng.Next(0, 256)}.{_rng.Next(0, 256)}";
            if (used.Add(fallback)) result.Add(fallback);
        }

        return result;
    }

    private List<string> GenerateIpv4BroadcastDistractors(string ip, int prefix, int count)
    {
        var correct = CalculateIpv4Broadcast(ip, prefix);
        var result = new List<string>();
        var used = new HashSet<string> { correct };

        var parts = ip.Split('.').Select(byte.Parse).ToArray();
        var mask = CreateIpv4Mask(prefix);
        var invMask = new byte[4];
        for (int i = 0; i < 4; i++)
            invMask[i] = (byte)~mask[i];

        var candidates = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            if (invMask[i] != 0)
            {
                var alt = (byte)(parts[i] | invMask[i]);
                var altBytes = new byte[4];
                Array.Copy(parts, altBytes, 4);
                altBytes[i] = (byte)(alt - 1);
                if (altBytes[i] < alt)
                    candidates.Add(new IPAddress(altBytes).ToString());

                altBytes[i] = (byte)(alt + 1);
                if (altBytes[i] > alt)
                    candidates.Add(new IPAddress(altBytes).ToString());
            }
        }

        var calcBytes = IPAddress.Parse(correct).GetAddressBytes();
        for (int i = 0; i < 4; i++)
        {
            if (calcBytes[i] > 0)
            {
                var altBytes = new byte[4];
                Array.Copy(calcBytes, altBytes, 4);
                altBytes[i] = 0;
                candidates.Add(new IPAddress(altBytes).ToString());
            }
        }

        foreach (var c in candidates)
        {
            if (result.Count >= count) break;
            if (used.Add(c)) result.Add(c);
        }

        while (result.Count < count)
        {
            var fallback = $"{_rng.Next(1, 224)}.{_rng.Next(0, 256)}.{_rng.Next(0, 256)}.{_rng.Next(0, 256)}";
            if (used.Add(fallback)) result.Add(fallback);
        }

        return result;
    }

    private Question MakeIpv6NetworkIdQuestion(int pc, int pw)
    {
        var prefix = GetRandomPrefix(new[] { 48, 56, 64 });
        var (ip, network) = GenerateRandomIpv6(prefix);
        var correct = network.ToLowerInvariant();
        var distractors = GenerateIpv6Distractors(network, 3);
        var options = new List<string> { correct };
        options.AddRange(distractors);
        Shuffle(options);

        return new Question
        {
            Level = 4,
            Topic = "IPv6",
            QuestionText = $"Qual é o Network ID de {ip}/{prefix}?",
            Options = options,
            CorrectIndex = options.IndexOf(correct),
            PointsCorrect = pc,
            PointsWrong = pw,
        };
    }

    private Question MakeIpv6SameNetworkQuestion(int pc, int pw)
    {
        var prefix = GetRandomPrefix(new[] { 48, 56, 64 });
        var (ip1, network1) = GenerateRandomIpv6(prefix);
        var sameNetwork = _rng.Next(2) == 0;

        string ip2;
        if (sameNetwork)
        {
            var bytes1 = IPAddress.Parse(ip1).GetAddressBytes();
            var bytes2 = new byte[16];
            Array.Copy(bytes1, bytes2, 16);
            var mask = CreateIpv6Mask(prefix);
            var invMask = new byte[16];
            for (int i = 0; i < 16; i++)
                invMask[i] = (byte)~mask[i];

            for (int i = 15; i >= 0; i--)
            {
                if (invMask[i] != 0)
                {
                    bytes2[i] = (byte)(bytes1[i] ^ _rng.Next(1, invMask[i] + 1));
                    break;
                }
            }
            ip2 = new IPAddress(bytes2).ToString();
        }
        else
        {
            // Alterar um bit do prefixo garante que o segundo IP pertence
            // a outra rede, em vez de depender de uma geração aleatória.
            var bytes2 = IPAddress.Parse(ip1).GetAddressBytes();
            var mask = CreateIpv6Mask(prefix);
            var networkOctet = Array.FindLastIndex(mask, value => value != 0);
            var networkBit = 1;

            while ((mask[networkOctet] & networkBit) == 0)
                networkBit <<= 1;

            bytes2[networkOctet] ^= (byte)networkBit;
            ip2 = new IPAddress(bytes2).ToString();
        }

        var correct = sameNetwork ? "Sim" : "Não";
        var options = new List<string> { "Sim", "Não" };

        return new Question
        {
            Level = 4,
            Topic = "IPv6",
            QuestionText = $"Os IPs {ip1}/{prefix} e {ip2}/{prefix} estão no mesmo segmento?",
            Options = options,
            CorrectIndex = options.IndexOf(correct),
            PointsCorrect = pc,
            PointsWrong = pw,
        };
    }

    private Question MakeIpv6SubnetQuestion(int pc, int pw)
    {
        var prefix = GetRandomPrefix(new[] { 48, 56, 64 });
        var (ip, network) = GenerateRandomIpv6(prefix);
        var newPrefix = prefix + 8;
        var subnetNetwork = CalculateIpv6NetworkId(ip, newPrefix);

        var correct = subnetNetwork.ToLowerInvariant();
        var distractors = GenerateIpv6Distractors(subnetNetwork, 3);
        var options = new List<string> { correct };
        options.AddRange(distractors);
        Shuffle(options);

        return new Question
        {
            Level = 4,
            Topic = "IPv6",
            QuestionText = $"Qual é a sub-rede de {ip}/{prefix} com prefixo /{newPrefix}?",
            Options = options,
            CorrectIndex = options.IndexOf(correct),
            PointsCorrect = pc,
            PointsWrong = pw,
        };
    }

    private Question MakeIpv6BroadcastConceptQuestion(int pc, int pw)
    {
        // [M50] Esta pergunta evita dizer que IPv6 tem broadcast tradicional.
        var options = new List<string>
        {
            "IPv6 não usa broadcast tradicional.",
            "FF02::1 é o broadcast IPv6.",
            "IPv6 usa o mesmo broadcast que IPv4.",
            "Não existe broadcast em redes."
        };

        return new Question
        {
            Level = 4,
            Topic = "IPv6",
            QuestionText = "Como funciona o broadcast no IPv6?",
            Options = options,
            CorrectIndex = 0,
            PointsCorrect = pc,
            PointsWrong = pw,
        };
    }

    private (string ip, string network) GenerateRandomIpv6(int prefix, bool differentNetwork = false)
    {
        var rngLocal = _rng;
        var bytes = new byte[16];

        var prefixes = new byte[][] {
            new byte[] { 0x20, 0x01, 0x0d, 0xb8 },
            new byte[] { 0x20, 0x01, 0x0d, 0xb9 },
            new byte[] { 0x20, 0x02, 0x0d, 0xb8 },
        };

        var selectedPrefix = prefixes[rngLocal.Next(prefixes.Length)];
        Array.Copy(selectedPrefix, bytes, 4);

        for (int i = 4; i < 16; i++)
            bytes[i] = (byte)rngLocal.Next(256);

        var mask = CreateIpv6Mask(prefix);
        var netBytes = new byte[16];
        Array.Copy(bytes, netBytes, 16);
        for (int i = 0; i < 16; i++)
            netBytes[i] &= mask[i];

        if (differentNetwork)
        {
            var diffOctet = -1;
            for (int i = 0; i < 16; i++)
            {
                if (mask[i] != 255)
                {
                    diffOctet = i;
                    break;
                }
            }

            if (diffOctet >= 0)
            {
                var invMask = (byte)~mask[diffOctet];
                var flippableBits = new List<int>();
                for (int b = 0; b < 8; b++)
                {
                    if ((invMask & (1 << b)) != 0)
                        flippableBits.Add(b);
                }

                if (flippableBits.Count > 0)
                {
                    var bitToFlip = flippableBits[rngLocal.Next(flippableBits.Count)];
                    bytes[diffOctet] ^= (byte)(1 << bitToFlip);
                }
            }

            for (int i = 0; i < 16; i++)
                bytes[i] &= mask[i];
        }

        var hostBytes = new byte[16];
        Array.Copy(bytes, hostBytes, 16);

        var invertedMask = new byte[16];
        for (int i = 0; i < 16; i++)
            invertedMask[i] = (byte)~mask[i];

        for (int i = 0; i < 16; i++)
        {
            if (invertedMask[i] != 0)
            {
                var maxAdd = invertedMask[i];
                if (maxAdd > 0)
                    hostBytes[i] = (byte)(hostBytes[i] + rngLocal.Next(1, maxAdd + 1));
                break;
            }
        }

        var ip = new IPAddress(hostBytes).ToString();
        var network = new IPAddress(netBytes).ToString();
        return (ip, network);
    }

    private List<string> GenerateIpv6Distractors(string networkIp, int count)
    {
        var result = new List<string>();
        var used = new HashSet<string> { networkIp.ToLowerInvariant() };

        var bytes = IPAddress.Parse(networkIp).GetAddressBytes();

        for (int i = 0; i < count * 3; i++)
        {
            var altBytes = new byte[16];
            Array.Copy(bytes, altBytes, 16);
            var octet = _rng.Next(8, 16);
            altBytes[octet] = (byte)_rng.Next(256);
            var alt = new IPAddress(altBytes).ToString().ToLowerInvariant();
            if (used.Add(alt))
            {
                result.Add(alt);
                if (result.Count >= count) break;
            }
        }

        while (result.Count < count)
        {
            var (_, newNet) = GenerateRandomIpv6(48);
            var alt = newNet.ToLowerInvariant();
            if (used.Add(alt)) result.Add(alt);
        }

        return result;
    }

    private static string GetIpv4Topic(int level)
    {
        return level switch
        {
            1 => "IPv4 básico",
            2 => "Sub-redes IPv4",
            3 => "Super-redes IPv4",
            _ => "IPv4",
        };
    }

    private static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
