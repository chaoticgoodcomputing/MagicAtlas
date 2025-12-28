namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
// Import all effect subdirectories
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Base type for all effects in Magic.
/// Effects are what happens when spells and abilities resolve.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "effectType")]
// Damage
[JsonDerivedType(typeof(DealDamageEffect), "dealDamage")]
[JsonDerivedType(typeof(PreventDamageEffect), "preventDamage")]
[JsonDerivedType(typeof(LifelinkEffect), "lifelink")]
// ZoneChange
[JsonDerivedType(typeof(DestroyEffect), "destroy")]
[JsonDerivedType(typeof(ExileEffect), "exile")]
[JsonDerivedType(typeof(ReturnToHandEffect), "returnToHand")]
[JsonDerivedType(typeof(ReturnToBattlefieldEffect), "returnToBattlefield")]
[JsonDerivedType(typeof(SacrificeEffect), "sacrifice")]
[JsonDerivedType(typeof(MillEffect), "mill")]
[JsonDerivedType(typeof(SearchLibraryEffect), "searchLibrary")]
[JsonDerivedType(typeof(ShuffleEffect), "shuffle")]
// CardFlow
[JsonDerivedType(typeof(DrawCardsEffect), "drawCards")]
[JsonDerivedType(typeof(DiscardCardsEffect), "discardCards")]
[JsonDerivedType(typeof(ScryEffect), "scry")]
[JsonDerivedType(typeof(SurveilEffect), "surveil")]
[JsonDerivedType(typeof(LookAtCardsEffect), "lookAtCards")]
// Counter
[JsonDerivedType(typeof(PutCountersEffect), "putCounters")]
[JsonDerivedType(typeof(RemoveCountersEffect), "removeCounters")]
// Resource
[JsonDerivedType(typeof(GainLifeEffect), "gainLife")]
[JsonDerivedType(typeof(LoseLifeEffect), "loseLife")]
[JsonDerivedType(typeof(AddManaEffect), "addMana")]
[JsonDerivedType(typeof(CostReductionEffect), "costReduction")]
// TokenCopy
[JsonDerivedType(typeof(CreateTokenEffect), "createToken")]
[JsonDerivedType(typeof(CopyEffect), "copy")]
[JsonDerivedType(typeof(CreateEmblemEffect), "createEmblem")]
// Modification
[JsonDerivedType(typeof(ModifyPTEffect), "modifyPT")]
[JsonDerivedType(typeof(GainAbilityEffect), "gainAbility")]
[JsonDerivedType(typeof(LoseAbilityEffect), "loseAbility")]
[JsonDerivedType(typeof(GainControlEffect), "gainControl")]
[JsonDerivedType(typeof(ExchangeCharacteristicEffect), "exchangeCharacteristic")]
// Control
[JsonDerivedType(typeof(TapEffect), "tap")]
[JsonDerivedType(typeof(UntapEffect), "untap")]
[JsonDerivedType(typeof(DoesntUntapEffect), "doesntUntap")]
[JsonDerivedType(typeof(CounterSpellEffect), "counterSpell")]
// Keyword
[JsonDerivedType(typeof(EvasionEffect), "evasion")]
[JsonDerivedType(typeof(ReachEffect), "reach")]
[JsonDerivedType(typeof(VigilanceEffect), "vigilance")]
[JsonDerivedType(typeof(TrampleEffect), "trample")]
[JsonDerivedType(typeof(HasteEffect), "haste")]
[JsonDerivedType(typeof(ProtectionEffect), "protection")]
[JsonDerivedType(typeof(CantBeCounteredEffect), "cantBeCountered")]
[JsonDerivedType(typeof(PartnerEffect), "partner")]
// Combat
[JsonDerivedType(typeof(CombatDamageTimingEffect), "combatDamageTiming")]
[JsonDerivedType(typeof(TargetingRestrictionEffect), "targetingRestriction")]
[JsonDerivedType(typeof(EnchantRestrictionEffect), "enchantRestriction")]
// Timing
[JsonDerivedType(typeof(TimingModificationEffect), "timingModification")]
[JsonDerivedType(typeof(CastWithoutPayingEffect), "castWithoutPaying")]
[JsonDerivedType(typeof(CommanderDesignationEffect), "commanderDesignation")]
// Replacement
[JsonDerivedType(typeof(ReplacementEffect), "replacement")]
// Core
[JsonDerivedType(typeof(CompositeEffect), "composite")]
[JsonDerivedType(typeof(UnparsedEffect), "unparsed")]
public abstract record Effect
{
  /// <summary>
  /// Duration of this effect, if temporary.
  /// </summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>
  /// Whether this effect is optional ("you may...").
  /// </summary>
  [JsonPropertyName("isOptional")]
  public bool IsOptional { get; init; }

  /// <summary>
  /// Secondary effect that happens "if you do" perform the main effect.
  /// </summary>
  [JsonPropertyName("ifYouDo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>
  /// "Unless [player] pays [cost]" clause that can prevent this effect.
  /// Common in cards like Rhystic Study, Mystic Remora, Ward.
  /// </summary>
  [JsonPropertyName("unlessClause")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}

/// <summary>
/// Represents an "unless [player] pays [cost]" clause.
/// </summary>
public sealed record UnlessClause
{
  /// <summary>
  /// The player who can pay to prevent the effect.
  /// </summary>
  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The cost that can be paid to prevent the effect.
  /// </summary>
  [JsonPropertyName("cost")]
  public required Cost Cost { get; init; }
}
