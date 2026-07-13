namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice a land and discard your hand" — Psychic Vortex's end-step tax: the
/// controller sacrifices a land they choose AND discards their entire hand, two
/// mandatory conjunctive effects in a single clause. CR 701.21a (sacrifice);
/// CR 701.9a (discard). The two effects are wrapped in a
/// <see cref="CompositeEffect"/>, mirroring the single-sentence-conjunction
/// convention used elsewhere for "and"-joined mandatory effect pairs
/// (<see cref="DiscardAndUntapAllLandsRule"/>).
///
/// <para>
/// "A land" has no formal target declaration (Rule 115.1 — only the "target"
/// keyword creates a target), so the reference kind is
/// <see cref="ObjectReferenceKind.Any"/>, mirroring
/// <see cref="SacrificeAnyCreatureTriggeredRule"/>'s "sacrifice a creature" shape
/// but filtered to <c>CardTypes:["land"]</c>. "Your hand" is modelled as a
/// <see cref="DerivedQuantity"/> with <see cref="DerivedKind.CardsInHand"/>,
/// matching <see cref="Spell.Rules.DiscardHandSpellRule"/>'s "discard your hand"
/// decomposition — descriptively, "hand" means "all cards in hand at the time the
/// effect resolves."
/// </para>
///
/// Priority 62 — matches the sibling <see cref="DiscardAndUntapAllLandsRule"/>:
/// above the generic single-effect rules (which would fail on the conjoined
/// clause). ANCHORED (^…$) so it cannot match as a substring of a broader clause.
/// </summary>
[TriggeredRule(Priority = 62)]
public sealed class SacrificeLandAndDiscardHandRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+a\s+land\s+and\s+discard\s+your\s+hand\.?$",
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
        new SacrificeEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Any,
            Filter = new ObjectFilter { CardTypes = ["land"] },
          },
        },
        new DiscardCardsEffect
        {
          Count = new DerivedQuantity { DerivedFrom = DerivedKind.CardsInHand },
          Player = ObjectReference.You(),
          Random = false,
        },
      ],
    };
    return true;
  }
}
