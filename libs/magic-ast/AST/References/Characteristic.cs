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
      _ => new OtherCharacteristic { Description = label },
    };

  /// <summary>A keyword-ability constraint ("has [keyword]"). Terse construction at parser sites.</summary>
  public static KeywordCharacteristic HasKeyword(KeywordAbility keyword) => new() { Keyword = keyword };

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
