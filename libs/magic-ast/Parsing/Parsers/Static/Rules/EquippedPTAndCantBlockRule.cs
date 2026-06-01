namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses a static Equipment/Aura line of the form
/// "Equipped creature gets +N/+N and can't block."
///
/// <para>
/// The P/T modification is a layer-7c continuous effect (CR 613.4c:
/// "Effects and counters that modify power and/or toughness (but don't set power
/// and/or toughness to a specific number or value) are applied.").
/// The blocking restriction is a CR 509.1a constraint (CR 509.1a:
/// "The defending player chooses which creatures they control, if any, will block.
/// The chosen creatures must be untapped and they can't also be battles...").
/// Both effects are always-on (static, no Duration) and apply to the
/// equipped/enchanted permanent (ObjectReferenceKind.EnchantedOrEquipped).
/// </para>
/// </summary>
[StaticRule(Priority = 966)]
public sealed class EquippedPTAndCantBlockRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+can'?t\s+block\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(StaticRuleHelpers.StripReminderText(clause.RawText));
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ModifyPTEffect
          {
            Target = target,
            PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
            ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
          },
          new CantBlockEffect
          {
            Target = target,
          },
        ],
      },
    ];
  }
}
