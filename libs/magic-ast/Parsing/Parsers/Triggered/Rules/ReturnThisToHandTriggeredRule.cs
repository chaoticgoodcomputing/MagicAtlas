namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return this [type] to its owner's hand" — a self-referential bounce where
/// the card names its own type word ("this enchantment", "this creature",
/// "this artifact"...) to refer to itself. Resolves to
/// <see cref="ObjectReferenceKind.Self"/> — the card moving its own zone with no
/// target (Rule 115.1 — "target" keyword absent, so no target is chosen).
///
/// Higher priority than <see cref="ReturnToHandRule"/> so the self-reference
/// "this [type]" is recognised as Self rather than mis-read as an indefinite
/// "a [type] you control" choice (ObjectReferenceKind.Any). The "this [object]"
/// phrasing is the oracle convention for a card referring to itself; modelling it
/// as Self keeps the AST descriptively faithful to what the text says.
///
/// Example corpus pattern:
///   "return this enchantment to its owner's hand"
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ReturnThisToHandTriggeredRule : ITriggeredRule
{
  // Matches: "[you may ]return this <type> to its owner's hand".
  // Optional leading "you may" handles the optional-bounce variant.
  private static readonly Regex _pattern = new(
    @"^(?<opt>you\s+may\s+)?return\s+this\s+(?:creature|permanent|artifact|enchantment|land|planeswalker|aura|equipment|vehicle)\s+to\s+its\s+owner'?s\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new ReturnToHandEffect {
      // "this [type]" = self-reference to the card carrying this ability.
      Target = ObjectReference.Self()}, match.Groups["opt"].Success);
    return true;
  }
}
