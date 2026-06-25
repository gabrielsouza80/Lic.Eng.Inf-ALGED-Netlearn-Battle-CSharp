namespace NetLearnBattle.CSharp.Models;

public class TopicStats
{
    public string Topic { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public double AccuracyRate { get; set; }
}
