namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Unleash: two static abilities per CR 702.98a.
///
/// <para>
/// CR 702.98a (verbatim): "Unleash is a keyword that represents two static
/// abilities. 'Unleash' means 'You may have this permanent enter with an
/// additional +1/+1 counter on it' and 'This permanent can't block as long as it
/// has a +1/+1 counter on it.'"
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.UnleashStaticRule"/> (priority 1001),
/// which returns both abilities as a list. This keyword file keeps the combinator
/// live as a fallback but no longer emits the opaque
/// <c>KeywordAbilityEffect{Unleash}</c> marker: it emits only the PRIMARY
/// enters-with-counter static replacement ability ("You may have this permanent
/// enter with an additional +1/+1 counter on it"). The <see cref="Definition"/> is
/// null because <see cref="IKeywordExpander.Expand"/> can only return a single
/// <see cref="Ability"/> and Unleash decomposes into two.
/// </para>
/// </summary>
[Keyword]
public sealed class UnleashKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Unleash decomposes
  /// into two static abilities (CR 702.98a). The oracle-text parser handles the
  /// full two-ability output via UnleashStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Unleash")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Unleash,
      When = StaticTimingKind.AsThisEnters,
      Reminder = reminder,
      Effects =
      [
        EffectWrap.Optional(
          new PutCountersEffect
          {
            Target = ObjectReference.Self(),
            CounterType = "+1/+1",
            Count = LiteralQuantity.Of(1),
          },
          isOptional: true
        ),
      ],
    }
  );
}
