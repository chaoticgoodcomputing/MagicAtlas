namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Blitz [cost]: three abilities per CR 702.152a (the sacrifice-variant sibling of
/// <see cref="DashKeyword"/>).
///
/// <para>
/// CR 702.152a (verbatim): "Blitz represents three abilities: two static abilities
/// that function while the card with blitz is on the stack, one of which may create
/// a delayed triggered ability, and a static ability that functions while the object
/// with blitz is on the battlefield. 'Blitz [cost]' means 'You may cast this card by
/// paying [cost] rather than its mana cost,' 'If this spell's blitz cost was paid,
/// sacrifice the permanent this spell becomes at the beginning of the next end step,'
/// and 'As long as this permanent's blitz cost was paid, it has haste and \"When this
/// permanent is put into a graveyard from the battlefield, draw a card.\"' Casting a
/// spell for its blitz cost follows the rules for paying alternative costs in rules
/// 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.BlitzStaticRule"/> (priority 1001),
/// which returns all three abilities as a list. This keyword file keeps the
/// combinator live as a fallback but no longer uses the deleted <c>BlitzEffect</c>
/// opaque marker: it emits only the PRIMARY alternative-cast static ability ("You may
/// cast this card by paying [cost] rather than its mana cost," from hand). The
/// <see cref="Definition"/> is null because <see cref="IKeywordExpander.Expand"/> can
/// only return a single <see cref="Ability"/> and Blitz decomposes into three.
/// </para>
///
/// <para>Combinator-only keyword — no <c>KeywordDefinition</c> entry exists in the
/// legacy <c>KeywordDefinitions.All</c> list for this keyword.</para>
/// </summary>
[Keyword]
public sealed class BlitzKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Blitz decomposes into
  /// three abilities (CR 702.152a). The oracle-text parser handles the full
  /// three-ability output via BlitzStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Blitz")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Blitz,
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
