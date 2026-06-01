namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Enchanted creature gets &lt;±P/±T&gt; until end of turn" and
/// "Equipped creature gets &lt;±P/±T&gt; until end of turn" — an activated pump
/// whose subject is the permanent this card is attached to.
///
/// <para>
/// <b>CR 611.1:</b> "A continuous effect modifies characteristics of objects,
/// modifies control of objects, or affects players or the rules of the game,
/// for a fixed or indefinite period."
/// </para>
/// <para>
/// <b>CR 613.4c:</b> "Layer 7c: Effects and counters that modify power and/or
/// toughness (but don't set power and/or toughness to a specific number or
/// value) are applied."
/// </para>
/// <para>
/// <b>CR 208.1:</b> "...Power and toughness can be modified or set to particular
/// values by effects."
/// </para>
///
/// <para>
/// Subject is modelled as
/// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> — the permanent this
/// Aura or Equipment is currently attached to — rather than free text.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class EnchantedEquippedModifyPTEffectRule : IActivatedEffectRule
{
  // Token grammar: [+\-](\d+|X) — e.g. "+1", "-2", "+X", "-X", "+0".
  private const string ModGrammar = @"(?<{0}>[+\-](?:\d+|X))";

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    var pGroup = string.Format(ModGrammar, "p");
    var tGroup = string.Format(ModGrammar, "t");

    // "Enchanted creature gets <±P/±T> until end of turn"
    // "Equipped creature gets <±P/±T> until end of turn"
    var match = Regex.Match(
      trimmed,
      $@"^(?:Enchanted|Equipped)\s+creature\s+gets\s+{pGroup}/{tGroup}\s+until\s+end\s+of\s+turn$",
      RegexOptions.IgnoreCase
    );

    if (!match.Success)
    {
      return null;
    }

    return new ModifyPTEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      PowerModifier = ActivatedRuleHelpers.ParseSignedModifier(match.Groups["p"].Value),
      ToughnessModifier = ActivatedRuleHelpers.ParseSignedModifier(match.Groups["t"].Value),
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
