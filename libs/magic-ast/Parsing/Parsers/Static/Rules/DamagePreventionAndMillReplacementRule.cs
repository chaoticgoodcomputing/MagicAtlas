namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Damage-prevention-plus-mill replacement effect (The Mindskinner family):
/// "If a source you control would deal damage to an opponent, prevent that damage
/// and each opponent mills that many cards."
///
/// CR 615.1: some continuous effects are prevention effects; they watch for a
/// damage event that would happen and completely prevent the damage that would be
/// dealt. CR 615.1: effects that use the word "prevent" are prevention effects.
/// CR 615.5: some prevention effects also include an additional effect, which may
/// refer to the amount of damage that was prevented.
///
/// CR 701.17: to mill a number of cards is to put that many cards from the top
/// of a player's library into their graveyard.
///
/// Structure: a <c>StaticAbility</c> with a <c>ReplacementEffect</c> that:
/// <list type="bullet">
///   <item>Watches for a <c>DamageEvent</c> where the source is controlled by
///   You (<c>Source.Controller = You</c>) and the target is an opponent
///   (<c>AffectedObjects.Controller = Opponent</c>).</item>
///   <item>Replaces the damage with a <c>CompositeEffect</c> of: (1) a
///   <c>PreventDamageEffect</c> targeting the opponent (All = true, recording
///   the prevention), and (2) a <c>MillEffect</c> targeting each opponent with
///   a <c>DerivedQuantity(DamageDealt)</c> — "that many cards" = the damage
///   amount that would have been dealt (CR 615.5).</item>
/// </list>
///
/// ANCHORED (^...$): the surface phrase "a source you control would deal damage to
/// an opponent, prevent that damage and each opponent mills that many cards" is
/// highly specific and does not appear as a substring of any other standard
/// oracle-text clause.
/// </summary>
[StaticRule(Priority = 968)]
public sealed class DamagePreventionAndMillReplacementRule : IStaticRule
{
  // Matches: "If a source you control would deal damage to an opponent, prevent
  // that damage and each opponent mills that many cards."
  // ANCHORED (^...$) to prevent substring matching inside broader clauses.
  private static readonly Regex _pattern = new(
    @"^\s*If\s+a\s+source\s+you\s+control\s+would\s+deal\s+damage\s+to\s+an\s+opponent,\s*prevent\s+that\s+damage\s+and\s+each\s+opponent\s+mills\s+that\s+many\s+cards\.?\s*$",
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
          new MagicAST.AST.Effects.Replacement.ReplacementEffect
          {
            Event = new MagicAST.AST.Effects.Replacement.DamageEvent
            {
              // Source must be controlled by the ability's controller.
              Source = new ObjectFilter { Controller = ControllerFilter.You },
              // The damage target is an opponent (a player, not a permanent).
              AffectedObjects = new ObjectFilter { Controller = ControllerFilter.Opponent },
            },
            OriginalEventOccurs = false,
            Replacement = new MagicAST.AST.Effects.Core.CompositeEffect
            {
              Effects =
              [
                // "prevent that damage" — the prevention action (CR 615.1).
                // Target: the opponent who would have received the damage.
                new MagicAST.AST.Effects.Damage.PreventDamageEffect
                {
                  All = true,
                  Target = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
                },
                // "each opponent mills that many cards" — additional effect (CR 615.5).
                // "that many" = the prevented damage amount (DerivedFrom: DamageDealt).
                new MagicAST.AST.Effects.ZoneChange.MillEffect
                {
                  Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
                  Count = new DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt },
                },
              ],
            },
          },
        ],
      },
    ];
  }
}
