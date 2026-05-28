namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Mountainwalk: This creature can't be blocked as long as the defending player controls a Mountain.
/// Rule 702.15. A landwalk variant — MAST records the keyword's presence; the
/// blocking restriction is engine territory.
///
/// <para>
/// Combinator-only keyword (no <see cref="KeywordDefinition"/>): Mountainwalk has no
/// <c>KeywordDefinitions.Mountainwalk</c> entry in the legacy file; only the parser combinator
/// lived in <c>OracleParsers</c> (via the private <c>Landwalk</c> helper).
/// </para>
/// </summary>
[Keyword]
public sealed class MountainwalkKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Mountainwalk")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Mountainwalk",
      Effects = [new EvasionEffect
      {
        UnblockableCondition = new EvasionCondition
        {
          ConditionType = EvasionConditionType.DefendingPlayerControls,
          PermanentFilter = new ObjectFilter { Subtypes = ["Mountain"] },
        },
      }],
      Reminder = reminder,
    }
  );
}
