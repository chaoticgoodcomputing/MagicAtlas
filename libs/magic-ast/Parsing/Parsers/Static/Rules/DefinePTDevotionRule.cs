namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;

/// <summary>
/// A characteristic-defining ability (CDA) that sets a creature's power and/or
/// toughness to a <see cref="DevotionQuantity"/> — the Theros Demigod template. Handles
/// the oracle pattern:
/// <list type="bullet">
/// <item>"[Name]'s power is equal to your devotion to [color]."
///   → <see cref="PTCharacteristic.Power"/></item>
/// <item>"[Name]'s toughness is equal to your devotion to [color]."
///   → <see cref="PTCharacteristic.Toughness"/></item>
/// <item>"[Name]'s power and toughness are each equal to your devotion to [color]."
///   → <see cref="PTCharacteristic.Both"/></item>
/// </list>
/// e.g. Anax, Hardened in the Forge: "Anax's power is equal to your devotion to red."
/// e.g. Daxos, Blessed by the Sun: "Daxos's toughness is equal to your devotion to white."
///
/// <para>
/// Rule 604.3: "A characteristic-defining ability defines a characteristic value for
/// the object it's on." Rule 107.3: the value of * in a CDA box is determined by an
/// ability (layer 7a, Rule 613.1a). CR 700.5a: "A player's 'devotion to [color]' is the
/// number of mana symbols of that color among the mana costs of permanents that player
/// controls." The line's trailing reminder-text parenthetical ("Each {R} in the mana
/// costs of permanents you control counts toward your devotion to red.") is captured on
/// the returned <see cref="StaticAbility.Reminder"/> field, mirroring the trailing-reminder
/// convention already established for triggered abilities (Thassa's Oracle).
/// </para>
///
/// <para>
/// Priority 977 — above <see cref="DefinePTDerivedRule"/> (976) and <see cref="DefinePTRule"/>
/// (975), so this devotion-specific branch fires before either of the "number of [filter]"
/// branches see the clause (the devotion phrase never contains "the number of").
/// </para>
/// </summary>
[StaticRule(Priority = 977)]
public sealed class DefinePTDevotionRule : IStaticRule
{
  // "[Name]'s power and toughness are each equal to your devotion to [color]."
  private static readonly Regex _bothPattern = new(
    @"^\s*.+?'s\s+power\s+and\s+toughness\s+are\s+each\s+equal\s+to\s+your\s+devotion\s+to\s+"
    + @"(?<color>white|blue|black|red|green)(?:\s+and\s+(?<color2>white|blue|black|red|green))?"
    + @"\.\s*(?:\((?<reminder>[^)]*)\)\s*)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "[Name]'s (power|toughness) is equal to your devotion to [color]."
  private static readonly Regex _singlePattern = new(
    @"^\s*.+?'s\s+(?<which>power|toughness)\s+is\s+equal\s+to\s+your\s+devotion\s+to\s+"
    + @"(?<color>white|blue|black|red|green)(?:\s+and\s+(?<color2>white|blue|black|red|green))?"
    + @"\.\s*(?:\((?<reminder>[^)]*)\)\s*)?$",
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
    var bothMatch = _bothPattern.Match(clause.RawText);
    if (bothMatch.Success)
    {
      return
      [
        BuildAbility(PTCharacteristic.Both, BuildColors(bothMatch), bothMatch),
      ];
    }

    var singleMatch = _singlePattern.Match(clause.RawText);
    if (singleMatch.Success)
    {
      var which = singleMatch.Groups["which"].Value.ToLowerInvariant();
      var characteristic = which == "power"
        ? PTCharacteristic.Power
        : PTCharacteristic.Toughness;

      return
      [
        BuildAbility(characteristic, BuildColors(singleMatch), singleMatch),
      ];
    }

    return null;
  }

  private static List<string> BuildColors(Match match)
  {
    var colors = new List<string> { _colorNameToCode[match.Groups["color"].Value] };
    if (match.Groups["color2"].Success)
    {
      colors.Add(_colorNameToCode[match.Groups["color2"].Value]);
    }
    return colors;
  }

  private static StaticAbility BuildAbility(PTCharacteristic characteristic, List<string> colors, Match match)
  {
    return new StaticAbility
    {
      Effects = [new DefinePTEffect
      {
        Characteristic = characteristic,
        Value = new DevotionQuantity { Colors = colors },
      }],
      Reminder = match.Groups["reminder"].Success
        ? new Parenthetical { Text = match.Groups["reminder"].Value.Trim() }
        : null,
    };
  }
}
