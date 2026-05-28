namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you cast an Aura spell" (Kor Spiritdancer family). Aura is an
/// enchantment <em>subtype</em> (Rule 205.3h), so the cast filter is recorded
/// as <c>Subtypes = ["Aura"]</c> rather than a card-type characteristic — the
/// distinction that <see cref="SpellCastConditionRule"/> cannot express (its
/// per-word loop matches card types like "creature"/"noncreature", and it has
/// no subtype channel). Sits above <see cref="SpellCastConditionRule"/>
/// (priority 998) so the Aura subtype qualifier is captured instead of being
/// silently dropped by the generic spell-cast recognizer.
/// </summary>
[TriggerConditionRule(Priority = 1001)]
public sealed class CastAuraSpellConditionRule : ITriggerConditionRule
{
  // "[you] cast(s) an Aura spell" — caster + Aura-subtype spell.
  private static readonly Regex _pattern = new(
    @"\b(?<caster>you|an?\s+opponent|an?\s+player)\s+casts?\s+an?\s+Aura\s+spell\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("aura") || !lower.Contains("spell"))
    {
      return null;
    }

    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    var caster = match.Groups["caster"].Value.ToLowerInvariant();
    var controller = caster.Contains("opponent")
      ? ControllerFilter.Opponent
      : (caster.Contains("you") ? ControllerFilter.You : (ControllerFilter?)null);

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.SpellCast,
      Filter = new ObjectFilter
      {
        CardTypes = ["spell"],
        Subtypes = ["Aura"],
        Controller = controller,
      },
    };
  }
}
