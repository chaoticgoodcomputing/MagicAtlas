namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Counter target spell unless its controller pays {COST}. If that spell is
/// countered this way, exile it instead of putting it into its owner's graveyard."
/// — No More Lies.
///
/// <para>
/// A single counter whose "unless its controller pays {cost}" clause (the target's
/// controller may pay to prevent the counter) is combined with a follow-up sentence
/// that redirects the countered spell's zone-change: instead of the default graveyard
/// (CR 701.6a — "A countered spell is put into its owner's graveyard.") the spell is
/// exiled. The redirect is a property on the counter effect
/// (<see cref="CounterSpellEffect.ExileInsteadOfGraveyard"/>), and the unless clause
/// wraps it via <see cref="MagicAST.AST.Effects.Core.EffectWrap.Preventable"/>, so this
/// whole two-sentence surface collapses to one <c>preventable(counterSpell)</c>.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>) on the FULL two-sentence text, mirroring
/// <see cref="CounterSpellPutOnTopInsteadOfGraveyardRule"/>: the base
/// <see cref="CounterUnlessPaysRule"/> / <see cref="CounterSpellRule"/> are anchored to
/// end right after the cost, so they never claim this longer surface, and the
/// sentence-bundle splitter cannot parse the dependent follow-up ("If that spell is
/// countered this way, …") as a standalone effect, so dispatch falls through to the
/// whole-text rule chain where this fires.
/// </para>
///
/// Priority 82: fires (like the put-on-top sibling) ahead of the shorter counter rules.
/// </summary>
[SpellRule(Priority = 82)]
public sealed class CounterUnlessPaysExileInsteadOfGraveyardRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Counter\s+target\s+spell\s+unless\s+its\s+controller\s+pays\s+"
    + @"(?<unless>(?:\{[^}]+\})+)\.\s+"
    + @"If\s+that\s+spell\s+is\s+countered\s+this\s+way,\s+"
    + @"exile\s+it\s+instead\s+of\s+putting\s+it\s+into\s+its\s+owner's\s+graveyard\.?$",
    RegexOptions.IgnoreCase
  );

  private readonly ManaCostParser _manaCostParser = new();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var parsed = _manaCostParser.Parse(m.Groups["unless"].Value);
    var unless = new UnlessClause
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      Cost = new ManaCost { Symbols = [.. parsed.Symbols] },
    };

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(
      new CounterSpellEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["spell"] },
        },
        ExileInsteadOfGraveyard = true,
      },
      unless
    );
    return true;
  }
}
