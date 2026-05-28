namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Miracle {cost}: You may cast this card for its miracle cost when you draw it,
/// if it's the first card you drew this turn. Rule 702.94. MAST records the keyword
/// and the printed alternative cost; the draw-trigger and cast-timing mechanics are
/// conventionally inferred from the rules and captured in the reminder parenthetical.
///
/// <para>
/// Combinator-only: no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions.cs</c>. <see cref="Definition"/> returns <c>null</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class MiracleKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Miracle")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Miracle",
      Effects = [new MiracleEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
