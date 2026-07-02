namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target creature with [filter]." — destroy a single creature that
/// satisfies an intrinsic-characteristic filter appended via "with".
///
/// Handles two filter shapes:
/// <list type="bullet">
///   <item>
///     <b>Keyword filter:</b> "with flying", "with defender", "with trample" →
///     <see cref="ObjectFilter.Characteristics"/> gets the keyword (lowercased).
///     Examples: Wing Snare ("Destroy target creature with flying.").
///   </item>
///   <item>
///     <b>Power comparison:</b> "with power N or greater / N or less" →
///     <see cref="ObjectFilter.PowerComparison"/> with
///     <see cref="ComparisonOperator.GreaterThanOrEqual"/> /
///     <see cref="ComparisonOperator.LessThanOrEqual"/>.
///     Examples: Smite the Monstrous ("Destroy target creature with power 4 or greater.").
///   </item>
///   <item>
///     <b>Toughness comparison:</b> "with toughness N or greater / N or less" →
///     <see cref="ObjectFilter.ToughnessComparison"/>.
///   </item>
/// </list>
///
/// This rule fires before <see cref="DestroyTargetSimpleRule"/> would silently reject
/// these phrases; <see cref="SpellRuleHelpers.ParseTargetFilter"/> cannot parse
/// multi-word "creature with X" phrases.
/// </summary>
[SpellRule]
public sealed class DestroyTargetWithFilterRule : ISpellRule
{
  // Anchored to "creature with …" so it does not shadow rules for
  // other card types (artifacts, lands, etc.) if those are added later.
  private static readonly Regex WithPattern = new(
    @"^Destroy\s+target\s+creature\s+with\s+(?<filter>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // "power N or greater" / "power N or less"
  private static readonly Regex PowerPattern = new(
    @"^power\s+(?<n>\d+)\s+or\s+(?<dir>greater|less)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // "toughness N or greater" / "toughness N or less"
  private static readonly Regex ToughnessPattern = new(
    @"^toughness\s+(?<n>\d+)\s+or\s+(?<dir>greater|less)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // A bare keyword (no spaces, not a stat comparison): "flying", "defender", etc.
  private static readonly Regex KeywordPattern = new(
    @"^[A-Za-z]+$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var raw = text.Trim().TrimEnd('.');
    var m = WithPattern.Match(raw);
    if (!m.Success)
    {
      return false;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();

    // --- Power comparison ---
    var power = PowerPattern.Match(filterPhrase);
    if (power.Success)
    {
      var value = int.Parse(power.Groups["n"].Value);
      var op = power.Groups["dir"].Value.Equals("greater", StringComparison.OrdinalIgnoreCase)
        ? ComparisonOperator.GreaterThanOrEqual
        : ComparisonOperator.LessThanOrEqual;

      effect = new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            PowerComparison = new Comparison { Operator = op, Value = value },
          },
        },
      };
      return true;
    }

    // --- Toughness comparison ---
    var toughness = ToughnessPattern.Match(filterPhrase);
    if (toughness.Success)
    {
      var value = int.Parse(toughness.Groups["n"].Value);
      var op = toughness.Groups["dir"].Value.Equals("greater", StringComparison.OrdinalIgnoreCase)
        ? ComparisonOperator.GreaterThanOrEqual
        : ComparisonOperator.LessThanOrEqual;

      effect = new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            ToughnessComparison = new Comparison { Operator = op, Value = value },
          },
        },
      };
      return true;
    }

    // --- Bare keyword filter ---
    if (KeywordPattern.IsMatch(filterPhrase))
    {
      effect = new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [Characteristic.FromLabel(filterPhrase.ToLowerInvariant())],
          },
        },
      };
      return true;
    }

    // Filter phrase was not one of the recognised shapes.
    return false;
  }
}
