namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Handles the "You have [keyword]." static ability shape, where the permanent
/// grants a keyword to its controller (a player, not a permanent).
///
/// CR 702.11a: "Hexproof is a static ability."
///
/// Oracle examples:
///   "You have hexproof." (Spirit of the Hearth)
///   "You have shroud." (Teferi's Protection effect)
///
/// The subject "You" maps to <see cref="ObjectReferenceKind.You"/> — the controller
/// player, NOT the permanent itself (not Self). The granted ability is embedded as a
/// full <see cref="StaticAbility"/> node via <see cref="GainAbilityEffect"/>.
/// Reminder text (the parenthetical) is stripped before matching; the caller places
/// it on the <see cref="Ability.Reminder"/> field.
/// </summary>
[StaticRule(Priority = 80)]
public sealed class YouHaveKeywordRule : IStaticRule
{
  // "You have <keyword>." — optionally followed by a parenthetical reminder.
  // The keyword capture group is greedy-but-minimal so it stops before any
  // trailing period or whitespace.
  private static readonly Regex _youHaveKeywordPattern = new(
    @"^\s*You\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Strip trailing reminder parenthetical before matching so that
    // "You have hexproof. (You can't be the target...)" still fires.
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);

    var match = _youHaveKeywordPattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var kw = match.Groups["kw"].Value.Trim().ToLowerInvariant();
    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedAbility is null)
    {
      return null;
    }

    // Extract reminder text (if any) from the original clause text for the
    // outer ability's Reminder field. The inner GainedAbility node carries
    // no reminder — reminders annotate the enclosing ability line.
    var reminderMatch = Regex.Match(clause.RawText, @"(\([^)]+\))\s*$");
    Parenthetical? reminder = reminderMatch.Success
      ? new Parenthetical { Text = reminderMatch.Groups[1].Value }
      : null;

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilityEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.You },
          GainedAbility = grantedAbility,
        }],
        Reminder = reminder,
      },
    ];
  }
}
