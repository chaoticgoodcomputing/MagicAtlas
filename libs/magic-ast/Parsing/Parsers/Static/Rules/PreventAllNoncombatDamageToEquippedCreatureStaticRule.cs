namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// "Prevent all [noncombat] damage that would be dealt to equipped creature."
/// (Magebane Armor) — a permanent, always-on damage-prevention shield scoped
/// to the Equipment's attached creature, optionally qualified to a single
/// damage type ("noncombat").
///
/// <para>
/// Sibling of <see cref="PreventAllDamageToCreaturesYouControlStaticRule"/> and
/// <see cref="PreventAllDamageToSelfByCreaturesStaticRule"/> (same "prevent all
/// damage that would be dealt to [subject]" shield shape); this variant's
/// subject is the attached permanent (CR 702.6 — Equip) rather than "this
/// creature" or "creatures you control", and it optionally narrows the
/// protected damage class ("noncombat") via <see cref="PreventDamageEffect.NoncombatOnly"/>
/// (mirroring the existing <see cref="PreventDamageEffect.CombatOnly"/> flag
/// for the opposite class).
/// </para>
///
/// <para>
/// CR 615.1 (cited verbatim): "Some continuous effects are prevention effects.
/// ... Such effects watch for a damage event that would happen and completely
/// or partially prevent the damage that would be dealt." CR 615.1a: "Effects
/// that use the word 'prevent' are prevention effects." CR 604.2 (cited
/// verbatim): "Static abilities create continuous effects, some of which are
/// prevention effects or replacement effects. These effects are active as
/// long as the permanent with the ability remains on the battlefield and has
/// the ability..."
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full oracle sentence is matched exactly so this rule
/// cannot fire on a substring of a broader clause, and the required "equipped
/// creature" subject keeps it from claiming the "this/enchanted creature" or
/// "creatures you control" siblings' distinct wording.
/// </para>
/// </summary>
[StaticRule(Priority = 964)]
public sealed class PreventAllNoncombatDamageToEquippedCreatureStaticRule : IStaticRule
{
  // "Prevent all [noncombat] damage that would be dealt to equipped creature."
  private static readonly Regex _pattern = new(
    @"^\s*Prevent\s+all\s+(?<noncombat>noncombat\s+)?damage\s+that\s+would\s+be\s+dealt\s+to\s+equipped\s+creature\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(body);
    if (!match.Success)
    {
      return null;
    }

    var isNoncombat = match.Groups["noncombat"].Success;

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new PreventDamageEffect
          {
            All = true,
            NoncombatOnly = isNoncombat,
            Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          },
        ],
      },
    ];
  }
}
