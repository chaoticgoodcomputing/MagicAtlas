namespace MagicAST.Keywords.Definitions;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Flanking: Whenever this creature becomes blocked by a creature without flanking,
/// the blocking creature gets -1/-1 until end of turn.
///
/// CR 702.25 (verbatim): "Flanking is a triggered ability that triggers during the
/// declare blockers step. 'Flanking' means 'Whenever this creature becomes blocked by
/// a creature without flanking, the blocking creature gets -1/-1 until end of turn.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Flanking",
///   Trigger:{ Timing:"Whenever", Event:"BecomesBlocked",
///             Filter:{CardTypes:["creature"],
///                     Characteristics:[OtherCharacteristic{Description:"withoutFlanking"}]} },
///   Effects:[ ModifyPTEffect{ Target:{Kind:ThatCreature},
///                             PowerModifier:-1, ToughnessModifier:-1,
///                             Duration:untilEndOfTurn } ] }.
///
/// <para>
/// The trigger's Filter encodes the blocker qualification ("by a creature without
/// flanking") — TriggerCondition has a single Filter field and no dedicated By/blocker
/// axis; the blocking-creature qualifier is placed there and noted. The effect target
/// is ObjectReferenceKind.ThatCreature (the creature named by the trigger condition —
/// the blocking creature), not Self/It (those refer to the Flanking creature itself).
/// </para>
/// </summary>
[Keyword]
public sealed class FlankingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Flanking",
      RuleReference = "702.25",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => BuildAbility(null),
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Flanking")
    from reminder in OptionalReminder
    select BuildAbility(reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for Flanking: whenever this creature
  /// becomes blocked by a creature without flanking, that blocking creature gets
  /// -1/-1 until end of turn.
  /// </summary>
  private static Ability BuildAbility(Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Flanking,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.BecomesBlocked,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.Other("withoutFlanking")],
        },
      },
      Effects =
      [
        new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
          PowerModifier = LiteralQuantity.Of(-1),
          ToughnessModifier = LiteralQuantity.Of(-1),
          Duration = UntilTimeDuration.EndOfTurn,
        },
      ],
      Reminder = reminder,
    };
}
