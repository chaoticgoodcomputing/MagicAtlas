namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target creature gets [+-](N|X)/[+-](M|X) (until end of turn | for as long as
/// [condition])." and the self-referential "This creature gets …" variant. Handles
/// literal and variable modifiers; variable negation is a CalculatedQuantity with
/// Operation = "negate" (see <see cref="ActivatedRuleHelpers.ParseSignedModifier"/>).
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class ModifyPTEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    // Token grammar for one modifier side: [+\-](\d+|X) e.g. "+1", "-2", "+X", "-X".
    const string modGrammar = @"(?<{0}>[+\-](?:\d+|X))";
    var pGroup = string.Format(modGrammar, "p");
    var tGroup = string.Format(modGrammar, "t");

    // Shape A: "Target creature gets <mod>/<mod> until end of turn"
    var eotMatch = Regex.Match(
      trimmed,
      $@"^Target\s+creature\s+gets\s+{pGroup}/{tGroup}\s+until\s+end\s+of\s+turn$",
      RegexOptions.IgnoreCase
    );
    if (eotMatch.Success)
    {
      return new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = ActivatedRuleHelpers.ParseSignedModifier(eotMatch.Groups["p"].Value),
        ToughnessModifier = ActivatedRuleHelpers.ParseSignedModifier(eotMatch.Groups["t"].Value),
        Duration = new UntilEndOfTurnDuration(),
      };
    }

    // Shape B: "Target creature gets <mod>/<mod> for as long as [condition]"
    var asLongAsMatch = Regex.Match(
      trimmed,
      $@"^Target\s+creature\s+gets\s+{pGroup}/{tGroup}\s+for\s+as\s+long\s+as\s+(?<cond>.+)$",
      RegexOptions.IgnoreCase
    );
    if (asLongAsMatch.Success)
    {
      return new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = ActivatedRuleHelpers.ParseSignedModifier(asLongAsMatch.Groups["p"].Value),
        ToughnessModifier = ActivatedRuleHelpers.ParseSignedModifier(asLongAsMatch.Groups["t"].Value),
        Duration = new AsLongAsDuration { Condition = asLongAsMatch.Groups["cond"].Value.Trim() },
      };
    }

    // Shape C: "This creature gets <mod>/<mod> until end of turn" — self-referential
    // P/T modifier (Rule 613.4c). Subject is the ability's source permanent.
    var selfEotMatch = Regex.Match(
      trimmed,
      $@"^This\s+creature\s+gets\s+{pGroup}/{tGroup}\s+until\s+end\s+of\s+turn$",
      RegexOptions.IgnoreCase
    );
    if (selfEotMatch.Success)
    {
      return new ModifyPTEffect
      {
        Target = ObjectReference.Self(),
        PowerModifier = ActivatedRuleHelpers.ParseSignedModifier(selfEotMatch.Groups["p"].Value),
        ToughnessModifier = ActivatedRuleHelpers.ParseSignedModifier(selfEotMatch.Groups["t"].Value),
        Duration = new UntilEndOfTurnDuration(),
      };
    }

    return null;
  }
}
