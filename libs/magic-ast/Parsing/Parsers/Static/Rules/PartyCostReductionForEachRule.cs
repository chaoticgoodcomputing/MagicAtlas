namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "This spell costs {N} less to cast for each creature in your party."
/// (Deadly Alliance, ZNC) — the party cost-reduction ability introduced by the
/// party mechanic.
///
/// CR 700.8: "Some cards refer to a player's party. A player's party consists of
/// up to one Cleric creature that player controls, up to one Rogue creature they
/// control, up to one Warrior creature they control, and up to one Wizard
/// creature they control." The "up to one each of the four classes, capped at
/// four" counting is engine territory (descriptive-not-engine doctrine); the AST
/// records only the filter phrase "creature in your party" via
/// <see cref="ObjectFilter.InParty"/>.
///
/// The printed line always carries a trailing reminder-text parenthetical
/// ("(Your party consists of up to one each of Cleric, Rogue, Warrior, and
/// Wizard.)") per Rule 207.2; it is stripped before matching so the anchored
/// end-of-string pattern still matches.
///
/// Priority 989 — fires before the generic <see cref="CostReductionForEachRule"/>
/// (priority 987), which matches clause.RawText directly (without stripping
/// reminder text) and would otherwise capture the whole reminder parenthetical
/// into its filter-phrase group as an unrecognised phrase.
/// </summary>
[StaticRule(Priority = 989)]
public sealed class PartyCostReductionForEachRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Strip trailing reminder text (Rule 207.2) before matching the end-anchored
    // pattern — see BareKeywordPairGrantRule for the identical idiom.
    var text = StaticRuleHelpers.StripReminderText(clause.RawText);

    var match = _partyCostReductionPattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Resource.CostReductionEffect
          {
            Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
            PerObject = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
              InParty = true,
            },
          },
        ],
      },
    ];
  }

  // "This spell costs {N} less to cast for each creature in your party."
  // Anchored (^…$) after reminder-text stripping.
  private static readonly Regex _partyCostReductionPattern = new(
    @"^\s*This\s+spell\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\s+for\s+each\s+creature\s+in\s+your\s+party\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
