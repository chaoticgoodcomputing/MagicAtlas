namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Defender: A creature with defender can't attack.
/// Rule 702.3. MAST records keyword presence; the can't-attack semantics are
/// derived by the rules engine.
/// Handles both bare "Defender" and "Defender (This creature can't attack.)"
/// — reminder text is consumed but not stored in the AST.
///
/// <para>Combinator-only keyword — no <c>KeywordDefinition</c> entry exists in the
/// legacy <c>KeywordDefinitions.All</c> list for this keyword.</para>
/// </summary>
[Keyword]
public sealed class DefenderKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Defender")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Defender",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Defender }],
      Reminder = reminder,
    }
  );
}
