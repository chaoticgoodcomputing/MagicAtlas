namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// "Prevent all damage that would be dealt to creatures you control." (Inner
/// Sanctum) — a permanent, always-on damage-prevention shield scoped to every
/// creature the controller controls, from any source (no "by [source]"
/// qualifier). Sibling of <see cref="PreventAllDamageToSelfByCreaturesStaticRule"/>
/// (which shields only "this creature" and only from creature sources); here the
/// protected class is a filtered set ("creatures you control") and the shield is
/// unconditional on source.
///
/// <para>
/// CR 615.1 (cited verbatim): "Some continuous effects are prevention effects.
/// Like replacement effects (see rule 614), prevention effects apply continuously
/// as events happen-they aren't locked in ahead of time. Such effects watch for a
/// damage event that would happen and completely or partially prevent the damage
/// that would be dealt. They act like \"shields\" around whatever they're
/// affecting." CR 615.1a: "Effects that use the word \"prevent\" are prevention
/// effects." CR 604.2 (cited verbatim): "Static abilities create continuous
/// effects, some of which are prevention effects or replacement effects. These
/// effects are active as long as the permanent with the ability remains on the
/// battlefield and has the ability, or as long as the object with the ability
/// remains in the appropriate zone, as described in rule 113.6."
/// </para>
///
/// <para>
/// Structure: a <see cref="StaticAbility"/> whose sole effect is a direct
/// <see cref="PreventDamageEffect"/> with <c>All=true</c> and <c>Target</c> = an
/// unbound <see cref="ObjectReferenceKind.Each"/> reference filtered to
/// <c>CardTypes=["creature"], Controller=You</c> ("creatures you control"). No
/// <c>Source</c> restriction (unlike the "by creatures" sibling) — the shield
/// blocks damage regardless of origin. No <c>Duration</c>: an always-on static
/// ability that persists while the permanent remains on the battlefield (CR
/// 604.2), so it carries no stated duration.
/// </para>
///
/// <para>
/// ANCHORED (^…$, well, <c>^…\b</c> at the classifier and full-line here): the
/// template is matched in its entirety so it cannot fire as a substring of a
/// broader clause, and cannot claim a substring of the "this creature by
/// [source]" sibling's distinct wording.
/// </para>
/// </summary>
[StaticRule(Priority = 60)]
public sealed class PreventAllDamageToCreaturesYouControlStaticRule : IStaticRule
{
  // "Prevent all damage that would be dealt to creatures you control."
  // ANCHORED (^...$) to prevent substring matching inside broader clauses.
  private static readonly Regex _pattern = new(
    @"^\s*Prevent\s+all\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+creatures\s+you\s+control\.?\s*$",
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
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
            },
          },
        ],
      },
    ];
  }
}
