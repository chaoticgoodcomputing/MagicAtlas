namespace MagicAST.AST.Effects.Modification;

using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Unattach this permanent." — the oracle-text instruction to remove an
/// Equipment, Aura, or Fortification from the object it is attached to,
/// returning it to the battlefield as an unattached permanent.
///
/// <para>
/// CR 702.151a (verbatim): "Reconfigure represents two activated abilities.
/// Reconfigure [cost] means "[Cost]: Attach this permanent to another target
/// creature you control. Activate only as a sorcery" and "[Cost]: Unattach
/// this permanent. Activate only if this permanent is attached to a creature
/// and only as a sorcery."
/// </para>
///
/// <para>
/// MAST records what oracle text says (the unattach instruction). The
/// activation condition "only if this permanent is attached to a creature"
/// is a runtime-state guard — no structured node for conditional activation
/// guards exists yet; it is omitted per the descriptive-not-engine doctrine
/// (engine territory, not present as a free-text field per the no-free-text rule).
/// </para>
///
/// <para>
/// Distinct from <see cref="AttachEffect"/>, which models explicit "attach"
/// instructions. This effect models the inverse action: detaching a previously
/// attached permanent from its host.
/// </para>
/// </summary>
[OracleEffect("unattach")]
public sealed record UnattachEffect : Effect
{
}
