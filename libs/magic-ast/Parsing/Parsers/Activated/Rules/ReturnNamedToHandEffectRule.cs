namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return [CardName] to its owner's hand." — a named-card self-bounce effect where
/// the card refers to itself by its printed name (CR 201.4). Paradigm card: KHM
/// Toralf's Hammer: "… Return Toralf's Hammer to its owner's hand." MAST resolves
/// the self-reference to <see cref="ObjectReferenceKind.Self"/>.
///
/// <para>
/// Distinct from <see cref="ReturnSelfToHandEffectRule"/>, which handles the
/// self-referential pronoun form "Return this [noun] to its owner's hand." This rule
/// handles the card-name form, where the card refers to itself by name rather than by
/// a generic self-referential noun. Both map to the same <c>Self</c> reference.
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent matching a substring of a longer clause. The card name
/// is captured but not stored — all that matters for structuring is that this is a
/// self-bounce effect; the name is engine-resolved at CR 201.4 and not threaded into
/// the AST as a binding variable (ADR 0004: reference-not-resolution).
/// </para>
///
/// <para>
/// Priority 988 — just below <see cref="ReturnSelfToHandEffectRule"/> (989) so the
/// self-referential noun form is tried first; this rule catches the card-name form
/// that falls through.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 988)]
public sealed class ReturnNamedToHandEffectRule : IActivatedEffectRule
{
  // "Return [CardName] to its owner's hand." — the card name starts with an
  // UPPERCASE letter (CR 201.4 — card names are capitalised). Do NOT use
  // RegexOptions.IgnoreCase so [A-Z] retains its case-sensitive meaning:
  // a named card always starts with a capital letter, while generic phrases
  // ("up to one target artifact…", "this card", "a permanent") start lower-case.
  // The trailing period is optional (it may have been stripped by the caller).
  private static readonly Regex _pattern = new(
    @"^Return\s+(?<name>[A-Z][^:]+?)\s+to\s+its\s+owner's\s+hand\.?$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    // The name must be a proper noun (card name), not a description like
    // "up to one target artifact, creature, or enchantment". Guard against:
    // - "this [noun]" / "the [noun]" — handled by ReturnSelfToHandEffectRule at higher priority
    // - "up to" — generic targeting phrase, not a card name
    // - "a [noun]" / "an [noun]" — generic targeting phrase
    // - "target" — targeting phrase, not a card name
    var name = m.Groups["name"].Value.Trim();
    if (name.StartsWith("this ", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("up to", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("target ", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("a ", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return new ReturnToHandEffect
    {
      Target = ObjectReference.Self(),
    };
  }
}
