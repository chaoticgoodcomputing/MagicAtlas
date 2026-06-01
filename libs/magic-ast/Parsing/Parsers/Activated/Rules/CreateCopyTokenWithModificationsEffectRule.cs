namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "Create a token that's a copy of target nonlegendary creature you control,
/// except it has haste." — the copy-token line of an activated ability
/// (Kiki-Jiki, Mirror Breaker). Produces a single <see cref="CopyEffect"/> whose
/// <see cref="CopyEffect.Target"/> is the targeted creature and whose
/// <see cref="CopyEffect.Modifications"/> carry the "except" clause(s).
///
/// <para>
/// CR 111.1: "A token is a marker used to represent any permanent that isn't
/// represented by a card." CR 707.2: "When copying an object, the copy acquires
/// the copiable values of the original object's characteristics…" — the "except"
/// clause overrides one of those copiable values; here "it has haste" is an
/// <see cref="AbilityAdder"/> modification. "nonlegendary creature you control"
/// is encoded as a targeted reference with the Legendary supertype negated via
/// <see cref="ObjectFilter.ExcludedSupertypes"/>.
/// </para>
///
/// <para>
/// Reached through <see cref="ActivatedAbilityParser"/>'s multi-sentence pre-pass,
/// which splits Kiki's two-sentence effect body ("Create … except it has haste.
/// Sacrifice it …") and dispatches each sentence independently. The companion
/// delayed-sacrifice sentence is handled by
/// <see cref="SacrificeAtEndStepEffectRule"/>.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 70)]
public sealed class CreateCopyTokenWithModificationsEffectRule : IActivatedEffectRule
{
  // "Create a token that's a copy of target [nonlegendary ]creature you control,
  //  except it has <ability>."
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+token\s+that's\s+a\s+copy\s+of\s+target\s+(?<nonlegendary>nonlegendary\s+)?creature(?:\s+(?<controller>you\s+control))?,\s+except\s+it\s+has\s+(?<ability>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var stripped = effectText.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    var hasController = m.Groups["controller"].Success;
    var isNonlegendary = m.Groups["nonlegendary"].Success;
    var abilityText = m.Groups["ability"].Value.Trim();

    return new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = hasController ? ControllerFilter.You : null,
          ExcludedSupertypes = isNonlegendary ? ["Legendary"] : null,
        },
      },
      Modifications = [new AbilityAdder { AbilityText = abilityText }],
    };
  }
}
