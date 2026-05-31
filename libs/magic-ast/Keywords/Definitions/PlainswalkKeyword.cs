namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Plainswalk: This creature can't be blocked as long as the defending player controls
/// a Plains. Rule 702.12. A landwalk variant — MAST records the keyword presence and
/// the land-type condition; the unblockable semantics are engine territory.
/// </summary>
[Keyword]
public sealed class PlainswalkKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Plainswalk")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Plainswalk",
      Effects = [new EvasionEffect
      {
        UnblockableCondition = new EvasionCondition
        {
          ConditionType = EvasionConditionType.DefendingPlayerControls,
          PermanentFilter = new ObjectFilter { Subtypes = ["Plains"] },
        },
      }],
      Reminder = reminder,
    }
  );
}
