namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player discards a card and you untap all lands you control" — the combined
/// triggered effect of a <c>DealsCombatDamageToPlayer</c> trigger (Rule 510 — Combat
/// Damage Step) where the recipient discards a card AND the controller untaps all their lands.
///
/// <para>
/// Sword of Feast and Famine is the canonical card for this pattern. The two effects
/// are conjunctive in a single clause, wrapped in a <see cref="CompositeEffect"/>
/// mirroring the single-sentence-conjunction convention used elsewhere in triggered
/// resolution text (Rule 701.9 — Discard; Rule 701.26 — Untap).
/// </para>
///
/// <para>
/// "That player" refers back to the player named in the trigger condition (Rule 603.2 —
/// "that player" in a triggered ability resolution resolves to the player identified by
/// the trigger event). "All lands you control" references the controller's entire land
/// permanents (CR 301 — Artifacts; CR 701.26a — untapping a permanent).
/// </para>
///
/// Priority 62 — sits above the generic single-effect rules (which would fail on the
/// conjoined clause) but below more specific multi-effect rules.
/// </summary>
[TriggeredRule(Priority = 62)]
public sealed class DiscardAndUntapAllLandsRule : ITriggeredRule
{
  // "that player discards a card and you untap all lands you control"
  // The discard count is always "a card" (one card) in the canonical Sword form.
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+discards?\s+a\s+card\s+and\s+you\s+untap\s+all\s+lands\s+you\s+control\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();

    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects =
      [
        new DiscardCardsEffect
        {
          Count = LiteralQuantity.Of(1),
          Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
          Random = false,
        },
        new UntapEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["land"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
    };
    return true;
  }
}
