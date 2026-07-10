namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// "Prevent all damage that would be dealt to you." (Glacial Chasm) — a
/// permanent, always-on damage-prevention shield scoped to the controlling
/// player, from any source (no "by [source]" qualifier). Sibling of
/// <see cref="PreventAllDamageToCreaturesYouControlStaticRule"/> (which shields
/// "creatures you control" rather than the player) and
/// <see cref="PreventAllDamageToSelfByCreaturesStaticRule"/> (which shields
/// "this creature" and only from creature sources); here the protected object
/// is the player ("you") and the shield is unconditional on source.
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
/// <see cref="PreventDamageEffect"/> with <c>All=true</c> and <c>Target</c> =
/// <see cref="ObjectReference.You"/> ("you", the player). No <c>Source</c>
/// restriction — the shield blocks damage regardless of origin. No
/// <c>Duration</c>: an always-on static ability that persists while the
/// permanent remains on the battlefield (CR 604.2), so it carries no stated
/// duration.
/// </para>
///
/// <para>
/// ANCHORED (^…$) on the full literal clause so it cannot fire as a substring of
/// a broader clause, and cannot claim a substring of the "creatures you control"
/// or "this creature by [source]" sibling shapes, which use distinct wording.
/// </para>
/// </summary>
[StaticRule(Priority = 60)]
public sealed class PreventAllDamageToYouStaticRule : IStaticRule
{
  // "Prevent all damage that would be dealt to you." ANCHORED (^...$) to prevent
  // substring matching inside broader clauses.
  private static readonly Regex _pattern = new(
    @"^\s*Prevent\s+all\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+you\.?\s*$",
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
          new PreventDamageEffect { All = true, Target = ObjectReference.You() },
        ],
      },
    ];
  }
}
