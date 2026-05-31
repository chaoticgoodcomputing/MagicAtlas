namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Bushido N: Whenever this creature blocks or becomes blocked, it gets +N/+N
/// until end of turn.
/// Rule 702.45. MAST records the keyword and its integer value; the
/// trigger-and-buff expansion is engine territory.
///
/// <para>
/// Combinator-only: no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions.cs</c>. <see cref="Definition"/> returns <c>null</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class BushidoKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Bushido")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Bushido",
      Effects = [new BushidoEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );
}
