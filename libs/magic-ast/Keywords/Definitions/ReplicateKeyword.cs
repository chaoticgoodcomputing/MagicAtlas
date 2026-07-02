namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Replicate [cost]: two abilities per CR 702.56a.
///
/// <para>
/// CR 702.56a (verbatim): "Replicate is a keyword that represents two abilities... 'Replicate
/// [cost]' means 'As an additional cost to cast this spell, you may pay [cost] any number of
/// times' and 'When you cast this spell, if a replicate cost was paid for it, copy it for each
/// time its replicate cost was paid. If the spell has any targets, you may choose new targets
/// for any of the copies.'"
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.ReplicateStaticRule"/> (priority 1001), which
/// returns both abilities as a list. This keyword file keeps the combinator live as a
/// fallback but no longer uses the deleted <c>ReplicateEffect</c> opaque marker: it emits only
/// the PRIMARY repeatable additional-cost static ability ("As an additional cost to cast this
/// spell, you may pay [cost] any number of times"). The <see cref="Definition"/> is null
/// because <see cref="IKeywordExpander.Expand"/> can only return a single
/// <see cref="Ability"/> and Replicate decomposes into two.
/// </para>
///
/// <para>Combinator-only keyword — no <c>KeywordDefinition</c> entry exists in the
/// legacy <c>KeywordDefinitions.All</c> list for this keyword.</para>
/// </summary>
[Keyword]
public sealed class ReplicateKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Replicate decomposes into
  /// two abilities (CR 702.56a). The oracle-text parser handles the full two-ability output
  /// via ReplicateStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Replicate")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Replicate,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = cost,
            IsOptional = true,
            Repeatable = true,
          },
        },
      ],
    }
  );
}
