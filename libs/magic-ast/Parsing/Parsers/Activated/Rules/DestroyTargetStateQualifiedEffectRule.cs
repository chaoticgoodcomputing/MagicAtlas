namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [state] creature." as an activated-ability effect — handles
/// game-state qualifiers that describe a creature's current combat/tap status
/// rather than its card-type or color.
/// <list type="bullet">
///   <item>"Destroy target attacking creature." — e.g., Mine Bearer</item>
///   <item>"Destroy target tapped creature."</item>
///   <item>"Destroy target blocking creature."</item>
///   <item>"Destroy target attacking or blocking creature."</item>
/// </list>
/// State words go into <see cref="ObjectFilter.Characteristics"/>, mirroring
/// the spell-side <c>DestroyTargetStateQualifiedRule</c> convention (e.g.,
/// Immolating Glare's "Destroy target attacking creature.").
///
/// CR 701.8a: "To destroy a permanent, move it from the battlefield to its
/// owner's graveyard."
/// </summary>
[ActivatedEffectRule(Priority = 600)]
public sealed class DestroyTargetStateQualifiedEffectRule : IActivatedEffectRule
{
  // Matches "Destroy target <state> creature" where <state> is one of the
  // recognised game-state qualifiers. The "attacking or blocking" disjunction
  // is captured as a single qualifier token.
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+(?<state>tapped|attacking\s+or\s+blocking|attacking|blocking)\s+creature\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return null;
    }

    // Normalise whitespace in the captured qualifier ("attacking or blocking"
    // may have irregular spacing from the source; collapse to single spaces).
    var state = Regex.Replace(m.Groups["state"].Value.Trim(), @"\s+", " ").ToLowerInvariant();

    return new DestroyEffect
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
  }
}
