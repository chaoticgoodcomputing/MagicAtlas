namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Increment (CR 702.191a). A triggered keyword ability printed as a bare
/// keyword token: "Whenever you cast a spell, if this permanent is a creature
/// and the amount of mana spent to cast that spell is greater than this
/// creature's power or this creature's toughness, put a +1/+1 counter on
/// this creature." MAST records the keyword's presence; the trigger condition,
/// mana comparison, and counter placement are engine territory per the
/// descriptive-not-engine doctrine.
///
/// <para>
/// Simple parameterless keyword — combinator-only (no KeywordDefinition in
/// the legacy registry). Tier: Simple. Mirrors <c>TrampleKeyword</c> shape.
/// </para>
/// </summary>
[Keyword]
public sealed class IncrementKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Increment")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Increment",
      Effects = [new IncrementEffect()],
      Reminder = reminder,
    }
  );
}
