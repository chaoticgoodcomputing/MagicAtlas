namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Equipped creature gets +N/+M and has protection from [quality] and from [quality]."
/// Also handles the Enchanted form and single-quality protection.
///
/// <para>
/// The P/T buff is a layer-7c continuous effect (CR 613.4c). The protection grant is
/// a static continuous effect (CR 702.16 — protection from a quality). Both apply as
/// long as the Aura or Equipment remains attached (static, no Duration).
/// </para>
///
/// <para>
/// The protection may be from one or more qualities, each preceded by "from" (or
/// "and from" for additional qualities), e.g.
/// "protection from black and from green" → two Color qualities (B, G).
/// </para>
///
/// <para>
/// CR 613.4c: "Effects and counters that modify power and/or toughness (but don't
/// set power and/or toughness to a specific number or value) are applied."
/// CR 702.16a: "Protection is a static ability … this permanent can't be blocked,
/// targeted, dealt damage, enchanted, or equipped by anything [with the quality]."
/// </para>
///
/// Priority 968 — above <see cref="EnchantedPTAndDualKeywordRule"/> (966) so the
/// "protection from" phrase is recognised before the dual-keyword rule attempts to
/// split it into two bare keyword names.
/// </summary>
[StaticRule(Priority = 968)]
public sealed class EquippedPTAndProtectionRule : IStaticRule
{
  // "Equipped/Enchanted creature gets +N/+M and has protection from <quality1> [and from <quality2> ...]"
  private static readonly Regex _pattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+protection\s+from\s+(?<qualities>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches "and from <next-quality>" chain
  private static readonly Regex _andFromPattern = new(
    @"\s+and\s+from\s+",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

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

    // Parse quality list: split on "and from" to get each quality word.
    var qualitiesRaw = match.Groups["qualities"].Value.Trim();
    var qualityParts = _andFromPattern.Split(qualitiesRaw);
    var qualities = new List<ProtectionQuality>(qualityParts.Length);
    foreach (var part in qualityParts)
    {
      var q = ParseQuality(part.Trim());
      if (q is null)
      {
        return null;
      }
      qualities.Add(q);
    }

    if (qualities.Count == 0)
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
          new CompositeEffect
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
                GainedAbility = new StaticAbility
                {
                  KeywordSource = KeywordAbility.Protection,
                  Effects =
                  [
                    new ProtectionEffect { From = qualities },
                  ],
                },
              },
            ],
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Maps a raw quality word ("black", "green", "Demons", "everything", etc.) onto a
  /// <see cref="ProtectionQuality"/>. Returns null for unrecognised shapes.
  /// </summary>
  private static ProtectionQuality? ParseQuality(string raw)
  {
    var normalized = raw.ToLowerInvariant().Trim();

    if (normalized is "everything" or "all")
    {
      return new ProtectionQuality { Kind = ProtectionQualityKind.Everything };
    }

    // Color names → WUBRG codes
    var colorCode = normalized switch
    {
      "white" => "W",
      "blue" => "U",
      "black" => "B",
      "red" => "R",
      "green" => "G",
      _ => null,
    };
    if (colorCode is not null)
    {
      return new ProtectionQuality { Kind = ProtectionQualityKind.Color, Value = colorCode };
    }

    // Characteristics
    if (normalized is "multicolored" or "monocolored" or "colorless")
    {
      return new ProtectionQuality { Kind = ProtectionQualityKind.Characteristic, Value = normalized };
    }

    // Card types (plural or singular)
    var singularized = normalized switch
    {
      "creatures" => "creature",
      "artifacts" => "artifact",
      "enchantments" => "enchantment",
      "instants" => "instant",
      "sorceries" => "sorcery",
      "planeswalkers" => "planeswalker",
      _ => normalized,
    };
    if (singularized is "creature" or "artifact" or "enchantment" or "instant" or "sorcery" or "planeswalker")
    {
      return new ProtectionQuality { Kind = ProtectionQualityKind.CardType, Value = singularized };
    }

    // Treat capitalized words as subtypes (e.g. "Demons", "Dragons")
    if (raw.Length > 0 && char.IsUpper(raw[0]))
    {
      return new ProtectionQuality { Kind = ProtectionQualityKind.Subtype, Value = raw };
    }

    return null;
  }
}
