namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;

/// <summary>
/// "Investigate." — CR 701.16a: "Investigate" means "Create a Clue token."
/// See rule 111.10f.
/// Maps the bare keyword action to <see cref="InvestigateEffect"/> with no
/// count (defaults to one Clue token). Reminder text has already been stripped
/// by the activated-ability parser before this rule runs (Rule 207.2).
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class InvestigateEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!trimmed.Equals("Investigate", System.StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return MagicAST.AST.Effects.Core.EffectWrap.Optional(new InvestigateEffect {}, false);
  }
}
