namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block permission (blocker-side): oracle text states that a creature
/// "can block an additional creature each combat" or "can block any number of
/// creatures." Rule 509.1a (the defending player may declare more than one
/// blocker for the same or additional attackers when an effect explicitly grants
/// the extra-blocker permission).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records that
/// the card's oracle line grants the named object permission to block more
/// creatures per combat phase than it otherwise could; it does not model the
/// runtime application of that permission during the declare-blockers step.
///
/// <para>
/// <see cref="IsUnlimited"/> distinguishes the two oracle-text shapes:
/// <list type="bullet">
///   <item><description>
///     <b>true</b> — "can block any number of creatures" (Guard Gomazoa pattern);
///     no numeric limit is stated in the oracle text.
///   </description></item>
///   <item><description>
///     <b>false</b> (default) — "can block an additional creature each combat"
///     (Foriysian Brigade, Two-Headed Giant of Foriys, etc.). Omitted from JSON
///     when false so existing fixtures require no changes.
///   </description></item>
/// </list>
/// </para>
///
/// <para>
/// "Each combat" vs. "this turn" timing distinction:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>each combat</b> — permanent grant; no duration is needed in the AST
///     because the permission is unconditional for as long as the effect source
///     is in play (the common printed form: Foriysian Brigade, Night Market
///     Guard, Ghastbark Twins, etc.).
///   </description></item>
///   <item><description>
///     <b>this turn</b> — temporary grant, modelled via
///     <see cref="IDurativeEffect.Duration"/> set to a
///     <c>UntilEndOfTurnDuration</c> (Anurid Swarmsnapper activated, Mounted
///     Archers, etc.).
///   </description></item>
/// </list>
/// <para>
/// When <see cref="Target"/> is null, the permission applies to the static
/// ability's controlling object (the printed card itself), e.g. "This creature
/// can block an additional creature each combat." When set, it names a distinct
/// object, e.g. <c>EnchantedOrEquipped</c> for Equipment/Aura bodies such as
/// Echo Circlet ("Equipped creature can block an additional creature each
/// combat") or a controller-scoped reference for global lines such as
/// "Each creature you control can block an additional creature each combat."
/// Mirrors <see cref="CantBlockEffect.Target"/>.
/// </para>
/// </remarks>
[OracleEffect("blockAdditional")]
public sealed record BlockAdditionalEffect : Effect
{
  /// <summary>
  /// When <c>true</c>, the oracle text grants permission to block any number
  /// of creatures ("This creature can block any number of creatures." —
  /// Guard Gomazoa pattern). When <c>false</c> (default), the text grants
  /// permission to block one additional creature each combat. Omitted from
  /// JSON when false so existing fixtures require no changes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool IsUnlimited { get; init; }

  /// <summary>
  /// The object the permission applies to. Null means the static ability's
  /// controlling object (the printed card itself); set for Aura/Equipment
  /// bodies and global grants targeting other objects.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}
