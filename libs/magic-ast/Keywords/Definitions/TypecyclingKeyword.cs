namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// [Type]cycling [cost]: Discard this card: Search your library for a [Type] card,
/// reveal it, put it into your hand, then shuffle.
/// Rule 702.28 (and the 702.29 landcycling variant). Catch-all parser for the
/// type-cycling family. Recognizes three printed shapes:
/// <list type="bullet">
///   <item>single-word land-type variants — Forestcycling, Swampcycling,
///   Plainscycling, etc. (the land type is the prefix before "cycling");</item>
///   <item>two-word "Basic landcycling {cost}" — searches for a basic land card
///   (Type = "Basic land");</item>
///   <item>bare "Landcycling {cost}" — searches for any land card
///   (Type = "Land").</item>
/// </list>
/// MAST records the keyword, the land type, and the cost; the inner
/// search/reveal/shuffle structure is inferred from the rules.
/// Lower priority (40) so specific cycling variants can outrank this catch-all
/// in the registry Or-chain.
/// </summary>
[Keyword(Priority = 40)]
public sealed class TypecyclingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  // "Basic landcycling {cost}" — two-word form; searches for a basic land card.
  // Must be tried before the bare/catch-all branches because its first token
  // ("Basic") does not end in "cycling".
  private static readonly TokenListParser<OracleToken, Ability> _basicLandcycling = (
    from basic in Keyword("Basic")
    from landcycling in Keyword("landcycling")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Basic landcycling",
      Effects = [new TypecyclingEffect
      {
        Type = "Basic land",
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  // Bare "Landcycling {cost}" — searches for any land card (Type = "Land").
  // Handled explicitly so the single-word catch-all does not yield the literal
  // type "Landcycling".
  private static readonly TokenListParser<OracleToken, Ability> _bareLandcycling = (
    from landcycling in Keyword("Landcycling")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Landcycling",
      Effects = [new TypecyclingEffect
      {
        Type = "Land",
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  // Single-word "[Type]cycling {cost}" catch-all — Forestcycling, Swampcycling, etc.
  private static readonly TokenListParser<OracleToken, Ability> _singleWordTypecycling = (
    from kwToken in Token
      .EqualTo(OracleToken.Word)
      .Try()
      .Where(t =>
      {
        var s = t.ToStringValue();
        return s.EndsWith("cycling", StringComparison.OrdinalIgnoreCase)
          && !s.Equals("cycling", StringComparison.OrdinalIgnoreCase);
      })
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    let kwText = kwToken.ToStringValue()
    let landType = kwText[..^"cycling".Length]
    select (Ability)new StaticAbility
    {
      KeywordSource = kwText,
      Effects = [new TypecyclingEffect
      {
        Type = char.ToUpperInvariant(landType[0]) + landType[1..],
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } =
    _basicLandcycling.Try()
      .Or(_bareLandcycling.Try())
      .Or(_singleWordTypecycling.Try());
}
