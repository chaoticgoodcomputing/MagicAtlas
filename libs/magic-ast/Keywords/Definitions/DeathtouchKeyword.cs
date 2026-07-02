namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Deathtouch: Any nonzero damage dealt by a source with deathtouch destroys the
/// damaged creature.
/// Rule 702.2. MAST records keyword presence; the damage-to-destroy semantics are
/// engine territory.
///
/// <para>Combinator-only keyword — no <c>KeywordDefinition</c> entry exists in the
/// legacy <c>KeywordDefinitions.All</c> list for this keyword.</para>
/// </summary>
[Keyword]
public sealed class DeathtouchKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Deathtouch")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Deathtouch,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Deathtouch }],
      Reminder = reminder,
    }
  );
}
