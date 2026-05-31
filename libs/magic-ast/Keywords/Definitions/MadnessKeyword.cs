namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Madness {cost}: Lets a player cast a discarded card for its madness cost rather
/// than putting it into the graveyard. Rule 702.35. MAST records the keyword and
/// the alternative cost; discard-into-exile-and-cast machinery is engine territory.
///
/// <para>
/// Combinator-only keyword: Madness has no entry in <c>KeywordDefinitions.All</c>
/// (it is not registered as a <see cref="KeywordDefinition"/>), so
/// <see cref="Definition"/> is <see langword="null"/>.
/// </para>
/// </summary>
[Keyword]
public sealed class MadnessKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Madness")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Madness",
      Effects = [new MadnessEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
