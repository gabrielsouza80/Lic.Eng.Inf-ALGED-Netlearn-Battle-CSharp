namespace NetLearnBattle.CSharp.Models;

public class AclEvaluation
{
    public string Action { get; set; } = "deny";
    public AclRule? MatchedRule { get; set; }
    public int MatchedRuleIndex { get; set; } = -1;
}
