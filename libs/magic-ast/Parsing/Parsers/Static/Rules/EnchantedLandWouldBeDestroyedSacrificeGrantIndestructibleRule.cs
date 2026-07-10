namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "If enchanted [type] would be destroyed, instead sacrifice this [permanent
/// type] and that [type] gains indestructible until end of turn." (Crackling
/// Emergence) — a destruction replacement effect (CR 614.1) whose alternative is
/// a COMPOUND action: the Aura sacrifices itself AND the would-be-destroyed
/// permanent gains indestructible for the rest of the turn (so a later
/// destruction attempt this turn also fails, CR 702.12b).
///
/// <para>
/// Sibling of <see cref="SelfRegenerateOnDestructionReplacementRule"/> ("If this
/// creature would be destroyed, regenerate it.") — both are unconditional
/// destruction replacements built on the same <see cref="ReplacementEffect"/> +
/// <see cref="DestructionEvent"/> primitives (<c>OriginalEventOccurs = false</c>,
/// mirroring how <c>SelfWouldBePutIntoGraveyardExileInsteadRule</c> models "exile
/// it instead"). Differs from that sibling in three ways: (1) the destroyed
/// object is the enchanted permanent, not the Aura itself, so
/// <see cref="ReplacementEvent.AffectedObjects"/> carries the "enchanted [type]"
/// filter (<c>IsEnchanted = true</c>, matching the trailing-retention
/// <c>CardTypes</c> convention used by
/// <see cref="EnchantedIsCreatureWithKeywordsStillTypeRule"/>) rather than
/// <c>IsSelf = true</c>; (2) the replacement action is a
/// <see cref="CompositeEffect"/> of two effects rather than one; (3) the second
/// effect is a duration-scoped keyword grant rather than a keyword action.
/// </para>
///
/// <para>
/// <b>"sacrifice this Aura"</b> → <see cref="SacrificeEffect"/> with
/// <c>Target = Self</c> (CR 700.4: "this [object]" in an object's own text is that
/// object, here the Aura naming itself by its own type line).
/// </para>
///
/// <para>
/// <b>"that land gains indestructible until end of turn"</b> → the anaphoric
/// "that [type]" back-references the "enchanted [type]" object just named by the
/// event, the same back-reference the existing corpus already encodes with
/// <see cref="ObjectReferenceKind.It"/> (e.g. Vastwood Zendikon's "return that
/// card to its owner's hand" → <c>Target: {Kind:"It"}</c>). The grant is a
/// <see cref="GainAbilityEffect"/> whose <see cref="GainAbilityEffect.GainedAbility"/>
/// is built via <see cref="StaticRuleHelpers.MapKeywordToStaticAbility"/> (the
/// same indestructible keyword-ability node used elsewhere), scoped by a
/// <see cref="MagicAST.AST.Effects.UntilTimeDuration"/> (CR "until end of turn").
/// </para>
///
/// <para>
/// Anchored (^…$) to the exact "If enchanted [type] would be destroyed, instead
/// sacrifice this [permanent-type] and that [type] gains indestructible until end
/// of turn" shape so it cannot collide with Totem/Umbra armor's differently-worded
/// "...instead remove all damage from it and destroy this Aura instead." replacement
/// (<see cref="MagicAST.Keywords.Definitions.TotemArmorKeyword"/>) or with
/// <see cref="SelfRegenerateOnDestructionReplacementRule"/>'s "regenerate it" shape.
/// </para>
///
/// Rule 614.1 (replacement effects — "If [event] would happen, [alternative]
/// instead"); Rule 701.19 is NOT invoked here (no "regenerate" keyword action, an
/// explicit sacrifice + grant instead); Rule 702.12b (indestructible: "can't be
/// destroyed"); Rule 111.7 / 303.4c (Aura's own "this Aura" self-reference).
/// </summary>
[StaticRule]
public sealed class EnchantedLandWouldBeDestroyedSacrificeGrantIndestructibleRule : IStaticRule
{
  // "If enchanted land would be destroyed, instead sacrifice this Aura and that
  // land gains indestructible until end of turn." <type> is the enchanted
  // permanent's card type (also the "that <type>" back-reference and the
  // sacrificed <permanent> noun, e.g. "Aura"/"Equipment").
  private static readonly Regex _pattern = new(
    @"^\s*If\s+enchanted\s+(?<type>artifact|land|creature|permanent|enchantment|planeswalker)\s+would\s+be\s+destroyed,\s+instead\s+sacrifice\s+this\s+(?<permanent>[A-Za-z]+)\s+and\s+that\s+\k<type>\s+gains\s+indestructible\s+until\s+end\s+of\s+turn\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var enchantedType = match.Groups["type"].Value.ToLowerInvariant();

    var indestructible = StaticRuleHelpers.MapKeywordToStaticAbility("indestructible");
    if (indestructible is null)
    {
      return null;
    }

    var replacement = new StaticAbility
    {
      Effects =
      [
        new ReplacementEffect
        {
          Event = new DestructionEvent
          {
            AffectedObjects = new ObjectFilter { CardTypes = [enchantedType], IsEnchanted = true },
          },
          OriginalEventOccurs = false,
          Replacement = new CompositeEffect
          {
            Effects =
            [
              new SacrificeEffect { Target = ObjectReference.Self() },
              new GainAbilityEffect
              {
                Target = ObjectReference.It(),
                GainedAbility = indestructible,
                Duration = UntilTimeDuration.EndOfTurn,
              },
            ],
          },
        },
      ],
    };

    return [replacement];
  }
}
