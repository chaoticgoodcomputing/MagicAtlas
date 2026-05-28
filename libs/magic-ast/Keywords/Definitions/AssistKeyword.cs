namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Assist (Rule 702.132). A cooperative-format keyword from Battlebond: another
/// player may pay up to a specified amount of the spell's generic mana cost. The
/// cap amount is printed only in reminder text — the oracle keyword is the bare
/// word "Assist" with no parameters. MAST records keyword presence only.
///
/// <para>
/// Combinator-only keyword — no <c>KeywordDefinitions.Assist</c> legacy entry exists;
/// <see cref="Definition"/> returns <c>null</c>. <see cref="Tier"/> is
/// <see cref="KeywordTier.Simple"/> because no argument follows the keyword token.
/// </para>
/// </summary>
[Keyword]
public sealed class AssistKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Assist")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Assist",
      Effects = [new AssistEffect()],
      Reminder = reminder,
    }
  );
}
