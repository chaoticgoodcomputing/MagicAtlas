namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "copy [target]" or "create a copy of [target]"
/// </summary>
[OracleEffect("copy")]
public sealed record CopyEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// How many copies to create. Null means a single copy (the default, e.g. Conspire's
  /// "copy it"). A <see cref="MagicAST.AST.Quantities.KeywordCostPaidCountQuantity"/> for
  /// Replicate — "copy it for each time its replicate cost was paid" (CR 702.56a).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }

  /// <summary>
  /// "Except"-clauses applied to the copy — power/toughness overrides,
  /// type additions, ability additions.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<CopyModification>? Modifications { get; init; }

  /// <summary>
  /// "You may choose new targets for the copy" — the controller of the copy
  /// may select new legal targets for it (Dualcaster Mage's ETB spell-copy).
  /// When <c>true</c>, the option to retarget the copy is granted; null/false
  /// means the copy keeps the original spell's targets. CR 707.10: a copy of a
  /// spell is put onto the stack and is not cast, so its targets are inherited
  /// from the copied spell unless an effect grants this reselection. A structured
  /// flag rather than free text — the retarget permission is rules-meaningful.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? MayChooseNewTargets { get; init; }

  /// <summary>
  /// The player who creates the copy tokens (CR 111.2). When null, the implicit
  /// creator is the ability's controller ("you"). Cards that name a different
  /// creator — "its controller creates two tokens that are copies" (Saw in Half)
  /// — carry an explicit <see cref="ObjectReference"/> here (e.g.
  /// <see cref="ObjectReferenceKind.Controller"/>). Parallels
  /// <see cref="MagicAST.AST.Effects.TokenCopy.CreateTokenEffect.Player"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Player { get; init; }
}
