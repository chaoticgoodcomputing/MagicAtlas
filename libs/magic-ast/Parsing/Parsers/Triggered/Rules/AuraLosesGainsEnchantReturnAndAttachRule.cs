namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Recognises the multi-effect sentence produced by Animate Dead's ETB resolution:
/// <c>it loses "enchant creature card in a graveyard" and gains "enchant creature
/// put onto the battlefield with this Aura." Return enchanted creature card to the
/// battlefield under your control and attach this Aura to it.</c>
///
/// <para>
/// This sentence is the first sentence in the complex ETB ability's effect body.
/// It produces four effects in sequence:
/// <list type="number">
///   <item><see cref="LoseAbilityEffect"/> — "it" (the Aura, <see cref="ObjectReferenceKind.Self"/>)
///   loses the quoted enchant ability text (captured on the free-text
///   <c>AbilityText</c> field, which is a typed residual per ADR 0001).</item>
///   <item><see cref="GainAbilityEffect"/> — "it" gains a new static
///   <see cref="EnchantRestrictionEffect"/> targeting any creature (the revised
///   enchant restriction once the Aura is on the battlefield).</item>
///   <item><see cref="ReturnToBattlefieldEffect"/> — returns the enchanted creature
///   card (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>) to the battlefield
///   under the controller's control.</item>
///   <item><see cref="AttachEffect"/> — attaches this Aura to "it"
///   (<see cref="ObjectReferenceKind.It"/> — the creature just returned).</item>
/// </list>
/// </para>
///
/// <para>
/// CR 702.5 (Enchant — Aura ability); CR 303.4 (Aura attachment to a permanent);
/// CR 400.6 (entering under a specific player's control); CR 701.3 (Attach).
/// CR 614.1 (replacement effects for the new enchant restriction on entry).
/// </para>
///
/// <para>
/// The overall pattern: <c>^it loses "(?&lt;lost&gt;.+?)" and gains "enchant creature
/// put onto the battlefield with this (?:Aura|enchantment)\.?" Return enchanted
/// creature card to the battlefield under your control and attach this Aura to it\.?$</c>
/// This is narrow enough to be safely anchored — the combination of "loses" +
/// quoted text + "gains" + "enchant creature put onto the battlefield with this
/// Aura" is unique to the Animate Dead / Dance of the Dead family.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class AuraLosesGainsEnchantReturnAndAttachRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^it\s+loses\s+""(?<lost>[^""]+)""\s+and\s+gains\s+""enchant\s+creature\s+put\s+onto\s+the\s+battlefield\s+with\s+this\s+(?:Aura|enchantment)\.?""\s+Return\s+enchanted\s+creature\s+card\s+to\s+the\s+battlefield\s+under\s+your\s+control\s+and\s+attach\s+this\s+Aura\s+to\s+it\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      // Also try matching the full text with trailing period
      m = _pattern.Match(text.Trim());
      if (!m.Success)
      {
        return false;
      }
    }

    var lostAbilityText = m.Groups["lost"].Value.Trim();

    // Effect 1: it loses the named enchant ability (LoseAbilityEffect with AbilityText residual)
    var loseAbility = new LoseAbilityEffect
    {
      Target = ObjectReference.Self(),
      AbilityText = lostAbilityText,
    };

    // Effect 2: it gains "enchant creature" (the new battlefield enchant restriction)
    // CR 702.5a: the replacement enchant ability is "enchant creature" — the Aura can
    // now enchant a creature on the battlefield (the one it just returned via Effect 3).
    var gainAbility = new GainAbilityEffect
    {
      Target = ObjectReference.Self(),
      GainedAbility = new StaticAbility
      {
        KeywordSource = KeywordAbility.Enchant,
        Effects =
        [
          new EnchantRestrictionEffect
          {
            LegalTargets = new ObjectFilter { CardTypes = ["creature"] },
          },
        ],
      },
    };

    // Effect 3: return enchanted creature card to the battlefield under your control.
    // "enchanted creature card" refers to what this Aura is currently enchanting
    // (a creature card in a graveyard at this point — the Aura's previous enchant target).
    // CR 400.6: the creature enters under the Aura controller's control.
    var returnCreature = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      UnderControl = ObjectReference.You(),
    };

    // Effect 4: attach this Aura to it (the creature just returned by Effect 3).
    // "it" is the anaphoric reference to the creature returned above.
    // CR 701.3: to attach is to take the Aura from where it is and put it onto the object.
    var attachAura = new AttachEffect
    {
      Target = ObjectReference.It(),
    };

    // Return the four effects as a CompositeEffect — the sentence has exactly
    // four atomic effects sharing one resolution sequence.
    effect = new MagicAST.AST.Effects.Core.CompositeEffect
    {
      Effects = [loseAbility, gainAbility, returnCreature, attachAura],
    };
    return true;
  }
}
