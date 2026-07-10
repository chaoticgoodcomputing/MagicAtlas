namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "target opponent may have you draw a card. If they don't, you may put a creature card
/// with equal or lesser toughness from your hand onto the battlefield." — Bane, Lord of
/// Darkness's dies-trigger consequent, where a targeted opponent (not the controller)
/// decides whether the controller draws a card, with a fallback battlefield-reanimation
/// option scaled to the toughness of the creature that just died.
///
/// <para>
/// Decomposes as a nested <see cref="OptionalEffect"/>:
/// <list type="bullet">
///   <item><see cref="OptionalEffect.Chooser"/> — "target opponent" (CR 115.1: "target"
///     creates a targeting requirement) → <see cref="ObjectReferenceKind.Target"/> filtered
///     to <c>opponent</c>, mirroring <see cref="TargetOpponentDiscardsTriggeredRule"/>'s
///     "target opponent" convention. This is load-bearing per <see cref="OptionalEffect"/>'s
///     doc comment: the decider (the opponent) is distinct from the recipient of
///     <see cref="OptionalEffect.Inner"/> (the controller, "you draw").</item>
///   <item><see cref="OptionalEffect.Inner"/> — <see cref="DrawCardsEffect"/> for the
///     controller (CR 121.1 — to draw a card is to move the top card of the library to
///     hand); Player = You (the "have you draw" recipient, distinct from the Chooser).</item>
///   <item><see cref="OptionalEffect.IfYouDoNot"/> — a second, controller-optional
///     ("you may") <see cref="OptionalEffect"/> wrapping a
///     <see cref="PutFromHandOntoBattlefieldEffect"/>: "a creature card with equal or
///     lesser toughness" restricts the filter with a <see cref="Comparison"/> relative to
///     <see cref="ObjectReferenceKind.ThatCreature"/> (the creature named by the enclosing
///     dies-trigger's filter, CR 109.5/CR 603.2) — mirroring the relative-comparison
///     convention used by <c>PowerComparison</c>/<c>ToughnessComparison</c> elsewhere
///     (e.g. Mentor, CR 702.134).</item>
/// </list>
/// </para>
///
/// <para>
/// Priority 95 — must run BEFORE <see cref="DrawCardsTriggeredRule"/> (default priority
/// 50), which matches ANY text containing "draw a card" and would otherwise claim this
/// two-sentence text and produce a bare, non-optional <c>drawCards</c> effect, silently
/// dropping both the "target opponent" chooser and the entire "If they don't, …" fallback
/// branch — the exact lossy-fallback failure mode <see cref="SacrificeAnotherCreatureDrawCardRule"/>
/// guards against for its own sibling surface. Fully anchored (<c>^…$</c>) so it cannot
/// fire on any other "target opponent may …" or "draw a card" clause.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class TargetOpponentMayDrawElsePutCreatureFromHandRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^target\s+opponent\s+may\s+have\s+you\s+draw\s+a\s+card\.\s*"
      + @"If\s+they\s+don'?t,\s+you\s+may\s+put\s+a\s+creature\s+card\s+with\s+equal\s+or\s+lesser\s+toughness\s+"
      + @"from\s+your\s+hand\s+onto\s+the\s+battlefield\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Chooser = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["opponent"] },
      },
      Inner = new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(1),
        Player = ObjectReference.You(),
      },
      IfYouDoNot = new OptionalEffect
      {
        Inner = new PutFromHandOntoBattlefieldEffect
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Zone = Zone.Hand,
            Controller = ControllerFilter.You,
            ToughnessComparison = new Comparison
            {
              Operator = ComparisonOperator.LessThanOrEqual,
              RelativeTo = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
              RelativeCharacteristic = RelativeCharacteristic.Toughness,
            },
          },
        },
      },
    };
    return true;
  }
}
