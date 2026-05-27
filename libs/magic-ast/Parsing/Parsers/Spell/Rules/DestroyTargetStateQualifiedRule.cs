namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [state] creature." — handles state-based qualifiers that
/// describe a creature's current game status rather than its card-type or color.
/// <list type="bullet">
///   <item>"Destroy target tapped creature." — e.g., Vengeance</item>
///   <item>"Destroy target attacking creature." — e.g., Immolating Glare</item>
///   <item>"Destroy target blocking creature."</item>
///   <item>"Destroy target attacking or blocking creature."</item>
/// </list>
/// State words go into <see cref="ObjectFilter.Characteristics"/> per the
/// established open-ended-qualifier convention (CR 109.3 — characteristics
/// include any quality that can be true or false of a permanent at any moment).
/// </summary>
[SpellRule]
public sealed class DestroyTargetStateQualifiedRule : ISpellRule
{
  // Matches "Destroy target <state> creature" where <state> is one of the
  // recognised game-state qualifiers. The "attacking or blocking" disjunction
  // is captured as a single qualifier token.
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+(?<state>tapped|attacking\s+or\s+blocking|attacking|blocking)\s+creature$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    // Normalise whitespace in the captured qualifier ("attacking or blocking"
    // may have irregular spacing from the source; collapse to single spaces).
    var state = Regex.Replace(m.Groups["state"].Value.Trim(), @"\s+", " ").ToLowerInvariant();

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [state],
        },
      },
    };
    return true;
  }
}
