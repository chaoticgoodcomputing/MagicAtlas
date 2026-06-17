namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the compound static Equipment line:
/// "Equipped creature gets +N/+N, has [keyword], and is a [color] [Subtype]."
///
/// <para>
/// Nim Deathmantle (SOM) is the canonical example:
/// "Equipped creature gets +2/+2, has intimidate, and is a black Zombie."
/// This is a four-part continuous ability:
/// <list type="bullet">
///   <item>P/T modification (layer 7c — CR 613.4c)</item>
///   <item>Keyword ability grant (layer 6 — CR 613.1f)</item>
///   <item>Color change to [color] (layer 5 — CR 613.1e)</item>
///   <item>Subtype change to [Subtype] (layer 4 — CR 613.1d)</item>
/// </list>
/// All four effects target the equipped creature (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>)
/// and persist for as long as the Equipment is attached (no Duration — always-on static).
/// </para>
///
/// <para>
/// Rule 702.6 (Equipment): an Equipment's static abilities apply to the equipped creature.
/// Rule 613.1 (layer ordering): type, color, and P/T changes are applied in strict layer order;
/// MAST records the oracle text descriptively — layer ordering is engine territory.
/// </para>
///
/// <para>
/// Reminder text is stripped by <see cref="StaticRuleHelpers.StripReminderText"/> before
/// matching, following the convention of sibling Equipment static rules such as
/// <see cref="EquippedPTAndCantBlockRule"/>.
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class EquippedPTKeywordColorSubtypeRule : IStaticRule
{
  // "Equipped creature gets +N/+N, has <keyword>, and is a <color> <Subtype>."
  // The keyword capture is a bare lowercase word or two-word phrase (e.g. "first strike").
  // The color is one of the five MTG color names (case-insensitive).
  // The subtype is a single proper-noun word (capitalised creature subtype like "Zombie").
  private static readonly Regex _pattern = new(
    @"^\s*Equipped\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+),\s+has\s+(?<kw>[a-z][a-z ]*?),\s+and\s+is\s+a\s+(?<color>white|blue|black|red|green)\s+(?<subtype>[A-Z][a-zA-Z]+)\.?\s*$",
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

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
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

    var kw = match.Groups["kw"].Value.Trim().ToLowerInvariant();
    var colorName = match.Groups["color"].Value;
    var subtype = match.Groups["subtype"].Value;

    // Ensure the subtype starts with uppercase per oracle-text convention for creature subtypes.
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..];

    if (!_colorNameToCode.TryGetValue(colorName, out var colorCode))
    {
      return null;
    }

    // Map the keyword to its canonical StaticAbility expansion.
    var grantedKeywordAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedKeywordAbility is null)
    {
      return null;
    }

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
          new GainAbilityEffect
          {
            Target = target,
            GainedAbility = grantedKeywordAbility,
          },
          new ChangeColorEffect
          {
            Target = target,
            Colors = [colorCode],
          },
          new ChangeSubtypeEffect
          {
            Target = target,
            Subtypes = [subtype],
          },
        ],
      },
    ];
  }
}
