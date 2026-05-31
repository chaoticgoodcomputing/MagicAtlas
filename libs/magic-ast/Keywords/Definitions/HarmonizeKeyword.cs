namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Harmonize {cost}: You may cast this card from your graveyard for its harmonize cost.
/// You may tap a creature you control to reduce that cost by {X}, where X is its power.
/// Then exile this spell.
/// Rule 702.157. MAST records the keyword and its associated mana cost; the
/// graveyard-cast, power-based cost reduction, and exile-after-cast mechanics are
/// conventionally inferred from the rules (reminder text).
///
/// <para>
/// Combinator-only: no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions.cs</c>. <see cref="Definition"/> returns <c>null</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class HarmonizeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Harmonize")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Harmonize",
      Effects = [new HarmonizeEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
