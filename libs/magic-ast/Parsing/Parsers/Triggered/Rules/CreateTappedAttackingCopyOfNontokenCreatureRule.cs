namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create a tapped and attacking token that's a copy of up to one other target nontoken
/// creature you control" — Satya, Aetherflux Genius's attack-trigger copy effect
/// (CR 111.1: token creation; CR 508.1k: token enters as an attacking creature declared
/// in the same attack step; CR 707.2: the token copies the copiable values of the target).
///
/// <para>
/// "Up to one" is encoded as <see cref="UpToQuantity"/> on the
/// <see cref="ObjectReference.Quantity"/> (maximum 1, minimum 0). "Other" excludes
/// the source creature (<see cref="ObjectFilter.ExcludeSelf"/> = true). "Nontoken"
/// is the <see cref="ObjectFilter.IsToken"/> = false axis (CR 111: a token is not a
/// card; "nontoken" means the object is not a token).
/// </para>
///
/// <para>
/// The "tapped and attacking" qualifier is contextually implicit from the attack trigger
/// and is not serialised as a separate field on <see cref="CopyEffect"/> — matching the
/// convention established by Kari Zev, Skyship Raider's gold fixture, where the same
/// phrasing on a <see cref="CreateTokenEffect"/> is also omitted.
/// </para>
///
/// <para>
/// ANCHORED (^…$): prevents spurious matches inside a longer copy-token sentence
/// that happens to contain this phrase as a substring. Priority 75 — above the
/// generic <see cref="CreateCopyOnCombatDamageTriggeredRule"/> (70) and well above
/// the <see cref="CreateTokenRule"/> fallback (50); specific enough to be tried
/// before any broader "create a … copy" path.
/// </para>
///
/// <para>
/// Rule citations: CR 111.1 (token creation), CR 508.1k (attacking token), CR 707.2
/// (copy semantics), CR 107.14 (energy), CR 603.7 (delayed triggered ability).
/// </para>
/// </summary>
[TriggeredRule(Priority = 75)]
public sealed class CreateTappedAttackingCopyOfNontokenCreatureRule : ITriggeredRule
{
  // "create a tapped and attacking token that's a copy of up to one other target
  // nontoken creature you control"
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+tapped\s+and\s+attacking\s+token\s+that(?:'s|'s)\s+a\s+copy\s+of\s+up\s+to\s+one\s+other\s+target\s+nontoken\s+creature\s+you\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    // CR 707.2: the token is a copy of the target nontoken creature.
    // CR 111.1: tokens are created on the battlefield.
    // "up to one" → UpToQuantity { Maximum = 1, Minimum = 0 }.
    // "other" → ExcludeSelf = true (excludes the ability's source, Satya).
    // "nontoken" → IsToken = false.
    effect = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          IsToken = false,
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
        },
        Quantity = new UpToQuantity { Maximum = 1, Minimum = 0 },
      },
    };
    return true;
  }
}
