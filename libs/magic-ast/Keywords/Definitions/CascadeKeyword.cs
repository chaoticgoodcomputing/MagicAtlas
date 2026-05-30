namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Cascade: When you cast this spell, exile cards from the top of your library
/// until you exile a nonland card whose mana value is less than this spell's mana
/// value. You may cast that card without paying its mana cost. Put the exiled cards
/// not cast on the bottom of your library in a random order.
/// Rule 702.85. MAST records keyword presence; the exile-and-cast machinery is
/// engine territory.
///
/// <para>
/// Combinator-only: no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions.cs</c>. <see cref="Definition"/> returns <c>null</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class CascadeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Cascade")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Cascade",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Cascade }],
      Reminder = reminder,
    }
  );
}
