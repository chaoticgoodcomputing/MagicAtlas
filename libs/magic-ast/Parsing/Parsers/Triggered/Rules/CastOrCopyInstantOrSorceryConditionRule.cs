namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you cast or copy an instant or sorcery spell" — the Magecraft
/// ability-word trigger condition (CR 207.2c: "magecraft" is listed as an ability word
/// with no rules meaning of its own; the trigger text is the mechanical definition).
///
/// <para>
/// CR 707.10: "To copy a spell … means to put a copy of it onto the stack; a copy of a
/// spell isn't cast." The disjunction "cast or copy" therefore covers both the casting
/// event and the copy event, which is why this rule produces
/// <see cref="TriggerEvent.CastOrCopySpell"/> rather than <see cref="TriggerEvent.SpellCast"/>.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>) on the trigger text after the timing word so that the matcher
/// cannot fire as a substring inside a more-specific sibling (e.g. a bare "cast a spell"
/// trigger). Priority 999 — above <see cref="SpellCastConditionRule"/> (998) so the
/// more-specific "cast or copy" disjunction is recognised before the plain "cast" rule
/// can claim the text.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class CastOrCopyInstantOrSorceryConditionRule : ITriggerConditionRule
{
  // Anchored: matches the full trigger text (including the timing word).
  // Pattern: "Whenever/When [subject] cast or copy an instant or sorcery spell"
  // Accepts any controller phrase for forward-compat (only "you" appears in current corpus).
  private static readonly Regex _pattern = new(
    @"^(?:whenever|when)\s+(?<subject>you|an?\s+opponent)\s+cast\s+or\s+cop(?:y|ied)\s+an?\s+instant\s+or\s+sorcery\s+spell$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick guard before allocating a Regex match
    if (!lower.Contains("cast or cop"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var subject = m.Groups["subject"].Value.ToLowerInvariant().Trim();
    var controller = subject.StartsWith("opponent")
      ? ControllerFilter.Opponent
      : ControllerFilter.You;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.CastOrCopySpell,
      Filter = new ObjectFilter
      {
        // "instant or sorcery spell" — the type disjunction lands on CardTypes
        // via QualifierAxisMapper convention (both "instant" and "sorcery" are in
        // _cardTypes, so they expand into the CardTypes list alongside "spell").
        CardTypes = ["spell", "instant", "sorcery"],
        Controller = controller,
      },
    };
  }
}
