namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Banding: Legacy combat ability (Rule 702.22). Any creatures with banding can
/// attack in a band; the attacking player controls how the defending player assigns
/// damage to the band. MAST records keyword presence; combat-band semantics are
/// engine territory. Reminder text varies but keyword token is uniform.
///
/// <para>
/// Combinator-only keyword — no <c>KeywordDefinitions.Banding</c> static property
/// exists; <see cref="Definition"/> is <c>null</c>. The combinator is the verbatim
/// former <c>OracleParsers.Banding</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class BandingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Banding")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Banding",
      Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Banding }],
      Reminder = reminder,
    }
  );
}
