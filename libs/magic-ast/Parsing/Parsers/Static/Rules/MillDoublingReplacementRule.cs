namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// Mill-doubling replacement effect (Bruvac the Grandiloquent):
/// "If an opponent would mill one or more cards, they mill twice that many cards instead."
///
/// CR 614.1: replacement effects apply continuously as events happen and watch for a
/// particular event — "If [event] would happen, [modified event] instead" is a
/// replacement, NOT a triggered ability. CR 701.17a: to mill a number of cards is to
/// put that many cards from the top of a library into a graveyard.
///
/// Structure mirrors <see cref="TokenDoublingReplacementRule"/>/<see cref="CounterDoublingReplacementRule"/>:
/// the replaced event is a <c>MillEvent</c> by an opponent ("one or more" → MinimumQuantity 1),
/// and the doubling is a structured <c>ReplacementModifier{ Type: "double" }</c> ("twice
/// that many"), not a free-text description. The trailing mill reminder (CR 207.2) carries
/// no rules meaning and is preserved on <see cref="Ability.Reminder"/>.
/// </summary>
[StaticRule(Priority = 975)]
public sealed class MillDoublingReplacementRule : IStaticRule
{
  private static readonly Regex _millDoublingPattern = new(
    @"^\s*If\s+an\s+opponent\s+would\s+mill\s+one\s+or\s+more\s+cards,\s+they\s+mill\s+twice\s+that\s+many\s+cards\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Captures the inner text of a trailing reminder parenthetical (CR 207.2).
  private static readonly Regex _reminderPattern = new(
    @"\(\s*(?<reminder>[^)]*?)\s*\)\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_millDoublingPattern.IsMatch(body))
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderMatch = _reminderPattern.Match(clause.RawText);
    if (reminderMatch.Success)
    {
      reminder = new Parenthetical { Text = reminderMatch.Groups["reminder"].Value };
    }

    return
    [
      new StaticAbility
      {
        Reminder = reminder,
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.MillEvent
          {
            MinimumQuantity = 1,
            Controller = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
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
