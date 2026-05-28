namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Evolve: Whenever a creature you control enters, if that creature has greater
/// power or toughness than this creature, put a +1/+1 counter on this creature.
/// Rule 702.100. Although mechanically a triggered ability, MAST records it as
/// a keyword marker — same approach as Prowess, Exalted, Cascade — and treats
/// the canonical trigger / power-comparison / counter-placement as engine territory.
/// </summary>
[Keyword]
public sealed class EvolveKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Evolve",
      RuleReference = "702.100",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Evolve",
        Effects = [new EvolveEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Evolve")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Evolve",
      Effects = [new EvolveEffect()],
      Reminder = reminder,
    }
  );
}
