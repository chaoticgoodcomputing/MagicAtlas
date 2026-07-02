namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Forestwalk: This creature can't be blocked as long as defending player controls a Forest.
/// Rule 702.14b. Landwalk variant. Combinator-only keyword — no <see cref="KeywordDefinition"/>
/// exists in the legacy <c>KeywordDefinitions</c> registry.
/// </summary>
[Keyword]
public sealed class ForestwalkKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Forestwalk")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Forestwalk,
      Effects = [new EvasionEffect
      {
        UnblockableCondition = new EvasionCondition
        {
          ConditionType = EvasionConditionType.DefendingPlayerControls,
          PermanentFilter = new ObjectFilter { Subtypes = ["Forest"] },
        },
      }],
      Reminder = reminder,
    }
  );
}
