namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you cast an Eldrazi spell" / "Whenever you cast a Zombie spell" —
/// the subtype-qualified spell-cast condition family. The subtype is a creature
/// subtype (Rule 205.3m), NOT a card type, so it lands on
/// <see cref="ObjectFilter.Subtypes"/> while the card type remains <c>"spell"</c>.
///
/// <para>
/// Sits above <see cref="SpellCastConditionRule"/> (priority 998) so the subtype
/// qualifier is captured first. <see cref="CastAuraSpellConditionRule"/>
/// (priority 1001) handles the Aura enchantment-subtype specifically; this rule
/// handles all other subtypes and sits between them at priority 1000.
/// </para>
///
/// <para>CR 205.3m: subtypes appear after the em-dash on the type line and are
/// distinct from the broad card types. "An Eldrazi spell" is a spell whose subtype
/// includes "Eldrazi".</para>
/// </summary>
[TriggerConditionRule(Priority = 1000)]
public sealed class SubtypeSpellCastConditionRule : ITriggerConditionRule
{
  // "[you|opponent|player] cast(s) an? [Subtype] spell" — subtype is a single
  // properly-capitalised word (Eldrazi, Zombie, Wizard, Dragon, …).
  private static readonly Regex _pattern = new(
    @"\b(?<caster>you|an?\s+opponent|an?\s+player)\s+casts?\s+an?\s+(?<subtype>[A-Z][a-zA-Z]+)\s+spell\b",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick guard: must contain "spell" and "cast"
    if (!lower.Contains("spell") || !lower.Contains("cast"))
    {
      return null;
    }

    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    var caster = match.Groups["caster"].Value.ToLowerInvariant();
    var subtype = match.Groups["subtype"].Value;

    // Skip if the "subtype" word is actually a card type handled by SpellCastConditionRule
    // (creature, instant, sorcery, artifact, enchantment, permanent).
    if (subtype is "Creature" or "Instant" or "Sorcery" or "Artifact" or "Enchantment" or "Permanent")
    {
      return null;
    }

    // Skip "Aura" — handled by CastAuraSpellConditionRule at priority 1001.
    if (subtype == "Aura")
    {
      return null;
    }

    var controller = caster.Contains("opponent")
      ? ControllerFilter.Opponent
      : (caster.StartsWith("you") ? ControllerFilter.You : (ControllerFilter?)null);

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.SpellCast,
      Filter = new ObjectFilter
      {
        CardTypes = ["spell"],
        Subtypes = [subtype],
        Controller = controller,
      },
    };
  }
}
