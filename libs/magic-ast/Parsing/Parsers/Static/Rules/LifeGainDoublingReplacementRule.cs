namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// Life-gain doubling replacement effect (The Wind Crystal):
/// "If you would gain life, you gain twice that much life instead."
///
/// <para>
/// CR 614.1 (verbatim): "Some continuous effects are replacement effects. Like
/// prevention effects (see rule 615), replacement effects apply continuously as events
/// happen—they aren't locked in ahead of time. Such effects watch for a particular event
/// that would happen and completely or partially replace it…" This line is therefore a
/// continuous <b>static</b> replacement ability (<c>Kind: static</c>), NOT a triggered
/// "whenever you gain life" ability: it watches the life-gain event and replaces it with
/// one for double the amount.
/// </para>
///
/// <para>
/// The replaced event is a <see cref="LifeChangeEvent"/> with <c>ChangeType = "gain"</c>
/// for you (<c>Controller = You</c>, from "If <b>you</b> would gain life"). Unlike the
/// "plus N" augmentation in <see cref="LifeGainAugmentationReplacementRule"/> (where "that
/// much life plus N" preserves the original gain and adds to it,
/// <c>OriginalEventOccurs = true</c>), "twice that much life instead" fully replaces the
/// original event with a new one of double the quantity, so
/// <see cref="ReplacementEffect.OriginalEventOccurs"/> = false — mirroring
/// <see cref="MillDoublingReplacementRule"/>/<see cref="NoncombatDamageDoublingReplacementRule"/>'s
/// "twice that many"/"double that damage" shape. The doubling is carried as a typed
/// <see cref="ReplacementModifier"/> (<c>Type = "double"</c>), not baked into the
/// discriminator or a free-text description.
/// </para>
///
/// <para>Anchored (^…$); a sibling of <see cref="LifeGainAugmentationReplacementRule"/> —
/// the two phrases ("plus N" vs "twice") never overlap as substrings of each other, so
/// this cannot mis-fire on that rule's cards or vice versa. Priority 971 — alongside the
/// other replacement rules.</para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class LifeGainDoublingReplacementRule : IStaticRule
{
  // "If you would gain life, you gain twice that much life instead."
  private static readonly Regex _pattern = new(
    @"^\s*If\s+you\s+would\s+gain\s+life,\s+you\s+gain\s+twice\s+that\s+much\s+life\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

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
            // "twice that much life instead" fully replaces the original gain with a
            // doubled one — the original amount does not additionally occur.
            OriginalEventOccurs = false,
            Modifier = new ReplacementModifier
            {
              Type = "double",
            },
          },
        ],
      },
    ];
  }
}
