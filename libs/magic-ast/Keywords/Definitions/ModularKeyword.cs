namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Modular N: two abilities per CR 702.43a.
///
/// <para>
/// CR 702.43a (verbatim): "Modular represents both a static ability and a triggered
/// ability. 'Modular N' means 'This permanent enters with N +1/+1 counters on it'
/// and 'When this permanent is put into a graveyard from the battlefield, you may put
/// a +1/+1 counter on target artifact creature for each +1/+1 counter on this
/// permanent.'"
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.ModularStaticRule"/> (priority 1001),
/// which returns both abilities as a list. This keyword file keeps the combinator live
/// as a fallback but no longer uses the deleted <c>ModularEffect</c> opaque marker: it
/// emits only the PRIMARY enters-with-counters static ability ("This permanent enters
/// with N +1/+1 counters on it"). The <see cref="Definition"/> is null because
/// <see cref="IKeywordExpander.Expand"/> can only return a single <see cref="Ability"/>
/// and Modular decomposes into two.
/// </para>
/// </summary>
[Keyword]
public sealed class ModularKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Modular decomposes into
  /// two abilities (CR 702.43a). The oracle-text parser handles the full two-ability
  /// output via ModularStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Modular")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Modular,
      When = StaticTimingKind.AsThisEnters,
      Reminder = reminder,
      Effects =
      [
        new PutCountersEffect
        {
          Target = ObjectReference.Self(),
          CounterType = "+1/+1",
          Count = LiteralQuantity.Of(int.Parse(value.ToStringValue())),
        },
      ],
    }
  );
}
