namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Evoke [cost]: You may cast this spell for its evoke cost. If you do, it's
/// sacrificed when it enters.
/// Rule 702.73. Scope: mana-cost parameter (all known printings).
/// The sacrifice-on-entry semantics are engine territory — MAST records the
/// keyword's presence and cost only, mirroring the Dash/Plot/Ninjutsu pattern.
/// </summary>
[Keyword]
public sealed class EvokeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Evoke",
      RuleReference = "702.73",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Evoke",
        Effects = [new EvokeEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Evoke")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Evoke",
      Effects = [new EvokeEffect
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
      throw new ArgumentException("Evoke requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
