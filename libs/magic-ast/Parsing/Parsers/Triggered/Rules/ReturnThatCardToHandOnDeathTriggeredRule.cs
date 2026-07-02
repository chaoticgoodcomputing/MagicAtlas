namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return that card to its owner's hand" — the Demonic Vigor pattern. This effect
/// appears as the resolution clause of a "When enchanted creature dies" trigger on
/// Auras whose enchanted host is returned to hand instead of the battlefield.
///
/// Glossary "Dies": 'A creature or planeswalker "dies" if it is put into a graveyard
/// from the battlefield. See rule 700.4.' CR 700.4: 'The term dies means "is put into
/// a graveyard from the battlefield."'
///
/// "That card" is a pronoun back-reference to the enchanted creature that just moved
/// from the battlefield to the graveyard. CR 400.7: 'An object that moves from one
/// zone to another becomes a new object with no memory of, or relation to, its
/// previous existence.' MAST models this anaphoric reference as
/// <see cref="ObjectReferenceKind.It"/> — the generic back-reference kind ("it",
/// "that card", "the creature" all denote the same game object).
///
/// Variant: "return that card to the battlefield under your control" is handled by
/// <see cref="ReturnEnchantedOnDeathTriggeredRule"/>; "return it to its owner's hand"
/// (self-referential Auras) is handled by
/// <see cref="ReturnSelfToHandOnDeathTriggeredRule"/>. This rule closes the remaining
/// "that card" x hand quadrant.
///
/// Example corpus patterns:
///   "return that card to its owner's hand"
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ReturnThatCardToHandOnDeathTriggeredRule : ITriggeredRule
{
  // Matches: "return that card to its owner's hand"
  // Optional leading "you may" for optional-bounce variants (none in current
  // corpus, but included for forward-compatibility and corpus consistency).
  private static readonly Regex _pattern = new(
    @"^(?<opt>you\s+may\s+)?return\s+that\s+card\s+to\s+its\s+owner'?s\s+hand$",
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

    var isOptional = match.Groups["opt"].Success;

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new ReturnToHandEffect {
      // "that card" = pronoun back-reference to the enchanted creature (now in graveyard).
      Target = ObjectReference.It()}, isOptional);
    return true;
  }
}
