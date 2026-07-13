namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Disturb {cost} (CR 702.146a, verbatim): "Disturb is an ability found on the front
/// face of some double-faced cards (see rule 712, "Double-Faced Cards"). 'Disturb
/// [cost]' means 'You may cast this card transformed from your graveyard by paying
/// [cost] rather than its mana cost.' See rule 712.8c."
///
/// <para>
/// It is a static ability (like Flashback/Escape), so the combinator emits a
/// <see cref="StaticAbility"/> carrying the shared <see cref="AlternativeCastEffect"/>
/// primitive (<c>FromZone = Zone.Graveyard</c>, <c>Cost = </c> the disturb cost) —
/// mirroring <see cref="FlashbackKeyword"/>. The distinguishing semantic vs. Flashback
/// is that the card is cast with its back face up, not as printed: recorded via
/// <see cref="AlternativeCastEffect.Transformed"/> = true so the graveyard-zone and
/// transformed-cast structure are both preserved rather than conflated with
/// Flashback's cast-as-printed permission. The post-cast zone bookkeeping (CR 712.8c)
/// is engine territory (ADR 0003/0004 describe-not-execute) and is not modeled.
/// Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions</c> registry.
/// </para>
/// </summary>
[Keyword]
public sealed class DisturbKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Disturb")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Disturb,
      Effects = [new AlternativeCastEffect
      {
        FromZone = Zone.Graveyard,
        Cost = cost,
        Transformed = true,
      }],
      Reminder = reminder,
    }
  );
}
