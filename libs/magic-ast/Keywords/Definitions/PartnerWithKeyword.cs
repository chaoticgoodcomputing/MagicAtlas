namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Partner with [Name]: A pair-binding variant of the Partner keyword. The parameter is the
/// literal name of the paired card (e.g., "Amy Pond").
/// Rule 702.124. MAST records the keyword and the partner name; the commander-pairing rules
/// are engine territory.
///
/// <para>
/// <b>Priority 60</b> — must be tried before <see cref="PartnerKeyword"/> (priority 50):
/// both lead with the "Partner" token; giving this a higher priority ensures the registry
/// tries <c>Partner with [Name]</c> first and backtracks to bare <c>Partner</c> only when
/// the "with" token is absent.
/// </para>
/// </summary>
[Keyword(Priority = 60)]
public sealed class PartnerWithKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Partner with",
      RuleReference = "702.124",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Name,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Partner with",
        Effects = [new PartnerEffect
        {
          PartnerType = PartnerType.PartnerWith,
          PartnerName = parameter?.Trim(),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from partner in Keyword("Partner")
    from with_ in Keyword("with")
    from nameWords in Token.EqualTo(OracleToken.Word).AtLeastOnce()
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Partner with",
      Effects = [new PartnerEffect
      {
        PartnerType = PartnerType.PartnerWith,
        PartnerName = string.Join(" ", nameWords.Select(t => t.ToStringValue())),
      }],
      Reminder = reminder,
    }
  );
}
