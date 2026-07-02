namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Reinforce N—[cost]. Rule 702.77.
///
/// <para>
/// CR 702.77a (verbatim): "Reinforce is an activated ability that functions only
/// while the card with reinforce is in a player's hand. 'Reinforce N-[cost]' means
/// '[Cost], Discard this card: Put N +1/+1 counters on target creature.'"
/// </para>
///
/// <para>
/// CR 702.77b (verbatim): "Although the reinforce ability can be activated only if
/// the card is in a player's hand, it continues to exist while the object is on
/// the battlefield and in all other zones. Therefore objects with reinforce will be
/// affected by effects that depend on objects having one or more activated
/// abilities." The hand-only functional restriction is engine territory — MAST does
/// not add a zone-restriction field (mirrors <see cref="CyclingKeyword"/>).
/// </para>
///
/// <para>
/// Composition: the "N—[cost]" number/em-dash glue is <see cref="AwakenKeyword"/>'s
/// shape; the discard-this-card cost body is <see cref="CyclingKeyword"/>'s; the
/// +1/+1-counters-on-target-creature effect is <see cref="ScavengeKeyword"/>'s
/// <see cref="PutCountersEffect"/> shape, but with a literal count (N) rather than a
/// derived one. Combinator-only: no matching <c>KeywordDefinitions</c> entry exists
/// in the legacy registry.
/// </para>
/// </summary>
[Keyword]
public sealed class ReinforceKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Reinforce")
    from n in ReinforceNumber
    from dash in EmDash
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Reinforce,
      Costs =
      [
        cost,
        new DiscardCost
        {
          Filter = new ObjectFilter { CardTypes = ["card"] },
          Quantity = LiteralQuantity.Of(1),
        },
      ],
      Effects =
      [
        new PutCountersEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
            },
          },
          CounterType = "+1/+1",
          Count = n,
        },
      ],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parses the "N" in "Reinforce N—[cost]" into a <see cref="LiteralQuantity"/>.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Quantity> ReinforceNumber = Token
    .EqualTo(OracleToken.Number)
    .Select(t => (Quantity)LiteralQuantity.Of(int.Parse(t.ToStringValue())));

  /// <summary>
  /// Parses the em-dash glue between the reinforce number and its cost.
  /// </summary>
  private static readonly TokenListParser<OracleToken, Token<OracleToken>> EmDash =
    Token.EqualTo(OracleToken.EmDash);
}
