namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Megamorph [cost]: A variant of Morph (Rule 702.37b). The player may cast
/// this card face down as a 2/2 colorless creature for {3}, and may turn it
/// face up by paying its megamorph cost; when turned face up via the megamorph
/// cost, a +1/+1 counter is placed on the permanent. MAST records the keyword
/// and the megamorph cost; the cast-face-down rules, turn-face-up mechanics,
/// and counter-placement are engine territory (descriptive-not-engine doctrine).
/// Rule 702.37b.
/// </summary>
[Keyword]
public sealed class MegamorphKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Megamorph",
      RuleReference = "702.37b",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = KeywordAbility.Megamorph,
        Effects = [new MegamorphEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Megamorph")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Megamorph,
      Effects = [new MegamorphEffect
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
      throw new ArgumentException("Megamorph requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
