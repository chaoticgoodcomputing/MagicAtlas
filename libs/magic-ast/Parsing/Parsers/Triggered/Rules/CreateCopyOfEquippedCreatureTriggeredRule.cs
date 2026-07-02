namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "create a token that's a copy of equipped creature, except the token isn't
/// legendary. That token gains haste." — Helm of the Host's combat trigger
/// resolution. Produces a single <see cref="CopyEffect"/> whose
/// <see cref="CopyEffect.Target"/> is the equipped creature and whose
/// <see cref="CopyEffect.Modifications"/> carry both "except"-style overrides on
/// the copy: it isn't legendary (<see cref="SupertypeRemover"/>) and it gains
/// haste (<see cref="AbilityAdder"/>).
///
/// <para>
/// CR 111.1: "A token is a marker used to represent any permanent that isn't
/// represented by a card." CR 707.2: "When copying an object, the copy acquires
/// the copiable values of the original object's characteristics…" — the two
/// modifications override copiable values the token would otherwise inherit. The
/// trailing "That token gains haste." sentence is folded into the same
/// <see cref="CopyEffect"/> as a haste modification (one oracle line → one copy
/// with its modifications), not split into a separate gain-ability effect.
/// "equipped creature" is the
/// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> reference (CR 301.5c).
/// </para>
///
/// <para>
/// Priority 70 — more specific than the generic create-token rules so the
/// "copy of equipped creature" phrasing is claimed here first. The rule matches
/// the whole two-sentence body; the dispatcher's sentence-bundle pre-pass bails
/// (the "That token gains haste" sentence has no standalone rule), so the full
/// body reaches this rule intact.
/// </para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class CreateCopyOfEquippedCreatureTriggeredRule : ITriggeredRule
{
  // Two-sentence body, terminal period already stripped by the dispatcher:
  //   "create a token that's a copy of equipped creature, except the token isn't legendary.
  //    That token gains haste"
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+token\s+that's\s+a\s+copy\s+of\s+equipped\s+creature,\s+except\s+the\s+token\s+isn't\s+legendary\.\s+that\s+token\s+gains\s+(?<ability>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var abilityText = m.Groups["ability"].Value.Trim();

    effect = new CopyEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      Modifications =
      [
        new SupertypeRemover { Supertypes = ["Legendary"] },
        new AbilityAdder { AbilityText = abilityText },
      ],
    };
    return true;
  }
}
