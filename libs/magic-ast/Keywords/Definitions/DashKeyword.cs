namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Dash [cost]: three abilities per CR 702.109a.
///
/// <para>
/// CR 702.109a (verbatim): "Dash represents three abilities: two static abilities
/// that function while the card with dash is on the stack, one of which may create
/// a delayed triggered ability, and a static ability that functions while the object
/// with dash is on the battlefield. 'Dash [cost]' means 'You may cast this card by
/// paying [cost] rather than its mana cost,' 'If this spell's dash cost was paid,
/// return the permanent this spell becomes to its owner's hand at the beginning of
/// the next end step,' and 'As long as this permanent's dash cost was paid, it has
/// haste.' Casting a spell for its dash cost follows the rules for paying alternative
/// costs in rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.DashStaticRule"/> (priority 1001),
/// which returns all three abilities as a list. This keyword file keeps the
/// combinator live as a fallback but no longer uses the deleted <c>DashEffect</c>
/// opaque marker: it emits only the PRIMARY alternative-cast static ability ("You may
/// cast this card by paying [cost] rather than its mana cost," from hand). The
/// <see cref="Definition"/> is null because <see cref="IKeywordExpander.Expand"/> can
/// only return a single <see cref="Ability"/> and Dash decomposes into three.
/// </para>
///
/// <para>Combinator-only keyword — no <c>KeywordDefinition</c> entry exists in the
/// legacy <c>KeywordDefinitions.All</c> list for this keyword.</para>
/// </summary>
[Keyword]
public sealed class DashKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Dash decomposes into
  /// three abilities (CR 702.109a). The oracle-text parser handles the full
  /// three-ability output via DashStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Dash")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Dash,
      Reminder = reminder,
      Effects =
      [
        new AlternativeCastEffect
        {
          FromZone = Zone.Hand,
          Cost = cost,
        },
      ],
    }
  );
}
