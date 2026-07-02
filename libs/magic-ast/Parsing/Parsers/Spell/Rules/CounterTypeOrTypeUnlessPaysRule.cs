namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Counter target [Q1] or [Q2] spell unless its controller pays {COST}." — Disrupt
/// ("instant or sorcery"). Hybridizes the two-type disjunction filter of
/// <see cref="CounterTargetTypeOrSubtypeSpellRule"/> with the unless-pays wrapper of
/// <see cref="CounterUnlessPaysRule"/>: neither sibling alone owns a disjunctive
/// spell-type filter combined with a pay-cost tax.
///
/// CR 701.6a: "To counter a spell or ability means to cancel it, removing it from the
/// stack. It doesn't resolve and none of its effects occur. A countered spell is put
/// into its owner's graveyard." CR 701.6b: "The player who cast a countered spell or
/// activated a countered ability doesn't get a "refund" of any costs that were paid."
/// Glossary "Counter" (1): "To cancel a spell or ability so it doesn't resolve and none
/// of its effects occur. See rule 701.6, "Counter."" CR 118.1: "A cost is an action or
/// payment necessary to take another action or to stop another action from taking
/// place. To pay a cost, a player carries out the instructions specified by the spell,
/// ability, or effect that contains that cost." — the unless-pay clause is this kind of
/// cost, letting the spell's controller stop the counter from happening.
///
/// Priority 85: above <see cref="CounterTargetTypeOrSubtypeSpellRule"/> (80, anchored
/// with a trailing "spell$" so it cannot match the unless tail), above
/// <see cref="CounterUnlessPaysRule"/> (60, single cardtype word so it cannot match
/// "instant or sorcery"), and above <see cref="CounterSpellRule"/> (50).
/// </summary>
[SpellRule(Priority = 85)]
public sealed class CounterTypeOrTypeUnlessPaysRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Counter\s+target\s+(?<t1>[A-Za-z]+)\s+or\s+(?<t2>[A-Za-z]+)\s+spell\s+unless\s+its\s+controller\s+pays\s+(?<unless>(?:\{[^}]+\})+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly HashSet<string> ColorWords = new(System.StringComparer.OrdinalIgnoreCase)
  {
    "white", "blue", "black", "red", "green", "colorless", "multicolored",
  };

  private static readonly HashSet<string> CardTypeVocab = new(System.StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "land", "planeswalker",
    "instant", "sorcery", "tribal", "battle",
  };

  private readonly ManaCostParser _manaCostParser = new();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var t1 = m.Groups["t1"].Value;
    var t2 = m.Groups["t2"].Value;
    if (ColorWords.Contains(t1) || ColorWords.Contains(t2))
    {
      return false;
    }

    var cardTypes = new List<string> { "spell" };
    var subtypes = new List<string>();
    foreach (var q in new[] { t1, t2 })
    {
      if (CardTypeVocab.Contains(q))
      {
        cardTypes.Add(q.ToLowerInvariant());
      }
      else
      {
        subtypes.Add(q);
      }
    }

    var filter = new ObjectFilter
    {
      CardTypes = cardTypes,
      Subtypes = subtypes.Count > 0 ? subtypes : null,
    };

    var parsed = _manaCostParser.Parse(m.Groups["unless"].Value);
    var unless = new UnlessClause
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      Cost = new ManaCost { Symbols = [.. parsed.Symbols] },
    };

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(new CounterSpellEffect {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter }}, unless);
    return true;
  }
}
