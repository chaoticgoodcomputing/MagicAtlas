namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// Chosen-player damage-doubling replacement effect (Sawhorn Nemesis): "If a
/// source would deal damage to the chosen player or a permanent they control,
/// it deals double that damage instead."
///
/// CR 614.12: some replacement effects, printed on a permanent, apply as long
/// as that permanent remains on the battlefield rather than only as it enters;
/// effects using "instead" are replacement effects (CR 614.1a). This effect
/// watches for a damage event directed at the player chosen by the card's
/// paired "As this creature enters, choose a player." ability (CR 607 linked
/// ability — see <see cref="ChoosePlayerOnEntryRule"/>) or a permanent that
/// player controls, and replaces the damage quantity with double.
///
/// Structure mirrors <see cref="NoncombatDamageDoublingReplacementRule"/>: the
/// replaced event is a <c>DamageEvent</c> with no <c>Source</c> restriction
/// ("a source", unrestricted — <c>Source</c> null per its "null = any source"
/// doc), directed at the chosen player (<c>AffectedObjects</c> with
/// <c>Controller = ControllerFilter.ChosenPlayer</c> and no <c>CardTypes</c>,
/// so the bare controller filter covers both the player themselves and any
/// permanent they control — exactly how <see cref="NoncombatDamageDoublingReplacementRule"/>
/// uses <c>ControllerFilter.Opponent</c> bare for "an opponent or a permanent an
/// opponent controls"), and the doubling is a structured
/// <c>ReplacementModifier{ Type: "double" }</c> ("double that damage"), not a
/// free-text description.
///
/// ANCHORED (^...$): the phrase "a source would deal damage to the chosen
/// player or a permanent they control … double that damage … instead" is
/// highly specific and does not appear as a substring of any other standard
/// oracle-text clause.
/// </summary>
[StaticRule(Priority = 977)]
public sealed class DamageToChosenPlayerDoublingReplacementRule : IStaticRule
{
  // Matches: "If a source would deal damage to the chosen player or a permanent
  // they control, it deals double that damage instead."
  // ^...$ anchoring prevents substring matches inside longer clauses.
  private static readonly Regex _damageToChosenPlayerDoublingPattern = new(
    @"^\s*If\s+a\s+source\s+would\s+deal\s+damage\s+to\s+the\s+chosen\s+player\s+or\s+a\s+permanent\s+they\s+control,\s+it\s+deals\s+double\s+that\s+damage\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_damageToChosenPlayerDoublingPattern.IsMatch(body))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.DamageEvent
          {
            AffectedObjects = new ObjectFilter { Controller = ControllerFilter.ChosenPlayer },
          },
          OriginalEventOccurs = false,
          Modifier = new MagicAST.AST.Effects.Replacement.ReplacementModifier
          {
            Type = "double",
          },
        }],
      },
    ];
  }
}
