namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Kicker {cost} (CR 702.33): "Kicker is a static ability that functions while the spell
/// with kicker is on the stack. 'Kicker [cost]' means 'You may pay an additional [cost]
/// as you cast this spell.'" It is a static ability, so the combinator emits a
/// <see cref="StaticAbility"/> carrying the shared <see cref="AdditionalCastCostEffect"/>
/// primitive (<c>IsOptional = true</c> — "you may pay"; <c>Repeatable = false</c> — at
/// most once, distinguishing it from Multikicker's <c>Repeatable = true</c>). The linked
/// "if this spell was kicked, ..." resolution (CR 702.33e / 607) is a separate ability on
/// the card and is not modeled here (ADR 0003/0004 describe-not-execute).
///
/// <para>
/// Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions</c> registry. The <c>Keyword("Kicker")</c> word-matcher matches
/// the whole word "Kicker", so the longer word "Multikicker" (CR 702.33c, handled by
/// <see cref="MultikickerKeyword"/>) does not match this combinator.
/// </para>
/// </summary>
[Keyword]
public sealed class KickerKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Kicker")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Kicker,
      Effects = [new AdditionalCastCostEffect
      {
        Cost = cost,
        IsOptional = true,
      }],
      Reminder = reminder,
    }
  );
}
