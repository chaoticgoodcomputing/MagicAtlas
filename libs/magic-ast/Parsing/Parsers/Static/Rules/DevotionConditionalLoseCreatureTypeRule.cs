namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "As long as your devotion to [color] is less than [N], [card name] isn't a creature."
/// — the Theros God template (CR 205.1a / CR 700.5a). Continuous layer-4 type-loss
/// conditional on the controller's devotion to a given colour falling below a threshold.
///
/// <para>
/// CR 700.5a: "A player's 'devotion to [color]' is the number of mana symbols of that
/// color among the mana costs of permanents that player controls." MAST records the
/// condition as a <c>quantityComparison</c> (<see cref="DevotionQuantity"/> &lt; threshold)
/// and the effect as a <c>loseType</c> (<see cref="LoseTypeEffect"/> targeting Self,
/// removing the "creature" card type from the permanent's type line). The StaticAbility
/// carries no Duration: the effect persists for as long as the condition holds
/// (modelled via an <see cref="AsLongAsDuration"/> wrapping the condition on the effect).
/// </para>
///
/// <para>
/// Priority 974 — above the generic <c>AsLongAsStaticGrantRule</c> (968) and below
/// the keyword-list rules (980+), so this dedicated shape wins before the generic
/// fallback sees it.
/// </para>
/// </summary>
[StaticRule(Priority = 974)]
public sealed class DevotionConditionalLoseCreatureTypeRule : IStaticRule
{
  // "As long as your devotion to white is less than five, Heliod isn't a creature."
  // Color names → WUBRG codes. Threshold word-numbers are mapped below.
  private static readonly Regex _pattern = new(
    @"^\s*As\s+long\s+as\s+your\s+devotion\s+to\s+(?<color>white|blue|black|red|green)"
    + @"\s+is\s+less\s+than\s+(?<threshold>one|two|three|four|five|six|seven|eight|nine|ten|\d+),\s*"
    + @"(?<name>.+?)\s+isn'?t\s+a\s+creature\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorNameToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  private static readonly IReadOnlyDictionary<string, int> _numberWords =
    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
      ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var colorCode = _colorNameToCode[m.Groups["color"].Value];

    var thresholdRaw = m.Groups["threshold"].Value;
    var threshold = _numberWords.TryGetValue(thresholdRaw, out var tw)
      ? tw
      : int.TryParse(thresholdRaw, out var ti) ? ti : 5;

    // The condition: your devotion to [color] is less than [threshold].
    // Expressed as a quantityComparison: DevotionQuantity < LiteralQuantity(threshold).
    // CR 700.5a: "A player's 'devotion to [color]' is the number of mana symbols of
    // that color among the mana costs of permanents that player controls."
    var condition = new QuantityComparisonCondition
    {
      Left = new DevotionQuantity { Colors = [colorCode] },
      Operator = ComparisonOperator.LessThan,
      Right = LiteralQuantity.Of(threshold),
    };

    var duration = new AsLongAsDuration { Condition = condition };

    // The effect: the named permanent (always Self in oracle text — the card
    // refers to itself by name, CR 201.4) loses the "creature" card type.
    // CR 205.1a: when an effect says a permanent "isn't" of a type, it loses
    // that type for the duration of the effect.
    var effect = new LoseTypeEffect
    {
      Subject = ObjectReference.Self(),
      LostType = "creature",
      Duration = duration,
    };

    return [new StaticAbility { Effects = [effect] }];
  }
}
