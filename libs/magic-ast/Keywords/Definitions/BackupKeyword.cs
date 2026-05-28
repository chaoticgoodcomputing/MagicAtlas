namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Backup N: When this creature enters, put N +1/+1 counters on target creature.
/// If that is another creature, it also gains the non-backup abilities printed
/// below this one until end of turn.
/// Rule 702.165. MAST records the keyword and its integer value; the counter
/// placement, ability-grant, and "printed below this one" scoping are engine
/// territory.
///
/// <para>
/// The <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Backup</c>
/// (including its inlined <c>ParseIntValue</c> guard); the <see cref="Combinator"/> is
/// the verbatim former <c>OracleParsers.Backup</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class BackupKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Backup",
      RuleReference = "702.165",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Backup",
        Effects = [new BackupEffect
        {
          Value = ParseIntValue("Backup", parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Backup")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Backup",
      Effects = [new BackupEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Integer-parameter guard, inlined from the former <c>KeywordDefinitions.ParseIntValue</c>.
  /// </summary>
  private static int ParseIntValue(string keywordName, string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException($"{keywordName} requires a numeric parameter.", nameof(parameter));
    }

    if (!int.TryParse(parameter.Trim(), out var value))
    {
      throw new ArgumentException(
        $"{keywordName} parameter must be an integer, got '{parameter}'.",
        nameof(parameter)
      );
    }

    return value;
  }
}
