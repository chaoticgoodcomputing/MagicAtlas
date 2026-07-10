namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Enchanted creature has protection from [quality]." optionally followed by the
/// self-preservation sentence "This effect doesn't remove this Aura."
///
/// <para>
/// The protection grant is a static continuous effect (CR 702.16a — protection from a
/// quality). Unlike <see cref="EquippedPTAndProtectionRule"/>, this clause carries no
/// P/T buff — it is the bare grant, so it emits a single
/// <see cref="GainAbilityEffect"/> rather than a <see cref="CompositeEffect"/>.
/// </para>
///
/// <para>
/// CR 702.16n: "Some Auras both give the enchanted creature protection from a quality
/// and say 'this effect doesn't remove' either that specific Aura or all Auras. This
/// means that the specified Auras aren't put into their owners' graveyards as a
/// state-based action. If the creature has other instances of protection from the same
/// quality, those instances affect Auras as normal." When present, this trailing
/// sentence is captured as <see cref="ProtectionEffect.DoesNotRemoveThisAura"/> — the
/// faithful encoding of the clause referenced against CR 702.16c (the state-based
/// action it overrides): "A permanent or player with protection can't be enchanted by
/// Auras that have the stated quality. Such Auras attached to the permanent or player
/// with protection will be put into their owners' graveyards as a state-based action."
/// </para>
///
/// Priority 969 — one above <see cref="EquippedPTAndProtectionRule"/> (968) so the bare
/// "Enchanted creature has protection from" grant (with no P/T buff prefix) is claimed
/// before any generic keyword-grant splitter gets a chance at it.
/// </summary>
[StaticRule(Priority = 969)]
public sealed class EnchantedHasProtectionRule : IStaticRule
{
  // "Enchanted creature has protection from <quality1> [and from <quality2> ...]."
  // optionally followed by "This effect doesn't remove this Aura."
  private static readonly Regex _pattern = new(
    @"^\s*Enchanted\s+creature\s+has\s+protection\s+from\s+(?<qualities>.+?)\.(?:\s*This\s+effect\s+doesn't\s+remove\s+this\s+Aura\.)?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches "and from <next-quality>" chain
  private static readonly Regex _andFromPattern = new(
    @"\s+and\s+from\s+",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Detects the trailing self-preservation sentence.
  private static readonly Regex _doesNotRemovePattern = new(
    @"This\s+effect\s+doesn't\s+remove\s+this\s+Aura\.",
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

    var doesNotRemoveThisAura = _doesNotRemovePattern.IsMatch(rawText);

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new GainAbilityEffect
          {
            Target = target,
            GainedAbility = new StaticAbility
            {
              KeywordSource = KeywordAbility.Protection,
              Effects =
              [
                new ProtectionEffect { From = qualities, DoesNotRemoveThisAura = doesNotRemoveThisAura },
              ],
            },
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

    // "the chosen color" — a DEFINITE back-reference to the single color bound by
    // the paired "As this Aura enters, choose a color." replacement ability (CR 607
    // linked ability; see ChooseColorOnEntryRule), NOT a fresh per-resolution choice
    // (that shape is "the color of your choice" -> ProtectionQualityKind.ChosenColor,
    // handled by the spell/activated protection-grant rules). Floating Shield.
    if (normalized is "the chosen color" or "chosen color")
    {
      return new ProtectionQuality
      {
        Kind = ProtectionQualityKind.ChosenCharacteristic,
        ChosenCharacteristic = ChosenCharacteristicKind.Color,
      };
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
