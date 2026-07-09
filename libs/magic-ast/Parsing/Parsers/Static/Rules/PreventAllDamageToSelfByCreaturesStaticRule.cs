namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// "Prevent all damage that would be dealt to this creature by creatures." — a
/// permanent, always-on damage-prevention shield (e.g. Champion Lancer) that
/// blocks all damage from creature sources specifically, while leaving
/// non-creature damage (burn spells, planeswalker abilities, noncombat damage
/// from artifacts, etc.) unaffected.
///
/// CR 615.1 (cited verbatim from rules-structure.json): "Some continuous effects
/// are prevention effects. Like replacement effects (see rule 614), prevention
/// effects apply continuously as events happen-they aren't locked in ahead of
/// time. Such effects watch for a damage event that would happen and completely
/// or partially prevent the damage that would be dealt. They act like \"shields\"
/// around whatever they're affecting."
/// CR 615.1a: "Effects that use the word \"prevent\" are prevention effects."
/// CR 615.2: "Many prevention effects apply to damage from a source."
/// CR 604.2 (cited verbatim): "Static abilities create continuous effects, some
/// of which are prevention effects or replacement effects. These effects are
/// active as long as the permanent with the ability remains on the battlefield
/// and has the ability, or as long as the object with the ability remains in the
/// appropriate zone, as described in rule 113.6."
///
/// Structure: a <see cref="StaticAbility"/> whose sole effect is a direct
/// <see cref="PreventDamageEffect"/> (mirroring the blanket-statement Fog shape
/// <c>PreventAllCombatDamageThisTurnRule</c> — no leading "If … would", so this is
/// NOT wrapped in a <c>ReplacementEffect</c>) with <c>All=true</c>, <c>Target</c> =
/// <see cref="ObjectReferenceKind.Self"/> ("this creature"), and <c>Source</c> = an
/// unbound <see cref="ObjectReferenceKind.Each"/> reference filtered to
/// <c>CardTypes=["creature"]</c> ("by creatures" — any creature source, not a
/// specific named one). No <c>Duration</c>: unlike the turn-scoped spell/activated
/// prevention shields, this is an always-on static ability that persists for as
/// long as the permanent remains on the battlefield (CR 604.2), so it carries no
/// stated duration.
///
/// ANCHORED (^...$): the full-line template is matched in its entirety so this
/// cannot fire as a substring of a broader clause, and cannot claim a substring of
/// a more specific sibling (e.g. a variant naming a particular creature type
/// instead of the bare "creatures").
/// </summary>
[StaticRule(Priority = 60)]
public sealed class PreventAllDamageToSelfByCreaturesStaticRule : IStaticRule
{
  // "Prevent all damage that would be dealt to this creature by creatures."
  // ANCHORED (^...$) to prevent substring matching inside broader clauses.
  private static readonly Regex _pattern = new(
    @"^\s*Prevent\s+all\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+this\s+creature\s+by\s+creatures\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new PreventDamageEffect
          {
            All = true,
            Target = ObjectReference.Self(),
            Source = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { CardTypes = ["creature"] },
            },
          },
        ],
      },
    ];
  }
}
