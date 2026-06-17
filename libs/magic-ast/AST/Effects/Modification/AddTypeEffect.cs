namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[Subject] are [card-type(s)] [subtype(s)] in addition to their other types." —
/// a layer-4 (CR 613.1d) continuous effect that ADDITIVELY grants one or more card
/// types and/or subtypes to a group of permanents. Unlike <see cref="SetCardTypesEffect"/>
/// (which replaces) and <see cref="ChangeSubtypeEffect"/> (which replaces subtypes),
/// this node models the "in addition to their other types" clause: existing card types
/// and subtypes are retained and the new type(s) are appended.
///
/// <para>
/// CR 205.1a (verbatim): "Some effects change an object's card type, subtype, and/or
/// supertype but specify that the object retains a prior card type, subtype, and/or
/// supertype. In such cases, all the object's prior card types, subtypes, and supertypes
/// are retained, and the effect causes the object to gain or lose other card types,
/// subtypes, and/or supertypes."
/// </para>
///
/// <para>
/// The canonical example is Ashaya, Soul of the Wild: "Nontoken creatures you control
/// are Forest lands in addition to their other types." → the creatures gain both the
/// 'land' card type and the 'Forest' land subtype while retaining their existing types.
/// </para>
///
/// <para>
/// MAST is descriptive: this node records the oracle declaration. The rules engine
/// applies it in layer 4 and resolves downstream consequences (e.g. the implicit basic
/// land mana ability from CR 305.6 for Forest lands).
/// </para>
/// </summary>
[OracleEffect("addType")]
public sealed record AddTypeEffect : ContinuousEffect
{
  /// <summary>
  /// The permanents that gain the additional type(s).
  /// Typically an <see cref="ObjectReferenceKind.Each"/> reference over a controller-
  /// scoped filter (e.g. "nontoken creatures you control").
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// Card types additively granted (lowercase, matching the
  /// <see cref="ObjectFilter.CardTypes"/> convention — e.g. <c>["land"]</c>).
  /// Null (and omitted) when only subtypes are added.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? AddedCardTypes { get; init; }

  /// <summary>
  /// Subtypes additively granted (PascalCase, matching the
  /// <see cref="ObjectFilter.Subtypes"/> convention — e.g. <c>["Forest"]</c>).
  /// Null (and omitted) when only card types are added.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? AddedSubtypes { get; init; }
}
