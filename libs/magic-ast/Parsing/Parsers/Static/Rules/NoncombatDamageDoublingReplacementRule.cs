namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// Noncombat-damage-doubling replacement effect (Solphim, Mayhem Dominus):
/// "If a source you control would deal noncombat damage to an opponent or a
/// permanent an opponent controls, it deals double that damage to that player
/// or permanent instead."
///
/// CR 614.1a: effects using "instead" are replacement effects. This effect
/// watches for a noncombat-damage event from a source the controller owns
/// and replaces the damage quantity with double.
///
/// Structure mirrors <see cref="MillDoublingReplacementRule"/>: the replaced
/// event is a <c>DamageEvent</c> with Source controlled by You and
/// DamageType "noncombat", directed at an opponent-controlled recipient,
/// and the doubling is a structured <c>ReplacementModifier{ Type: "double" }</c>
/// ("double that damage"), not a free-text description.
///
/// ANCHORED (^...$): the phrase "noncombat damage to an opponent or a permanent
/// an opponent controls … double that damage … instead" is highly specific and
/// does not appear as a substring of any other standard oracle-text clause.
/// </summary>
[StaticRule(Priority = 976)]
public sealed class NoncombatDamageDoublingReplacementRule : IStaticRule
{
  // Matches the Solphim family: "If a source you control would deal noncombat
  // damage to an opponent or a permanent an opponent controls, it deals double
  // that damage to that player or permanent instead."
  // ^...$ anchoring prevents substring matches inside longer clauses.
  private static readonly Regex _noncombatDamageDoublingPattern = new(
    @"^\s*If\s+a\s+source\s+you\s+control\s+would\s+deal\s+noncombat\s+damage\s+to\s+an\s+opponent\s+or\s+a\s+permanent\s+an\s+opponent\s+controls,\s+it\s+deals\s+double\s+that\s+damage\s+to\s+that\s+player\s+or\s+permanent\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_noncombatDamageDoublingPattern.IsMatch(body))
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
            Source = new ObjectFilter { Controller = ControllerFilter.You },
            DamageType = "noncombat",
            AffectedObjects = new ObjectFilter { Controller = ControllerFilter.Opponent },
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
