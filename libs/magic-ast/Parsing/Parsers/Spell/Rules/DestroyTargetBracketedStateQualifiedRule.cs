namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [attacking] creature." — the Cleave-keyword (CR 702.148a)
/// printed-markup form of the state-qualified destroy shape, where the state
/// qualifier is wrapped in literal square brackets on the printed card (removed
/// only if the spell's cleave cost was paid). E.g. Fierce Retribution's second
/// line: "Destroy target [attacking] creature."
///
/// <para>
/// Modeled identically to the un-bracketed sibling
/// <see cref="DestroyTargetStateQualifiedRule"/> — "model the effect as printed"
/// — because the brackets are Cleave-removal markup, not rules text; the printed
/// (unpaid-cleave) reading of the spell is exactly "Destroy target attacking
/// creature." Kept as a separate rule (rather than editing the sibling regex) so
/// the bracket-anchored pattern can never collide with the un-bracketed one.
/// </para>
/// </summary>
[SpellRule]
public sealed class DestroyTargetBracketedStateQualifiedRule : ISpellRule
{
  // Matches "Destroy target [<state>] creature" — literal square brackets around
  // the state qualifier, per the Cleave printed-markup convention.
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+\[(?<state>tapped|attacking\s+or\s+blocking|attacking|blocking)\]\s+creature$",
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

    var state = Regex.Replace(m.Groups["state"].Value.Trim(), @"\s+", " ").ToLowerInvariant();

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.FromLabel(state)],
        },
      },
    };
    return true;
  }
}
