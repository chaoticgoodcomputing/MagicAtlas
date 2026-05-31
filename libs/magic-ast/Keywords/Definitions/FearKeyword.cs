namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Fear: This creature can't be blocked except by artifact creatures and/or black creatures.
/// Rule 702.36. MAST records keyword presence; the evasion semantics are expressed via
/// EvasionEffect with a Characteristics-stretch ObjectFilter covering both the artifact
/// type and the black color qualifier.
/// </summary>
[Keyword]
public sealed class FearKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Fear",
      RuleReference = "702.36",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Fear",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [Characteristic.Other("artifact"), Characteristic.Other("black")],
          },
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Fear")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Fear",
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.Other("artifact"), Characteristic.Other("black")],
        },
      }],
      Reminder = reminder,
    }
  );
}
