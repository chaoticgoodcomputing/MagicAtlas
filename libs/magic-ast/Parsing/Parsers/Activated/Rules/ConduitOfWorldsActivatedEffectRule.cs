namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Choose target nonland permanent card in your graveyard. If you haven't
/// cast a spell this turn, you may cast that card. If you do, you can't cast
/// additional spells this turn." — Conduit of Worlds' activated-ability effect.
///
/// <para>
/// This is a three-sentence activated-ability body that forms one semantically
/// coupled unit — the "Choose target" targeting declaration, a history-predicate
/// gate, and a conditional cast permission with an IfYouDo restriction — and must
/// be recognized as a whole rather than dispatched sentence-by-sentence.
/// </para>
///
/// <para>
/// AST shape:
/// <list type="bullet">
///   <item>
///     <see cref="ConditionalEffect"/> whose <c>Condition</c> is a
///     <see cref="CountCondition"/> checking that you have cast zero spells this
///     turn (<see cref="CastThisTurnPredicate"/> with Caster=You, Count=Equal:0).
///   </item>
///   <item>
///     The <c>Then</c> branch is an <see cref="OptionalEffect"/> ("you may") wrapping a
///     <see cref="MayCastTargetFromGraveyardEffect"/> targeting the chosen nonland
///     permanent card in the controller's graveyard.
///   </item>
///   <item>
///     The <see cref="OptionalEffect.IfYouDo"/> consequence is a
///     <see cref="CantCastAdditionalSpellsThisTurnEffect"/> — the controller may cast
///     no further spells this turn.
///   </item>
/// </list>
/// </para>
///
/// <para>
/// CR 602.5d (verbatim): "Activated abilities that read 'Activate only as a sorcery'
/// mean the player must follow the timing rules for casting a sorcery spell, though
/// the ability isn't actually a sorcery." (The <c>OnlyAsSorcery</c> restriction is
/// stripped by <see cref="MagicAST.Parsing.Parsers.ActivatedAbilityParser"/> before
/// this rule is invoked.)
/// </para>
///
/// <para>
/// ANCHORED (^…$) on the full three-sentence pattern so it cannot fire as a substring
/// of a more-specific sibling or be claimed by a looser dispatcher.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 960)]
public sealed class ConduitOfWorldsActivatedEffectRule
  : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // Anchored match on the trimmed, trailing-period-stripped effect text that
  // TryParseMultiRuleEffects hands to TryMatchMulti.
  private static readonly Regex Pattern = new(
    @"^Choose\s+target\s+nonland\s+permanent\s+card\s+in\s+your\s+graveyard\.\s+" +
    @"If\s+you\s+haven't\s+cast\s+a\s+spell\s+this\s+turn,\s+you\s+may\s+cast\s+that\s+card\.\s+" +
    @"If\s+you\s+do,\s+you\s+can't\s+cast\s+additional\s+spells\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns null — this shape is served exclusively via
  /// <see cref="TryMatchMulti"/> so the single-effect path never claims it.
  /// </remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    if (!Pattern.IsMatch(effectText.Trim()))
    {
      return false;
    }

    // "Choose target nonland permanent card in your graveyard" — the targeting
    // declaration. The chosen card is referenced as {Kind: Target} in subsequent
    // sentences; its filter captures the declared targeting restriction.
    var targetFilter = new ObjectFilter
    {
      CardTypes = ["permanent"],
      ExcludedCardTypes = ["land"],
      Zone = Zone.Graveyard,
      Controller = ControllerFilter.You,
    };

    // "If you haven't cast a spell this turn" →
    // CountCondition{ Filter:{History:CastThisTurnPredicate{Caster:You}}, Count:{Equal,0} }
    var condition = new CountCondition
    {
      Filter = new ObjectFilter
      {
        History = new CastThisTurnPredicate { Caster = ControllerFilter.You },
      },
      Count = new Comparison
      {
        Operator = ComparisonOperator.Equal,
        Value = 0,
      },
    };

    // "you may cast that card" — optional cast of the targeted graveyard card
    // "If you do, you can't cast additional spells this turn" — IfYouDo restriction
    var castEffect = new MayCastTargetFromGraveyardEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = targetFilter,
      },
    };

    var optionalCast = new OptionalEffect
    {
      Inner = castEffect,
      IfYouDo = new CantCastAdditionalSpellsThisTurnEffect(),
    };

    var conditional = new ConditionalEffect
    {
      Condition = condition,
      Then = optionalCast,
    };

    effects = [conditional];
    return true;
  }
}
