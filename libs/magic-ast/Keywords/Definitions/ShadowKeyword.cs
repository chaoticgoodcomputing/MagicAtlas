namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Shadow: This creature can't be blocked except by creatures with shadow, and a
/// creature without shadow can't be blocked by creatures with shadow.
/// Rule 702.28. Mutual evasion: only shadow can block shadow. EvasionEffect with
/// CanBeBlockedBy restricted to the "shadow" characteristic.
/// </summary>
[Keyword]
public sealed class ShadowKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Shadow",
      RuleReference = "702.28",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.Shadow,
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [Characteristic.HasKeyword(KeywordAbility.Shadow)],
          },
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Shadow")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Shadow,
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.HasKeyword(KeywordAbility.Shadow)],
        },
      }],
      Reminder = reminder,
    }
  );
}
