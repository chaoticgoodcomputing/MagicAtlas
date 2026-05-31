namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may return this card from your graveyard to your hand" — graveyard-based
/// self-retrieval triggered from the graveyard zone (Rule 603.6a: a triggered
/// ability on a card in a graveyard functions from that zone). The Eidolon family
/// (Dissension) is the canonical corpus for this pattern.
///
/// The source zone (Graveyard) is encoded on the Target filter's Zone axis rather
/// than on the ability itself — MAST describes what the oracle text says, not where
/// the object resides at runtime (Rule 400 series).
///
/// Example corpus patterns:
///   "you may return this card from your graveyard to your hand"
/// </summary>
[TriggeredRule]
public sealed class ReturnSelfFromGraveyardTriggeredRule : ITriggeredRule
{
  // Matches: "you may return this card from your graveyard to your hand"
  // The leading "you may" is structurally required for this oracle pattern
  // (the entire Eidolon family uses it); IsOptional is always true here.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+return\s+this\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new ReturnToHandEffect {
      // "this card" — self-reference; Zone = Graveyard encodes the source zone
      // stated in the oracle text ("from your graveyard").
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Self,
        Filter = new ObjectFilter
        {
          Zone = Zone.Graveyard,
        },
      }}, true);
    return true;
  }
}
