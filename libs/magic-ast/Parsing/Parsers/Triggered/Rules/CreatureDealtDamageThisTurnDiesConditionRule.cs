namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Death-watch with a damage-provenance qualifier:
/// "Whenever a creature dealt damage by [source] this turn dies, …" — Sengir Bats,
/// Sengir Vampire, Predator Ooze, Blood Cultist, Vein Drinker, Zurgo Helmsmasher,
/// Garza Zol, Madame Vastra, Rot Wolf, Abattoir Ghoul, and the rest of the family.
///
/// <para>
/// The trigger event is a <see cref="TriggerEvent.Dies"/> (CR 700.4 / 603.2 — a
/// creature is put into a graveyard from the battlefield). The qualifier "dealt
/// damage by [source] this turn" is a backward-looking provenance condition on the
/// dying creature: it is the existing <see cref="DealtDamageByPredicate"/> history
/// predicate (whose own doc-comment names exactly "a creature dealt damage by Zurgo
/// this turn"). The source that dealt the damage is the ability's own permanent —
/// written "this creature" / "this permanent", as the card's own name (CR 201.5
/// self-reference), or "equipped creature" for the Equipment in the family (Scythe
/// of the Wretched).
/// </para>
///
/// <para>
/// This rule sits ABOVE <see cref="DiesConditionRule"/> (Priority 991) because the
/// shared <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> would otherwise read
/// the "this creature" inside the provenance clause as a self-reference on the dying
/// subject (returning <c>IsSelf = true</c> and dropping the History) — wrong: the
/// subject is "a creature" (any creature), and "this creature" is the damage source,
/// not the dying object. Recognising the whole shape here keeps the subject and the
/// provenance source correctly separated.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class CreatureDealtDamageThisTurnDiesConditionRule : ITriggerConditionRule
{
  // "[a/another] creature dealt damage by <source> this turn dies".
  // <source> is captured so the provenance source can be resolved (self by "this
  // creature"/"this permanent", self-by-name, or the equipped creature).
  private static readonly Regex _pattern = new(
    @"\b(?<another>another\s+)?creature\s+dealt\s+damage\s+by\s+(?<source>.+?)\s+this\s+turn\s+dies\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Cheap guards before the regex (the dispatcher precomputes `lower`).
    if (!lower.Contains("dealt damage by") || !lower.Contains("this turn") || !lower.Contains("dies"))
    {
      return null;
    }

    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    var source = ResolveSource(match.Groups["source"].Value.Trim());

    // "another creature dealt damage by …" excludes the source itself (CR 109.5).
    var excludeSelf = match.Groups["another"].Success ? (bool?)true : null;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        ExcludeSelf = excludeSelf,
        History = new DealtDamageByPredicate
        {
          Source = source,
          Window = DamageWindow.ThisTurn,
        },
      },
    };
  }

  /// <summary>
  /// Resolves the captured damage-source phrase to an <see cref="ObjectReference"/>.
  /// "this creature" / "this permanent" and the card's own name (CR 201.5
  /// self-reference) are the source permanent itself (<see cref="ObjectReferenceKind.Self"/>);
  /// "equipped creature" is the Equipment's attached creature
  /// (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>).
  /// </summary>
  private static ObjectReference ResolveSource(string sourcePhrase)
  {
    var lower = sourcePhrase.ToLowerInvariant();

    if (lower is "this creature" or "this permanent")
    {
      return ObjectReference.Self();
    }

    if (lower is "equipped creature")
    {
      return new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };
    }

    // Otherwise the source is the card naming itself (CR 201.5) — e.g. "dealt damage
    // by Zurgo this turn". A self-reference, the same resolution as "this creature".
    return ObjectReference.Self();
  }
}
