namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Swampwalk: This creature can't be blocked as long as the defending player
/// controls a Swamp. Rule 702.14 (landwalk). MAST records keyword presence;
/// the blocking restriction is engine territory.
///
/// <para>
/// Combinator-only keyword (no KeywordDefinition in the legacy registry).
/// The combinator is the inline expansion of the private
/// <c>OracleParsers.Landwalk("Swampwalk", "Swamp")</c> call. Tier: Simple.
/// </para>
/// </summary>
[Keyword]
public sealed class SwampwalkKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Swampwalk")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Swampwalk,
      Effects = [new EvasionEffect
      {
        UnblockableCondition = new EvasionCondition
        {
          ConditionType = EvasionConditionType.DefendingPlayerControls,
          PermanentFilter = new ObjectFilter { Subtypes = ["Swamp"] },
        },
      }],
      Reminder = reminder,
    }
  );
}
