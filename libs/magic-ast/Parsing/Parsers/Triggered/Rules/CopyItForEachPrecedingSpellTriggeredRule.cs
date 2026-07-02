namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "copy it for each other instant and sorcery spell you've cast before it this
/// turn. You may choose new targets for the copies."
/// — Thousand-Year Storm's triggered effect.
///
/// <para>
/// The copy count equals the number of other instant and sorcery spells the
/// controller cast earlier this turn (the "storm count" for instants/sorceries).
/// Each copy represents one preceding spell in the cast sequence. The count is
/// expressed as a <see cref="CountQuantity"/> over an <see cref="ObjectFilter"/>
/// with <see cref="CastThisTurnPredicate"/> (Caster=You) and
/// <see cref="ObjectFilter.ExcludeSelf"/> = true (the "other" qualifier, excluding
/// the triggering spell itself, CR 109.5).
/// </para>
///
/// <para>
/// "Before it this turn" — the temporal ordering qualifier restricting the count
/// to spells cast earlier in the turn rather than the full "this turn" set —
/// is engine-execution semantics (CR 603.1: a triggered ability fires once the
/// triggering event occurs; the storm-count state is tracked by the engine, not
/// by the AST descriptor). MAST records the "other [filter] spells cast this
/// turn" reference (ExcludeSelf + CastThisTurnPredicate); the ordering is implied
/// by the trigger event itself and not separately encoded.
/// </para>
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to
/// put a copy of it onto the stack; a copy of a spell isn't cast."
/// CR 702.40a: "Storm" is the keyword for the analogous "copy for each spell
/// cast before it this turn" mechanic on a spell itself; Thousand-Year Storm is
/// a standing enchantment that applies a Magecraft-style storm effect to
/// instants and sorceries only.
/// CR 603.1: "Triggered abilities have a trigger condition and an effect."
/// </para>
///
/// <para>
/// "You may choose new targets for the copies" is the structured retarget
/// permission carried on <see cref="CopyEffect.MayChooseNewTargets"/> — a
/// rules-meaningful flag, not free text (CR 707.10c).
/// </para>
///
/// <para>
/// Priority 76: must run BEFORE <see cref="CopyTriggeringSpellTriggeredRule"/>
/// (priority 74), which handles the shorter "copy it. You may choose new targets
/// for the copy." body and would fail to match this longer clause, but to be safe
/// this more-specific rule runs first. The pattern is ANCHORED (^…$) so it cannot
/// match as a substring of a more-specific sibling.
/// </para>
/// </summary>
[TriggeredRule(Priority = 76)]
public sealed class CopyItForEachPrecedingSpellTriggeredRule : ITriggeredRule
{
  // "copy it for each other instant and sorcery spell you've cast before it this
  //  turn[. You may choose new targets for the copies]"
  // The trailing terminal period is stripped by the dispatcher before TryMatch.
  private static readonly Regex _pattern = new(
    @"^copy\s+it\s+for\s+each\s+other\s+instant\s+and\s+sorcery\s+spell\s+you'?ve\s+cast\s+before\s+it\s+this\s+turn"
    + @"(?:\.\s+(?<newtargets>you\s+may\s+choose\s+new\s+targets\s+for\s+the\s+cop(?:y|ies)))?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    // The count: number of other instant and sorcery spells cast before this
    // one this turn. ObjectFilter: card types = spell+instant+sorcery (matching
    // the trigger filter), Controller = You, CastThisTurnPredicate (Caster=You),
    // ExcludeSelf = true (the "other" qualifier, CR 109.5).
    var countQuantity = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = ["spell", "instant", "sorcery"],
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
        History = new CastThisTurnPredicate { Caster = ControllerFilter.You },
      },
    };

    effect = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.It,
      },
      Count = countQuantity,
      MayChooseNewTargets = m.Groups["newtargets"].Success ? true : null,
    };
    return true;
  }
}
