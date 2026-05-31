namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// First strike: This creature deals combat damage before creatures without first
/// strike. Rule 702.7.
///
/// <para>
/// Exemplar of the <b>multi-word simple</b> keyword shape (Stage A template). The
/// combinator matches two sequential word tokens ("First" then "Strike"/"strike") via
/// chained <see cref="KeywordCombinators.Keyword(string)"/> calls. The
/// <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.FirstStrike</c>;
/// the <see cref="Combinator"/> is the verbatim former <c>OracleParsers.FirstStrike</c>.
/// </para>
///
/// <para>
/// <b>Ordering note:</b> multi-word keywords whose first token is a prefix shared with
/// a shorter keyword must sort earlier in the Or-chain (raise <see cref="KeywordAttribute.Priority"/>).
/// "First" has no single-word collision today, so default priority 50 preserves the
/// legacy ordering. The legacy chain placed First strike / Double strike late; because
/// their leading tokens ("First"/"Double") are unique, order among unrelated keywords
/// is immaterial to first-success-wins.
/// </para>
/// </summary>
[Keyword]
public sealed class FirstStrikeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "First strike",
      RuleReference = "702.7",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.FirstStrike,
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from first in Keyword("First")
    from strike in Keyword("Strike").Or(Keyword("strike"))
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.FirstStrike,
      Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      Reminder = reminder,
    }
  );
}
