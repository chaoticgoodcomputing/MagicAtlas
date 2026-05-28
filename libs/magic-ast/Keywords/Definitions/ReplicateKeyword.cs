namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Replicate [cost]: When you cast this spell, copy it for each time you
/// paid its replicate cost. You may choose new targets for the copies.
/// Rule 702.57. Scope: mana-cost parameter (all known printings).
/// The per-payment copy-creation and target-selection are engine territory
/// — MAST records the keyword's presence and cost only, mirroring the
/// Buyback/Conspire copy-spell pattern.
/// </summary>
[Keyword]
public sealed class ReplicateKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Replicate",
      RuleReference = "702.57",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Replicate",
        Effects = [new ReplicateEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Replicate")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Replicate",
      Effects = [new ReplicateEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Mana-cost-parameter parser, inlined from the former
  /// <c>KeywordDefinitions.ParseManaCost</c>.
  /// </summary>
  private static ManaCost ParseManaCost(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Replicate requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
