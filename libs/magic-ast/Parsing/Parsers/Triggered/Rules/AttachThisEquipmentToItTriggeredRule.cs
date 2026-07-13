namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "[you may] attach this [Equipment|Aura|Fortification] to it" — the ETB-triggered
/// auto-attach shape where the object named by the trigger condition (Rule 603.6a —
/// a "Whenever a [X] enters" ability) IS the attachment target itself, referenced
/// back as "it" rather than picked via a fresh target. Rule 701.3: "To take an Aura,
/// Equipment, or Fortification from where it currently is and put it onto a
/// specified object or player."
///
/// <para>
/// Paradigm card: Cloak and Dagger — "Whenever a Rogue creature enters, you may
/// attach this Equipment to it." The "it" is a back-reference (ADR 0004) to the
/// permanent named by the trigger's own <see cref="TriggerCondition.Filter"/>, so
/// the attach target is <see cref="ObjectReferenceKind.It"/> carrying no filter of
/// its own — mirroring the same reference the compound Animate Dead / Nim Deathmantle
/// rules already build for their "…and attach this [Aura|Equipment] to it" tails.
/// </para>
///
/// <para>
/// Distinct from <see cref="AttachTriggeredRule"/>, which handles "attach it to
/// target [filter] you control" — there the object being attached TO is a fresh
/// target, not the triggering object.
/// </para>
///
/// <para>
/// Tightly anchored (^…$) to the bare "[you may] attach this &lt;type&gt; to it"
/// sentence so this rule only claims the whole effect fragment when there is
/// nothing else in it — the higher-priority compound rules
/// (<see cref="AuraLosesGainsEnchantReturnAndAttachRule"/> at 95,
/// <see cref="YouMayPayReturnAndAttachEquipmentRule"/> at 81) already own the
/// longer "…and attach this [Aura|Equipment] to it" tails that appear inside a
/// multi-effect sentence, and this rule's anchoring means it can never steal a
/// match from within those compounds even if dispatch order changed.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class AttachThisEquipmentToItTriggeredRule : ITriggeredRule
{
  // "[you may ]attach this <Equipment|Aura|Fortification> to it[.]" — the WHOLE
  // effect fragment, nothing more.
  private static readonly Regex _pattern = new(
    @"^(?:you\s+may\s+)?attach\s+this\s+(?:Equipment|Aura|Fortification)\s+to\s+it\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    var isOptional = trimmed.StartsWith("you may", StringComparison.OrdinalIgnoreCase);

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(
      new AttachEffect { Target = ObjectReference.It() },
      isOptional
    );
    return true;
  }
}
