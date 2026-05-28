namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Morph {cost}: The player may cast this card face down as a 2/2 colorless creature for {3},
/// and may turn it face up later by paying its morph cost.
/// Rule 702.37. MAST records the keyword and the morph cost; the cast-face-down rules and
/// turn-face-up mechanics are engine territory.
///
/// <para>
/// Combinator-only keyword (no <see cref="KeywordDefinition"/>): Morph has no
/// <c>KeywordDefinitions.Morph</c> entry in the legacy file; only the parser combinator
/// lived in <c>OracleParsers</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class MorphKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Morph")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Morph",
      Effects = [new MorphEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
