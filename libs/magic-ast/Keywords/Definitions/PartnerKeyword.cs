namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Partner (parameterless): You can have two commanders if both have partner.
/// Rule 702.124. The bare form allows any two Partner commanders to pair up, as opposed to
/// "Partner with [Name]" which binds two specific cards.
///
/// <para>
/// Default priority 50 — must be tried <b>after</b> <see cref="PartnerWithKeyword"/> (priority 60):
/// both lead with the "Partner" token; <c>PartnerWith</c> backtracks when "with" is absent,
/// leaving this parser to match the bare form.
/// </para>
/// </summary>
[Keyword]
public sealed class PartnerKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Partner",
      RuleReference = "702.124",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Partner",
        Effects = [new PartnerEffect
        {
          PartnerType = PartnerType.Partner,
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Partner")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Partner",
      Effects = [new PartnerEffect
      {
        PartnerType = PartnerType.Partner,
      }],
      Reminder = reminder,
    }
  );
}
