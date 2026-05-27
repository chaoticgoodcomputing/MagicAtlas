namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Handles the "discard their hand" / "discard your hand" spell-effect surface,
/// with an optional ", then draws [N] cards" continuation.
///
/// <para>Covered oracle shapes:</para>
/// <list type="bullet">
///   <item>"Each player discards their hand, then draws seven cards." (Wheel of Fortune)</item>
///   <item>"Target player discards their hand, then draws four cards." (Wheel and Deal)</item>
///   <item>"Each player discards their hand, then draws cards equal to the greatest number of cards a player discarded this way." (Windfall)</item>
///   <item>Standalone variants without a draw continuation: "Each player discards their hand.", "Discard your hand."</item>
/// </list>
///
/// <para>
/// The discard count is modelled as a <see cref="DerivedQuantity"/> with
/// <see cref="DerivedKind.CardsInHand"/> — descriptively, "hand" in oracle text means
/// "all cards in your hand at the time the effect resolves."
/// </para>
/// </summary>
[SpellRule]
public sealed class DiscardHandSpellRule : ISpellRule, IMultiSpellRule
{
  // -------------------------------------------------------------------------
  // Regex patterns
  // -------------------------------------------------------------------------

  /// <summary>
  /// Captures subject, discard verb, and optional draw continuation.
  /// Groups: subject, drawCount (literal word/digit), drawExpr (equal-to phrase).
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^(?<subject>Each\s+player|Target\s+player|You)\s+discard(?:s)?\s+(?:your|their)\s+hand(?:,\s*then\s+draw(?:s)?\s+(?:(?<drawCount>[a-z]+|\d+)\s+cards?|cards?\s+equal\s+to\s+(?<drawExpr>.+)))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path (standalone discard with no draw).
  // Returns false when a draw continuation is present; the multi-rule path
  // handles those via TryMatchMulti.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }
    // If a draw continuation is present, delegate to TryMatchMulti.
    if (m.Groups["drawCount"].Success || m.Groups["drawExpr"].Success)
    {
      return false;
    }

    var player = ResolvePlayer(m.Groups["subject"].Value);
    effect = new DiscardCardsEffect
    {
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.CardsInHand },
      Player = player,
      Random = false,
    };
    return true;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — two-effect list when a draw continuation is present.
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }
    if (!m.Groups["drawCount"].Success && !m.Groups["drawExpr"].Success)
    {
      return false;
    }

    var player = ResolvePlayer(m.Groups["subject"].Value);
    var discard = new DiscardCardsEffect
    {
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.CardsInHand },
      Player = player,
      Random = false,
    };

    Quantity drawCount;
    if (m.Groups["drawCount"].Success)
    {
      var raw = m.Groups["drawCount"].Value;
      if (!SpellRuleHelpers.TryParseSmallWord(raw, out var n))
      {
        return false;
      }
      drawCount = LiteralQuantity.Of(n);
    }
    else
    {
      // "cards equal to <expression>" — CalculatedQuantity preserves the full phrase.
      drawCount = new CalculatedQuantity { Expression = m.Groups["drawExpr"].Value.Trim() };
    }

    var draw = new DrawCardsEffect
    {
      Count = drawCount,
      Player = player,
    };

    effects = new List<Effect> { discard, draw };
    return true;
  }

  // -------------------------------------------------------------------------
  // Subject → ObjectReference
  // -------------------------------------------------------------------------
  private static ObjectReference ResolvePlayer(string subject)
  {
    return subject.ToLowerInvariant() switch
    {
      "each player" => new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
      "target player" => new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
      _ => ObjectReference.You(), // "you"
    };
  }
}
