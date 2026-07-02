namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Battle;
using MagicAST.Parsing;

/// <summary>
/// Standalone Siege type-reminder line, identical on every "Battle — Siege"
/// front face (the March-of-the-Machine Invasions):
/// "(As a Siege enters, choose an opponent to protect it. You and others can
/// attack it. When it's defeated, exile it, then cast it transformed.)".
///
/// <para>
/// Emits a <see cref="StaticAbility"/> carrying a field-less
/// <see cref="SiegeEffect"/> marker, with the verbatim parenthetical preserved
/// on <c>Reminder</c> via the existing reminder mechanism (CR 207.2 — italic
/// text with no game function). This honors no-silent-drop: the recognized
/// reminder is captured, not discarded, and the marker gives it a structured
/// host that anchors the Siege mechanic on the card's rules text.
/// </para>
///
/// <para>
/// MAST describes, it does not execute. The defeat → exile → cast-transformed
/// life cycle (CR 310.7 — a battle with defense 0 leaves the battlefield as a
/// state-based action) and the casting of a battle as a spell (CR 310.1) are
/// engine territory, summarized in the captured reminder rather than modeled.
/// The Siege's defense value is absent from the MAST <c>Input</c>, so it is out
/// of scope.
/// </para>
/// </summary>
[StaticRule]
public sealed class SiegeReminderStaticRule : IStaticRule
{
  // Whole-clause match: the entire reminder parenthetical. Captured verbatim
  // (including the surrounding parentheses) into Reminder.Text, matching the
  // convention used by the other reminder-bearing static rules (Blitz, Dash,
  // Modular, …) whose Parenthetical.Text retains its parentheses.
  private static readonly Regex Pattern = new(
    @"^\s*(?<reminder>\(As a Siege enters,.*\))\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new SiegeEffect()],
        Reminder = new Parenthetical { Text = match.Groups["reminder"].Value.Trim() },
      },
    ];
  }
}
