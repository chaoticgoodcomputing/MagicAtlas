namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Convoke: Your creatures can help cast this spell. Each creature you tap while
/// casting this spell pays for {1} or one mana of that creature's color.
/// Rule 702.51. MAST records the keyword's presence; the per-creature cost-reduction
/// mechanic is engine territory. Combinator-only: no matching <c>KeywordDefinitions</c>
/// entry exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class ConvokeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Convoke")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Convoke,
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Convoke }],
      Reminder = reminder,
    }
  );
}
