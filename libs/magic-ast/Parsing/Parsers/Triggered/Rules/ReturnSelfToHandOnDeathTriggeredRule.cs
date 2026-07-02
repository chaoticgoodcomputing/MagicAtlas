namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return it to its owner's hand" — self-referential bounce that appears on
/// Aura (and occasionally other) cards whose death trigger returns themselves
/// to hand. The pronoun "it" back-references the triggering object named in
/// the trigger condition ("this Aura", "this artifact", etc.). There is no
/// formal target (Rule 115.1 — "target" keyword absent); the card simply moves
/// zone-change without targeting.
///
/// Example corpus patterns:
///   "return it to its owner's hand"
/// </summary>
[TriggeredRule]
public sealed class ReturnSelfToHandOnDeathTriggeredRule : ITriggeredRule
{
  // Matches: "return it to its owner's hand"
  // Optional leading "you may" for optional-bounce variants (none in current
  // corpus, but included for forward-compatibility and corpus consistency).
  private static readonly Regex _pattern = new(
    @"^(?<opt>you\s+may\s+)?return\s+it\s+to\s+its\s+owner'?s\s+hand$",
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
      // "it" = pronoun back-reference to the triggering object (this Aura/artifact/etc.)
      Target = ObjectReference.It()}, isOptional);
    return true;
  }
}
