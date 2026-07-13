namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Tap that creature." — the definite-article back-reference form of a trailing tap
/// clause, used as the closing sentence of a multi-sentence spell that already named
/// "target creature" earlier in the same effect text: "Target creature becomes white
/// until end of turn. Tap that creature." (Niveous Wisps).
///
/// "That creature" here is anaphoric within a single spell's resolution — it refers
/// back to the object chosen for the "target creature" earlier in the SAME sentence
/// chain (CR 109.2), not to an object named by a trigger condition (contrast
/// <see cref="ObjectReferenceKind.ThatCreature"/>, which is reserved for the
/// triggered-ability back-reference to the trigger's own Filter). Mirrors
/// <see cref="UntapThatCreatureRule"/>'s identical mapping to
/// <see cref="ObjectReferenceKind.It"/> for the "Untap that creature." shape.
///
/// Anchored on the bare fragment (^…$) so only the standalone trailing sentence
/// matches; any trailing qualifier falls through untouched to its own handling.
/// </summary>
[SpellRule]
public sealed class TapThatCreatureRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Tap\s+that\s+creature$",
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

    effect = new TapEffect { Target = new ObjectReference { Kind = ObjectReferenceKind.It } };
    return true;
  }
}
