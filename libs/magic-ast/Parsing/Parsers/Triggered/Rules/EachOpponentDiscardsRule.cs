namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each opponent discards a card" — ETB discard effect imposed on all opponents
/// simultaneously. Rule 701.9 (Discard). MAST records the discard event and the
/// player scope (EachOpponent); the simultaneous discard order for multiplayer is
/// engine territory, not described by the oracle text.
///
/// <para>
/// This rule covers the bare single-card form only. Counts other than one
/// (e.g., "each opponent discards two cards") are a separate shape handled
/// by extension once corpus pressure warrants.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class EachOpponentDiscardsRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(1),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
      IsOptional = false,
    };
    return true;
  }

  // Matches the exact canonical oracle form: "each opponent discards a card[.]"
  // Terminal period already stripped by the dispatcher before matching.
  private static readonly Regex _pattern = new(
    @"^each\s+opponent\s+discards\s+a\s+card\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
