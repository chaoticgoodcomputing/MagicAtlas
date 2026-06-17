namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap them." — a trailing sentence in a multi-sentence triggered ability
/// resolution that untaps a group of permanents named in the preceding sentence.
///
/// <para>
/// The pronoun "them" is a plural anaphoric back-reference (CR 109.2 — game objects)
/// to the set of permanents named by the prior effect clause in the same triggered
/// ability. Because MAST is descriptive, not imperative (ADR-0003), and
/// <see cref="TryParseSentenceBundleEffects"/> dispatches each sentence independently,
/// this rule models the untap target using the <see cref="ObjectReferenceKind.It"/>
/// pronoun reference — the universal anaphoric stand-in for "the set just named"
/// (the distinction between singular "it" and plural "them" is a linguistic
/// surface form; the reference kind is the same structured back-reference slot).
/// </para>
///
/// <para>
/// Rule 701.20 (Tap and Untap): "To untap a permanent, rotate it back to the
/// upright position from a sideways position. Only tapped permanents can be untapped."
/// Rule 109.2 (Objects): an object is a card, token, spell, or ability; pronouns
/// in oracle text refer to objects in the resolution context.
/// </para>
/// </summary>
[TriggeredRule(Priority = 955)]
public sealed class UntapThemTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^\s*[Uu]ntap\s+them\.?\s*$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new UntapEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
    };
    return true;
  }
}
