namespace MagicAST.AST.References;

using System.Text.Json.Serialization;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A single characteristic constraint on an <see cref="ObjectFilter"/> beyond
/// the structured axes (card type, subtype, color, zone, comparisons). Where a
/// constraint has a first-class home on <see cref="ObjectFilter"/>, use that;
/// this union is for constraints that do not yet — modelled honestly as typed
/// variants rather than the bare strings this field used to carry.
///
/// <para>
/// Today it distinguishes a keyword-ability constraint
/// (<see cref="KeywordCharacteristic"/>) from the typed residual
/// (<see cref="OtherCharacteristic"/>) that holds anything not yet structured.
/// The residual is a deferral, not a destination: state predicates (tapped,
/// attacking), negated types/colors, and relational predicates are carved out
/// into their own variants — or routed to existing <see cref="ObjectFilter"/>
/// fields — in follow-up work.
/// </para>
/// </summary>
[PolymorphicBase("CharacteristicType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Characteristic>))]
public abstract record Characteristic
{
  /// <summary>
  /// Maps a raw oracle characteristic label to its structured form: a
  /// <see cref="KeywordCharacteristic"/> when the label names a keyword ability
  /// this AST structures, otherwise the typed residual
  /// <see cref="OtherCharacteristic"/>. Case-insensitive on the label; the
  /// residual preserves the caller's text verbatim.
  /// </summary>
  public static Characteristic FromLabel(string label) =>
    label.Trim().ToLowerInvariant() switch
    {
      "flying" or "with flying" => new KeywordCharacteristic { Keyword = KeywordAbility.Flying },
      "reach" => new KeywordCharacteristic { Keyword = KeywordAbility.Reach },
      "shadow" => new KeywordCharacteristic { Keyword = KeywordAbility.Shadow },
      "deathtouch" => new KeywordCharacteristic { Keyword = KeywordAbility.Deathtouch },
      "trample" => new KeywordCharacteristic { Keyword = KeywordAbility.Trample },
      "haste" => new KeywordCharacteristic { Keyword = KeywordAbility.Haste },
      "vigilance" => new KeywordCharacteristic { Keyword = KeywordAbility.Vigilance },
      "indestructible" => new KeywordCharacteristic { Keyword = KeywordAbility.Indestructible },
      "infect" => new KeywordCharacteristic { Keyword = KeywordAbility.Infect },
      "hexproof" => new KeywordCharacteristic { Keyword = KeywordAbility.Hexproof },
      "shroud" => new KeywordCharacteristic { Keyword = KeywordAbility.Shroud },
      "attacking" => new CombatStateCharacteristic { State = CombatState.Attacking },
      "blocking" => new CombatStateCharacteristic { State = CombatState.Blocking },
      "attacking or blocking" => new CombatStateCharacteristic
      {
        State = CombatState.AttackingOrBlocking,
      },
      "attacking alone" => new CombatStateCharacteristic { State = CombatState.AttackingAlone },
      "tapped" => new TappedStateCharacteristic { Tapped = true },
      "untapped" => new TappedStateCharacteristic { Tapped = false },
      "equipped" => new EquippedStateCharacteristic(),
      "with a +1/+1 counter" or "with a +1/+1 counter on it" => new CounterCharacteristic
      {
        CounterType = "+1/+1",
      },
      _ => new OtherCharacteristic { Description = label },
    };

  /// <summary>A keyword-ability constraint ("has [keyword]"). Terse construction at parser sites.</summary>
  public static KeywordCharacteristic HasKeyword(KeywordAbility keyword) => new() { Keyword = keyword };

  /// <summary>A combat-state constraint ("attacking creatures", "blocking creature"). Terse construction at parser sites.</summary>
  public static CombatStateCharacteristic InCombat(CombatState state) => new() { State = state };

  /// <summary>The typed residual for a not-yet-structured characteristic phrase.</summary>
  public static OtherCharacteristic Other(string description) => new() { Description = description };
}

/// <summary>
/// The filtered object must have a keyword ability — e.g. "creature with
/// flying", or (inside an evasion <c>CanBeBlockedBy</c> filter) "can be blocked
/// only by creatures with flying".
/// </summary>
[CharacteristicKind("keyword")]
public sealed record KeywordCharacteristic : Characteristic
{
  /// <summary>The required keyword ability.</summary>
  public required KeywordAbility Keyword { get; init; }
}

/// <summary>
/// A combat-state constraint — the filtered object is attacking and/or blocking.
/// "Attacking creatures get +1/+1", "deals damage to target blocking creature",
/// "exile target attacking creature". The combat-state predicate the
/// <see cref="Characteristic"/> doc calls out as a first-class carve-out from the
/// <see cref="OtherCharacteristic"/> residual. Describes what the oracle text says
/// (the object is in this combat role); the turn/combat machinery is engine
/// territory (CR 508/509).
/// </summary>
[CharacteristicKind("combatState")]
public sealed record CombatStateCharacteristic : Characteristic
{
  /// <summary>Which combat role the filtered object is in.</summary>
  public required CombatState State { get; init; }
}

/// <summary>The combat role a <see cref="CombatStateCharacteristic"/> constrains.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CombatState
{
  /// <summary>Attacking (CR 508).</summary>
  Attacking,

  /// <summary>Blocking (CR 509).</summary>
  Blocking,

  /// <summary>Attacking or blocking — the disjunction "attacking or blocking creature".</summary>
  AttackingOrBlocking,

  /// <summary>Attacking alone — attacking with no other attacker (e.g. "can't attack alone" companions).</summary>
  AttackingAlone,
}

/// <summary>
/// A tapped/untapped state constraint — "tapped creature" (Vengeance, CR 110.5a) or
/// "untapped creature" (Saryth). The state predicate the <see cref="Characteristic"/>
/// doc calls out as a first-class carve-out from the <see cref="OtherCharacteristic"/>
/// residual. Describes what the oracle text says (the object is tapped, or untapped);
/// the actual tap status is engine territory (CR 701.21 / 110.5).
/// </summary>
[CharacteristicKind("tapped")]
public sealed record TappedStateCharacteristic : Characteristic
{
  /// <summary><c>true</c> for "tapped", <c>false</c> for "untapped".</summary>
  public required bool Tapped { get; init; }
}

/// <summary>
/// An equipped-state constraint — "Equipped creatures" / "Equipped Warriors"
/// (Kor Blademaster: "Equipped Warriors you control have double strike."). The
/// attachment-state analogue of <see cref="TappedStateCharacteristic"/>: the
/// filtered object currently has an Equipment attached to it (CR 702.6). Same
/// category the <see cref="Characteristic"/> doc calls out as a first-class
/// carve-out from the <see cref="OtherCharacteristic"/> residual, and the
/// filter-axis sibling of <c>ObjectIsEquippedCondition</c> (which carries the
/// same "is equipped" predicate for a single back-referenced object rather
/// than a filtered set). MAST records only that the granted-to set is scoped
/// to permanents currently carrying an attached Equipment; the actual
/// attachment state is engine territory (CR 702.6, 704.5n).
/// </summary>
[CharacteristicKind("equipped")]
public sealed record EquippedStateCharacteristic : Characteristic;

/// <summary>
/// A counter constraint — the filtered object must have a counter of a given kind,
/// e.g. "creature with a +1/+1 counter on it" (Crowned Ceratok, Sapphire Drake;
/// CR 122). The counter-presence predicate carved out of the
/// <see cref="OtherCharacteristic"/> residual. With no <see cref="Count"/> the
/// constraint is mere presence ("a counter"); a <see cref="Count"/> comparison
/// constrains how many ("two or more +1/+1 counters").
/// </summary>
[CharacteristicKind("counter")]
public sealed record CounterCharacteristic : Characteristic
{
  /// <summary>The counter kind, as printed — e.g. <c>"+1/+1"</c>, <c>"charge"</c> (CR 122.1).</summary>
  public required string CounterType { get; init; }

  /// <summary>
  /// Optional constraint on the number of such counters ("two or more +1/+1 counters").
  /// Null means mere presence ("a +1/+1 counter").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? Count { get; init; }
}

/// <summary>
/// Typed residual for a characteristic constraint that does not yet have a
/// structured variant — carries the literal oracle phrase. Use sparingly;
/// prefer a structured variant (or an existing <see cref="ObjectFilter"/> field)
/// when the shape recurs.
/// </summary>
[CharacteristicKind("other")]
public sealed record OtherCharacteristic : Characteristic, IResidual
{
  /// <summary>The literal characteristic phrase from the oracle text.</summary>
  public required string Description { get; init; }
}
