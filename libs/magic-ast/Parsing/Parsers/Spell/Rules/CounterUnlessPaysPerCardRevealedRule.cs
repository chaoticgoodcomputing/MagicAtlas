namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Counter target spell unless its controller pays {MANA} for each card revealed
/// this way." — the counter sentence of the Scent of Brine family, whose payment
/// scales with the count disclosed by the sibling
/// <see cref="RevealAnyNumberBlueCardsFromHandRule"/> ("Reveal any number of blue
/// cards in your hand."). Distinct from the flat-cost
/// <see cref="CounterUnlessPaysRule"/> (Mana Leak, "pays {3}"): there the unless-cost
/// is a fixed <see cref="ManaCost"/>; here it is a
/// <see cref="ScaledManaCost"/> whose total is (per-unit) × (cards revealed this way),
/// so the two shapes need distinct rules. Anchored end-to-end on the "for each card
/// revealed this way" tail, so it never overlaps the flat-cost rule.
///
/// <para>
/// Emits <see cref="EffectWrap.Preventable"/>(<see cref="CounterSpellEffect"/>,
/// <see cref="UnlessClause"/>): the inner is "counter target spell"
/// (<c>Filter=CardTypes["spell"]</c>), the unless-clause names the spell's Controller
/// and a <see cref="ScaledManaCost"/> of per-unit "{1}" times a
/// <see cref="CardsRevealedThisWayQuantity"/>. Mirrors the Mana Leak gold's
/// Preventable/Counter/Unless shape, differing only in the scaled cost.
/// </para>
///
/// <para>
/// CR 701.6a (verbatim): "To counter a spell or ability means to cancel it, removing
/// it from the stack. It doesn't resolve and none of its effects occur. A countered
/// spell is put into its owner's graveyard."
/// CR 118.1 (verbatim): "A cost is an action or payment necessary to take another
/// action or to stop another action from taking place." — the unless-payment stops
/// the counter from happening.
/// </para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class CounterUnlessPaysPerCardRevealedRule : ISpellRule
{
  // Captures <unit> — the mana paid per card revealed ({1}, {2}, {U}, …).
  private static readonly Regex Pattern = new(
    @"^Counter\s+target\s+spell\s+unless\s+its\s+controller\s+pays\s+"
    + @"(?<unit>(?:\{[^}]+\})+)\s+for\s+each\s+card\s+revealed\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private readonly ManaCostParser _manaCostParser = new();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var parsed = _manaCostParser.Parse(m.Groups["unit"].Value);
    var unless = new UnlessClause
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      Cost = new ScaledManaCost
      {
        PerUnit = new ManaCost { Symbols = [.. parsed.Symbols] },
        Count = new CardsRevealedThisWayQuantity(),
      },
    };

    effect = EffectWrap.Preventable(
      new CounterSpellEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["spell"] },
        },
      },
      unless
    );
    return true;
  }
}
