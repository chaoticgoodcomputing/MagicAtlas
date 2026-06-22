namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice this [permanent-noun] unless you pay {COST}" — the upkeep-tax
/// pattern. The "unless you pay" gate is a cost-or-consequence: the sacrifice
/// (Rule 701.21 — Sacrifice) happens unless its controller chooses to pay the
/// stated cost, and paying a cost is never automatic (Rule 118.5 — "it's not
/// automatically paid"). This exact templating is the canonical Echo wording
/// (Rule 702.30 — Echo: "At the beginning of your upkeep, ... sacrifice it
/// unless you pay [cost]"), here printed directly on the card rather than
/// abbreviated to the keyword.
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "At the beginning of your upkeep"
///   effect  = "sacrifice this creature unless you pay {B}"
/// </para>
///
/// <para>
/// The self-noun varies by the card's type — "this creature" (Whipstitched
/// Zombie), "this enchantment" (Breeding Pit, Glaciers), "this artifact"
/// (Forethought Amulet), "this Aura" (Melancholy, Thirst), and the generic
/// "this permanent" all name the same object: the source bearing the ability.
/// The noun is descriptive type-flavour and carries no AST distinction, so the
/// pattern accepts the full set and the produced shape is identical regardless
/// of which noun was printed.
/// </para>
///
/// <para>
/// The pronoun form "this [noun]" is a self-reference (Rule 109.2): the
/// permanent bearing the ability is the object that would be sacrificed.
/// MAST models this as <see cref="ObjectReferenceKind.Self"/>, distinct from
/// the "sacrifice it" ETB-land pattern handled by
/// <see cref="SacrificeUnlessPayTriggeredRule"/> which uses
/// <see cref="ObjectReferenceKind.It"/>.
/// </para>
///
/// <para>
/// Produces a <see cref="SacrificeEffect"/> with Target = Self and an
/// <see cref="UnlessClause"/> whose Player is You and whose Cost is the
/// parsed mana expression.
/// </para>
///
/// <para>
/// Representative cards: Whipstitched Zombie (NEM), Wild Leotau (CON),
/// Spindrift Drake (WTH), Molting Harpy (UDS) — creatures; Breeding Pit (5ED),
/// Glaciers (ICE), Drought (MIR), Justice (LEG), Thelon's Chant (TSP) —
/// enchantments; Forethought Amulet (MIR) — artifact; Melancholy, Thirst (WTH)
/// — Auras.
/// Rule citations: 701.21 (Sacrifice), 118.5 (paying a cost is not automatic),
/// 702.30 (Echo — the canonical "sacrifice it unless you pay" templating).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeSelfUnlessPayRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+this\s+(?:creature|permanent|enchantment|artifact|aura)\s+unless\s+you\s+pay\s+(?<cost>(?:\{[^}]+\})+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(m.Groups["cost"].Value);
    if (manaCost is null)
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(new SacrificeEffect {
      Target = ObjectReference.Self()}, new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = manaCost,
      });
    return true;
  }
}
