namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Islandwalk: This creature can't be blocked as long as defending player controls
/// an Island. Rule 702.14 (landwalk variant). MAST records keyword presence as an
/// EvasionEffect with a DefendingPlayerControls condition on Island permanents.
/// No KeywordDefinition — Islandwalk has no entry in KeywordDefinitions.cs
/// (landwalk variants were not added to the definitions registry).
/// </summary>
[Keyword]
public sealed class IslandwalkKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Islandwalk")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Islandwalk,
      Effects = [new EvasionEffect
      {
        UnblockableCondition = new EvasionCondition
        {
          ConditionType = EvasionConditionType.DefendingPlayerControls,
          PermanentFilter = new ObjectFilter { Subtypes = ["Island"] },
        },
      }],
      Reminder = reminder,
    }
  );
}
