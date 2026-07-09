namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap that creature." — the definite-article back-reference form of a trailing
/// untap clause, used as the closing sentence of a multi-sentence spell that already
/// named "target creature" (or a filtered variant of it) earlier in the same effect
/// text: "Target creature gets +2/+2 until end of turn. Untap that creature."
/// (Savage Surge, Veteran's Reflexes), "Put a +1/+1 counter on target creature you
/// control. Untap that creature." (Stony Strength).
///
/// "That creature" here is anaphoric within a single spell's resolution — it refers
/// back to the object chosen for the "target creature" earlier in the SAME sentence
/// chain, not to an object named by a trigger condition (contrast
/// <see cref="ObjectReferenceKind.ThatCreature"/>, which is reserved for the
/// triggered-ability back-reference to the trigger's own Filter — CR 603.2). Per Rule
/// 109.2, an anaphoric reference to a previously-established object uses the same
/// "it" semantics regardless of whether the card's own text spells it "it" or "that
/// creature" — this mirrors the established convention already used by
/// <see cref="ThreatenRule"/> for the identical "Untap that creature." clause
/// ("'That creature' ... maps to ObjectReferenceKind.It (same pronoun semantics as
/// 'it' — Rule 109.2)").
///
/// Anchored on the bare fragment (^…$) so only the standalone trailing sentence
/// matches; any trailing qualifier ("Untap that creature and roll a six-sided die.")
/// falls through untouched to its own handling.
/// </summary>
[SpellRule]
public sealed class UntapThatCreatureRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Untap\s+that\s+creature$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new UntapEffect { Target = new ObjectReference { Kind = ObjectReferenceKind.It } };
    return true;
  }
}
