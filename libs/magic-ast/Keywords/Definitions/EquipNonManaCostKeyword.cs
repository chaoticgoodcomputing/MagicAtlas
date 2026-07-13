namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Activated;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Equip—[non-mana cost]: the em-dash cost variant of equip, e.g. "Equip—Discard a
/// card." (Murderer's Axe).
///
/// CR 702.6a (verbatim): "Equip is an activated ability of Equipment cards. 'Equip
/// [cost]' means '[Cost]: Attach this permanent to target creature you control.
/// Activate only as a sorcery.'" The template covers any cost, not just mana; oracle
/// text renders non-mana equip costs with an em-dash separator instead of a colon
/// (the same em-dash-cost style used elsewhere for non-mana keyword costs, e.g.
/// <see cref="EscapeKeyword"/>'s "Escape—[cost], …").
///
/// <para>
/// Currently recognizes discard costs (<see cref="DiscardCost"/>), reusing the same
/// <c>ActivatedRuleHelpers.ParseDiscardPattern</c> text parser that backs
/// <c>DiscardCostRule</c> for colon-form activated-ability costs, so "Discard a card",
/// "Discard two cards", "Discard a legendary card", etc. all resolve consistently.
/// </para>
///
/// <para>
/// Kept as a separate file from <see cref="EquipKeyword"/> (mana-cost form): the two
/// forms have disjoint grammars (bare mana symbols vs. a discard clause terminated by
/// a period) and a shared discriminator (<see cref="KeywordAbility.Equip"/>) is
/// reused rather than the grammars being folded into one combinator. Combinator-only:
/// no <c>KeywordDefinition</c> entry in the legacy registry.
/// </para>
/// </summary>
[Keyword]
public sealed class EquipNonManaCostKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Equip")
    from emDash in Token.EqualTo(OracleToken.EmDash)
    from discardWord in Keyword("Discard")
    from tail in DiscardClauseTail
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Equip,
      Costs = [ParseDiscardCost(tail)],
      Effects =
      [
        new AttachEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Consumes the discard clause's descriptive tail — every token after "Discard" up
  /// to (and including) the sentence-terminating period — without structuring it here;
  /// <see cref="ParseDiscardCost"/> re-tokenizes the joined text through the shared
  /// discard-pattern parser. Mirrors <see cref="EscapeKeyword"/>'s clause-tail shape.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Token<OracleToken>[]> DiscardClauseTail = Token
    .Matching<OracleToken>(k => k != OracleToken.Period, "discard clause text")
    .Many()
    .Then(tokens => Token.EqualTo(OracleToken.Period).Select(_ => tokens));

  /// <summary>
  /// Rejoins "Discard" with its clause tail (e.g. "a card") and hands the resulting
  /// text ("discard a card") to the shared <c>ActivatedRuleHelpers.ParseDiscardPattern</c>
  /// so quantity/filter parsing (numerals, "legendary card", etc.) stays in one place.
  /// </summary>
  private static DiscardCost ParseDiscardCost(Token<OracleToken>[] tail)
  {
    var clauseText = "discard " + string.Join(" ", tail.Select(t => t.ToStringValue()));
    var (quantity, filter) = ActivatedRuleHelpers.ParseDiscardPattern(clauseText);
    return new DiscardCost { Filter = filter, Quantity = quantity };
  }
}
