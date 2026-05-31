namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Flashback {cost} (CR 702.34): "Flashback [cost]" means "You may cast this card
/// from your graveyard by paying [cost] rather than paying its mana cost." It is a
/// static ability, so the combinator emits a <see cref="StaticAbility"/> carrying the
/// shared <see cref="AlternativeCastEffect"/> primitive (<c>FromZone = Graveyard</c>,
/// <c>Cost = </c> the flashback mana cost). The post-cast "then exile it" is engine
/// territory (ADR 0003/0004 describe-not-execute) and is not modeled.
/// Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions</c> registry.
/// </summary>
[Keyword]
public sealed class FlashbackKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Flashback")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Flashback",
      Effects = [new AlternativeCastEffect
      {
        FromZone = Zone.Graveyard,
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
