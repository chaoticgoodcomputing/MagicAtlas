namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target nonbasic land." — single-target destroy of a nonbasic land as an
/// activated-ability effect (Dwarven Miner, Dwarven Blastminer, Rishadan Cutpurse-style
/// land denial).
///
/// The "nonbasic" qualifier is modeled on the structured supertype-negation axis rather
/// than as free text: a nonbasic land is a land WITHOUT the "basic" supertype
/// (CR 205.4c: "Any land with the supertype 'basic' is a basic land. Any land that
/// doesn't have this supertype is a nonbasic land, even if it has a basic land type.").
/// Hence <c>CardTypes=["land"]</c> + <c>ExcludedSupertypes=["Basic"]</c>, parallel to
/// how "nonlegendary creature" is encoded (see
/// <see cref="ObjectFilter.ExcludedSupertypes"/>).
///
/// CR 701.8a: "To destroy a permanent, move it from the battlefield to its owner's
/// graveyard."
///
/// GUARD: anchored to exactly "Destroy target nonbasic land". The bare-type sibling
/// <see cref="DestroyTargetEffectRule"/> (Priority 600) matches "Destroy target land"
/// but NOT "nonbasic land" (its type group has no "nonbasic land" alternative), so the
/// two rules are disjoint; this rule sits at a higher priority only to make precedence
/// explicit.
/// </summary>
[ActivatedEffectRule(Priority = 610)]
public sealed class DestroyTargetNonbasicLandEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+nonbasic\s+land\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    if (!Pattern.IsMatch(effectText.Trim()))
    {
      return null;
    }

    return new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["land"],
          ExcludedSupertypes = ["Basic"],
        },
      },
    };
  }
}
