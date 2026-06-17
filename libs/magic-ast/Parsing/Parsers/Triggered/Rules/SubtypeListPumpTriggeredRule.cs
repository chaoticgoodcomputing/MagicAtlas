namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[SubtypeA], [SubtypeB], [SubtypeC], and [SubtypeD] you control get +N/+N until end of turn."
///
/// Handles the multi-subtype tribal pump pattern: a comma-and-"and"-joined list of
/// creature subtypes that the controller's creatures of those types get a temporary
/// P/T boost. The "you control" controller filter is required. Covers both two-item
/// ("[A] and [B] you control") and three-or-more-item ("[A], [B], and [C] you control")
/// forms.
///
/// <para>
/// The subtypes list uses OR semantics on the <see cref="ObjectFilter.Subtypes"/> axis
/// (a creature qualifies if it has ANY of the listed subtypes — CR 205.3m). The boost
/// is recorded as a <see cref="ModifyPTEffect"/> with
/// <see cref="Duration"/> = <c>untilTime{Turn, End}</c>.
/// </para>
///
/// <para>
/// Rule 613.1c (Layer 6, ability-granting effects) and 613.4c (Layer 7c,
/// P/T-modification effects): temporary boosts are layer-7c continuous effects.
/// CR 205.3m: creature subtypes list.
/// </para>
/// </summary>
[TriggeredRule(Priority = 960)]
public sealed class SubtypeListPumpTriggeredRule : ITriggeredRule
{
  // Matches a comma-separated (and optional "and") list of capitalized creature
  // subtype words followed by "you control get +N/+N until end of turn".
  // Requires AT LEAST TWO subtype words (joined by comma or "and") so this rule
  // does not steal the bare "creatures you control get ..." shape handled by
  // EtbTeamPumpTriggeredRule. A single subtype like "Goblins you control get ..."
  // would also be excluded (let LordPTBuffRule or EtbTeamPumpTriggeredRule handle it).
  // Captures the subtype list in group "subtypes" and the P/T values in "p" and "t".
  private static readonly Regex _pattern = new(
    @"^(?<subtypes>(?:[A-Z][a-zA-Z]+)(?:(?:,\s+(?:[A-Z][a-zA-Z]+))+(?:,?\s+and\s+(?:[A-Z][a-zA-Z]+))?|\s+and\s+(?:[A-Z][a-zA-Z]+)))\s+you\s+control\s+get\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var subtypesRaw = m.Groups["subtypes"].Value;
    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);

    // Parse the subtype list: split on commas and "and", then singularize.
    var subtypes = ParseSubtypeList(subtypesRaw);
    if (subtypes.Count == 0)
    {
      return false;
    }

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = subtypes,
          Controller = ControllerFilter.You,
        },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  /// <summary>
  /// Parses a comma-and-"and"-joined list of creature subtype words into
  /// their singular oracle-canonical forms.
  /// "Birds, Frogs, Otters, and Rats" → ["Bird", "Frog", "Otter", "Rat"]
  /// </summary>
  private static IReadOnlyList<string> ParseSubtypeList(string raw)
  {
    // Split on commas and the word "and" (with optional surrounding whitespace).
    var tokens = Regex.Split(raw.Trim(), @"(?:,\s*(?:and\s+)?|\s+and\s+)");
    var result = new List<string>(tokens.Length);
    foreach (var token in tokens)
    {
      var word = token.Trim();
      if (word.Length == 0)
      {
        continue;
      }
      // Singularize: strip trailing "s" for regular plurals, handle known irregulars.
      var singular = Singularize(word);
      if (singular.Length == 0)
      {
        continue;
      }
      result.Add(singular);
    }
    return result;
  }

  private static readonly IReadOnlyDictionary<string, string> _irregularPlurals =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Elves"] = "Elf",
      ["Mice"] = "Mouse",
      ["Wolves"] = "Wolf",
      ["Dwarves"] = "Dwarf",
      ["Loci"] = "Locus",
      ["Djinn"] = "Djinn",
    };

  private static string Singularize(string plural)
  {
    if (_irregularPlurals.TryGetValue(plural, out var singular))
    {
      return singular;
    }
    // Regular plural: strip trailing "s".
    if (plural.EndsWith('s') && plural.Length > 1)
    {
      return plural[..^1];
    }
    return plural;
  }
}
