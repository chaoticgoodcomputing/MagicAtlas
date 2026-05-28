namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Flying: This creature can't be blocked except by creatures with flying or reach.
/// Rule 702.9.
///
/// <para>
/// Exemplar of the <b>simple parameterless</b> keyword shape (Stage A template). The
/// <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Flying</c>;
/// the <see cref="Combinator"/> is the verbatim former <c>OracleParsers.Flying</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class FlyingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Flying",
      RuleReference = "702.9",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Flying",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["flying", "reach"],
          },
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Flying")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flying",
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["flying", "reach"],
        },
      }],
      Reminder = reminder,
    }
  );
}
