namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Evoke [cost]: two abilities per CR 702.74a.
///
/// <para>
/// CR 702.74a (verbatim): "Evoke represents two abilities: a static ability that
/// functions in any zone from which the card with evoke can be cast and a triggered
/// ability that functions on the battlefield. 'Evoke [cost]' means 'You may cast
/// this card by paying [cost] rather than paying its mana cost' and 'When this
/// permanent enters, if its evoke cost was paid, its controller sacrifices it.'
/// Casting a spell for its evoke cost follows the rules for paying alternative
/// costs in rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.EvokeStaticRule"/> (priority 1001),
/// which returns both abilities as a list. This keyword file keeps the combinator
/// live as a fallback, emitting the PRIMARY alternative-cast
/// <see cref="StaticAbility"/> (no longer the deleted opaque <c>EvokeEffect</c>
/// marker). The <see cref="Definition"/> is null because
/// <see cref="IKeywordExpander.Expand"/> can only return a single
/// <see cref="Ability"/> and Evoke decomposes into two.
/// </para>
/// </summary>
[Keyword]
public sealed class EvokeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Evoke decomposes into
  /// two abilities (CR 702.74a) — a static alternative-cast ability and an
  /// ETB-sacrifice triggered ability. The oracle-text parser handles the two-ability
  /// output via EvokeStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Evoke")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Evoke,
      Effects =
      [
        new AlternativeCastEffect
        {
          FromZone = Zone.Hand,
          Cost = cost,
        },
      ],
      Reminder = reminder,
    }
  );
}
