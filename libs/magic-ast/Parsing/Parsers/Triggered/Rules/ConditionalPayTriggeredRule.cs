namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Handles "you may pay {COST}. If you do, [effect]." conditional-pay triggers
/// and the discard-as-cost loot variant "you may discard a card. If you do,
/// draw a card."
///
/// <para>Two shapes:</para>
/// <list type="bullet">
///   <item><description>
///     <b>Mana-pay shape</b>: "you may pay {N}. If you do, [EFFECT]." →
///     <see cref="ConditionalPayEffect"/> with the mana cost on
///     <c>Cost</c> and the parsed consequent on <c>IfYouDo</c>.
///     Example: "Whenever a creature dies, you may pay {1}. If you do,
///     you gain 1 life." (Deathgreeter, ALA).
///   </description></item>
///   <item><description>
///     <b>Discard-loot shape</b>: "you may discard [N] card(s). If you do,
///     draw [M] card(s)." → <see cref="DiscardCardsEffect"/> with
///     <c>IsOptional = true</c> and <c>IfYouDo = DrawCardsEffect</c>.
///     Mirrors the spell-level <c>DiscardThenDrawSpellRule</c>.
///   </description></item>
/// </list>
///
/// <para>
/// Priority 80: both shapes contain the trigger text "you may [action]. If
/// you do," as a prefix. Without elevated priority the discard-loot shape
/// would race against the plain <see cref="EachOpponentDiscardsRule"/>.
/// </para>
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class ConditionalPayTriggeredRule : ITriggeredRule
{
  // Lazy singletons for inner-effect delegation (pattern from ReturnToHandWithIfYouDoGainLifeRule).
  private static readonly YouGainLifeRule _gainLifeRule = new();
  private static readonly DrawCardsTriggeredRule _drawCardsRule = new();
  private static readonly YouLoseLifeRule _loseLifeRule = new();
  private static readonly PutCountersTriggeredRule _putCountersRule = new();
  private static readonly ScryTriggeredRule _scryRule = new();
  private static readonly SurveilTriggeredRule _surveilRule = new();
  private static readonly EachOpponentLosesLifeTriggeredRule _eachOpponentLosesLifeRule = new();

  // ── Pattern 1: "you may pay {COST}. If you do, [effect]" ──────────────
  // The cost is one or more {X} symbols; the consequent effect is everything
  // after "If you do," (terminal period already stripped by dispatcher).
  private static readonly Regex _manaPayPattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\.\s*If\s+you\s+do,\s*(?<rest>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // ── Pattern 2: "you may discard [N] card(s). If you do, draw [M] card(s)" ──
  private static readonly Regex _discardDrawPattern = new(
    @"^you\s+may\s+discard\s+(?<dn>a|one|two|three|four|five|\d+)\s+cards?\s*\.\s*If\s+you\s+do,\s*draw\s+(?<rn>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // ── Discard-loot shape (try first — more specific regex anchors) ──────
    var loot = _discardDrawPattern.Match(text);
    if (loot.Success)
    {
      var discardCount = TriggeredRuleHelpers.ParseWordOrDigitCount(loot.Groups["dn"].Value) ?? 1;
      var drawCount = TriggeredRuleHelpers.ParseWordOrDigitCount(loot.Groups["rn"].Value) ?? 1;

      var draw = new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(drawCount),
        Player = ObjectReference.You(),
      };
      effect = new DiscardCardsEffect
      {
        Count = LiteralQuantity.Of(discardCount),
        Player = ObjectReference.You(),
        Random = false,
        IsOptional = true,
        IfYouDo = draw,
      };
      return true;
    }

    // ── Mana-pay shape ────────────────────────────────────────────────────
    var pay = _manaPayPattern.Match(text);
    if (!pay.Success)
    {
      return false;
    }

    var costStr = pay.Groups["cost"].Value;
    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(costStr);
    if (manaCost is null)
    {
      return false;
    }

    var restText = pay.Groups["rest"].Value.TrimEnd('.').Trim();
    var ifYouDo = TryParseIfYouDoEffect(restText);
    if (ifYouDo is null)
    {
      return false;
    }

    effect = new ConditionalPayEffect
    {
      Cost = manaCost,
      IsOptional = true,
      IfYouDo = ifYouDo,
    };
    return true;
  }

  /// <summary>
  /// Dispatches the "if you do" consequent text through common triggered
  /// effect rules. Follows the delegation pattern in
  /// <see cref="ReturnToHandWithIfYouDoGainLifeRule"/>: each candidate rule
  /// is tried in turn; the first match wins.
  /// </summary>
  private Effect? TryParseIfYouDoEffect(string text)
  {
    if (_gainLifeRule.TryMatch(text, out var gainLife) && gainLife is not null) return gainLife;
    if (_drawCardsRule.TryMatch(text, out var draw) && draw is not null) return draw;
    if (_loseLifeRule.TryMatch(text, out var loseLife) && loseLife is not null) return loseLife;
    if (_putCountersRule.TryMatch(text, out var counters) && counters is not null) return counters;
    if (_scryRule.TryMatch(text, out var scry) && scry is not null) return scry;
    if (_surveilRule.TryMatch(text, out var surveil) && surveil is not null) return surveil;
    if (_eachOpponentLosesLifeRule.TryMatch(text, out var oppLose) && oppLose is not null)
      return oppLose;

    // No match: return null so the caller (TryMatch) rejects the whole text
    // rather than emitting a partially-parsed effect. The dispatcher will
    // then leave the full triggered ability as unparsed.
    return null;
  }
}
