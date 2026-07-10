namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Impending N—[cost] (CR 702.176). An alternative-cast keyword that also seeds a
/// battlefield time-counter mechanic — it represents four abilities.
///
/// <para>
/// CR 702.176a (verbatim): "Impending is a keyword that represents four abilities. The
/// first is a static ability that functions while the spell with impending is on the
/// stack. The second is static ability that creates a replacement effect that may apply
/// to the permanent with impending as it enters the battlefield from the stack. The third
/// is a static ability that functions on the battlefield. The fourth is a triggered
/// ability that functions on the battlefield. \"Impending N-[cost]\" means \"You may
/// choose to pay [cost] rather than pay this spell's mana cost,\" \"If you chose to pay
/// this permanent's impending cost, it enters with N time counters on it,\" \"As long as
/// this permanent's impending cost was paid and it has a time counter on it, it's not a
/// creature,\" and \"At the beginning of your end step, if this permanent's impending cost
/// was paid and it has a time counter on it, remove a time counter from it.\" Casting a
/// spell for its impending cost follows the rules for paying alternative costs in rules
/// 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.ImpendingStaticRule"/> (priority 1001),
/// which returns all four abilities as a list (mirroring the
/// <see cref="BuybackKeyword"/> / <c>BuybackStaticRule</c> precedent). This keyword file
/// keeps the combinator live as a fallback that emits only the PRIMARY alternative-cast
/// static ability (the first CR clause) — the <see cref="IKeywordExpander.Expand"/>
/// contract returns a single <see cref="Ability"/>, but Impending decomposes into four, so
/// <see cref="Definition"/> is null.
/// </para>
/// </summary>
[Keyword]
public sealed class ImpendingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Impending decomposes into
  /// four abilities (CR 702.176a). The oracle-text parser handles the full four-ability
  /// output via ImpendingStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <summary>Parses the "N" numeric literal (the time-counter count).</summary>
  private static readonly TokenListParser<OracleToken, Quantity> NumberLiteral = Token
    .EqualTo(OracleToken.Number)
    .Select(t => (Quantity)LiteralQuantity.Of(int.Parse(t.ToStringValue())));

  /// <summary>Parses the em-dash separating "Impending N" from the cost.</summary>
  private static readonly TokenListParser<OracleToken, Superpower.Model.Token<OracleToken>> Dash =
    Token.EqualTo(OracleToken.EmDash);

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Impending")
    from n in NumberLiteral
    from dash in Dash
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    // First CR clause only: "You may choose to pay [cost] rather than pay this spell's
    // mana cost." Cast from hand at an alternative cost — modelled with the shared
    // AlternativeCastEffect. The N (time counters) and the counter/type-change/end-step
    // abilities are produced by ImpendingStaticRule, which preempts this fallback.
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Impending,
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
