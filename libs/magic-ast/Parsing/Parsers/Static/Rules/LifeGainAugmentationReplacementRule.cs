namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Life-gain augmentation replacement effect (Pest Rescuer):
/// "If you would gain life, you gain that much life plus 1 instead."
///
/// <para>
/// CR 614.1 (verbatim): "Some continuous effects are replacement effects. Like
/// prevention effects (see rule 615), replacement effects apply continuously as events
/// happen—they aren't locked in ahead of time. Such effects watch for a particular event
/// that would happen and completely or partially replace it…" This line is therefore a
/// continuous <b>static</b> replacement ability (<c>Kind: static</c>), NOT a triggered
/// "whenever you gain life" ability: it watches the life-gain event and increases the
/// amount by a fixed quantity before the gain resolves.
/// </para>
///
/// <para>
/// CR 119.3 (verbatim): "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." The replaced event is a
/// <see cref="LifeChangeEvent"/> with <c>ChangeType = "gain"</c> for you
/// (<c>Controller = You</c>, from "If <b>you</b> would gain life"). The "that much life"
/// clause preserves the original amount, so the original gain still occurs
/// (<see cref="ReplacementEffect.OriginalEventOccurs"/> = true — augmentation, mirroring
/// <see cref="SpellCopyAugmentationReplacementRule"/>'s "that many times plus an
/// additional time"), and "plus 1" is carried as a typed
/// <see cref="ReplacementModifier"/> (<c>Type = "plus"</c>, <c>Amount</c> a
/// <see cref="LiteralQuantity"/>) rather than baked into the discriminator, so a
/// "plus 2" variant reuses the same shape.
/// </para>
///
/// <para>Anchored (^…$); the captured digit is the additive amount. Priority 971 —
/// alongside the other replacement rules; the exact phrase cannot be confused with any
/// other static shape.</para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class LifeGainAugmentationReplacementRule : IStaticRule
{
  // "If you would gain life, you gain that much life plus N instead."
  private static readonly Regex _pattern = new(
    @"^\s*If\s+you\s+would\s+gain\s+life,\s+you\s+gain\s+that\s+much\s+life\s+plus\s+(?<amount>\d+)\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value, System.Globalization.CultureInfo.InvariantCulture);

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ReplacementEffect
          {
            // The event being replaced: you would gain life (CR 119.3).
            Event = new LifeChangeEvent
            {
              ChangeType = "gain",
              Controller = ObjectReference.You(),
            },
            // "that much life" preserves the original gain; "plus N" augments it.
            OriginalEventOccurs = true,
            Modifier = new ReplacementModifier
            {
              Type = "plus",
              Amount = LiteralQuantity.Of(amount),
            },
          },
        ],
      },
    ];
  }
}
