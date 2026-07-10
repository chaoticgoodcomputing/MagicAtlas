namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// "Prevent all damage that would be dealt to enchanted creature by sources of
/// the chosen color." — the Prismatic Ward shield: an always-on damage-prevention
/// static ability that blocks all damage from sources of whichever color was
/// picked by the paired "As this Aura enters, choose a color." replacement
/// ability (CR 614.12 — see <see cref="ChooseColorOnEntryRule"/>).
///
/// CR 615.1 (cited verbatim from rules-structure.json): "Some continuous effects
/// are prevention effects. Like replacement effects (see rule 614), prevention
/// effects apply continuously as events happen-they aren't locked in ahead of
/// time. Such effects watch for a damage event that would happen and completely
/// or partially prevent the damage that would be dealt. They act like \"shields\"
/// around whatever they're affecting."
/// CR 615.1a: "Effects that use the word \"prevent\" are prevention effects."
/// CR 615.2: "Many prevention effects apply to damage from a source."
///
/// Structure: mirrors <see cref="PreventAllDamageToSelfByCreaturesStaticRule"/> —
/// a bare <see cref="PreventDamageEffect"/> (no leading "If … would", so not
/// wrapped in a <c>ReplacementEffect</c>) with <c>All=true</c>. <c>Target</c> is
/// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> ("enchanted creature").
/// <c>Source</c> is an unbound <see cref="ObjectReferenceKind.Each"/> reference
/// whose <see cref="ObjectFilter.ChosenCharacteristic"/> is
/// <see cref="ChosenCharacteristicKind.Color"/> — the structured consumer side of
/// the CR 607 linked "chosen color" reference, rather than a literal
/// <c>Colors</c> value (the color isn't known until the Aura's entry ability
/// resolves). No <c>Duration</c>: an always-on static ability that persists for
/// as long as the permanent remains on the battlefield (CR 604.2).
///
/// ANCHORED (^...$): the full-line template is matched in its entirety, and the
/// target noun is pinned to "enchanted creature" specifically, so this rule
/// cannot fire as a substring of a broader clause and does not collide with the
/// sibling "Prevent all damage that would be dealt to you by sources of the
/// chosen color." template (Charm School — a player-targeted variant with a
/// different `Target`, left unhandled by this rule).
/// </summary>
[StaticRule(Priority = 962)]
public sealed class PreventAllDamageToEnchantedCreatureByChosenColorSourceStaticRule : IStaticRule
{
  // "Prevent all damage that would be dealt to enchanted creature by sources of
  // the chosen color."
  // ANCHORED (^...$) to prevent substring matching inside broader clauses.
  private static readonly Regex _pattern = new(
    @"^\s*Prevent\s+all\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+enchanted\s+creature\s+by\s+sources\s+of\s+the\s+chosen\s+color\.?\s*$",
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
            Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            Source = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { ChosenCharacteristic = ChosenCharacteristicKind.Color },
            },
          },
        ],
      },
    ];
  }
}
