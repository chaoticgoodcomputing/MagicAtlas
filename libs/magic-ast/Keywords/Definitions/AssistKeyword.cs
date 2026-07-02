namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Assist (CR 702.132a): "Assist is a static ability that modifies the rules of paying
/// for the spell with assist (see rules 601.2g-h). If the total cost to cast a spell with
/// assist includes a generic mana component, before you activate mana abilities while
/// casting it, you may choose another player. That player has a chance to activate mana
/// abilities... the player you chose may pay for any amount of the generic mana in the
/// spell's total cost."
///
/// <para>
/// Decomposed to <see cref="AlternativePaymentEffect"/> with
/// <see cref="AlternativePaymentMethod.DelegatePayment"/> (another player pays) and
/// <see cref="AlternativePaymentKind.Generic"/> (generic mana only). Source is null
/// because payment is delegated to a player, not drawn from objects.
/// </para>
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
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Assist")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Assist,
      Effects =
      [
        new AlternativePaymentEffect
        {
          Method = AlternativePaymentMethod.DelegatePayment,
          Pays = AlternativePaymentKind.Generic,
        },
      ],
      Reminder = reminder,
    }
  );
}
