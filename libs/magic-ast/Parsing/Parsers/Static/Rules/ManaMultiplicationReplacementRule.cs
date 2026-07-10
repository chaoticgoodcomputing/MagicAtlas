namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// Mana-multiplication replacement effect (Nyxbloom Ancient):
/// "If you tap a permanent for mana, it produces three times as much of that mana instead."
/// Also covers the doubling sibling Mana Reflection: "If you tap a permanent for mana, it
/// produces twice as much of that mana instead."
///
/// CR 106.12: "To 'tap [a permanent] for mana' is to activate a mana ability of that
/// permanent that includes the {T} symbol in its activation cost." CR 605 governs mana
/// abilities; CR 614.1: replacement effects apply continuously as events happen and watch
/// for a particular event — "If [event], [modified event] instead" is a replacement, NOT a
/// triggered ability. The replaced event is the mana that permanent would produce; the
/// "twice/three times as much … instead" clause scales that produced amount.
///
/// Structure mirrors <see cref="MillDoublingReplacementRule"/>: the replaced event is a
/// structured <c>ManaProductionEvent</c> (the tapped permanent carried on
/// <c>AffectedObjects</c> as <c>CardTypes: ["permanent"], Controller: You</c>, mirroring the
/// identical "you tap a permanent for mana" phrase modeled by
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.TapForManaConditionRule"/>), and the
/// multiplication is a structured <c>ReplacementModifier{ Type: "double" | "triple" }</c>,
/// not a free-text description. <c>OriginalEventOccurs = false</c>: the mana produced is
/// replaced by the multiplied amount ("instead"), not added on top.
///
/// ANCHORED (^…$): the whole clause is matched, so it can neither be consumed as a substring
/// of a longer clause nor claim a substring of a more specific sibling.
/// </summary>
[StaticRule(Priority = 975)]
public sealed class ManaMultiplicationReplacementRule : IStaticRule
{
  // "If you tap a permanent for mana, it produces {twice|three times} as much of that mana
  // instead." — the multiplier word maps to a structured ReplacementModifier.Type.
  private static readonly Regex _manaMultiplicationPattern = new(
    @"^\s*If\s+you\s+tap\s+a\s+permanent\s+for\s+mana,\s+it\s+produces\s+(?<mult>twice|three\s+times)\s+as\s+much\s+of\s+that\s+mana\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _manaMultiplicationPattern.Match(body);
    if (!match.Success)
    {
      return null;
    }

    // "twice" → double; "three times" → triple.
    var multiplier = Regex.Replace(match.Groups["mult"].Value, @"\s+", " ").Trim();
    var modifierType = multiplier.Equals("twice", System.StringComparison.OrdinalIgnoreCase)
      ? "double"
      : "triple";

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.ManaProductionEvent
          {
            AffectedObjects = new ObjectFilter
            {
              CardTypes = ["permanent"],
              Controller = ControllerFilter.You,
            },
          },
          OriginalEventOccurs = false,
          Modifier = new MagicAST.AST.Effects.Replacement.ReplacementModifier
          {
            Type = modifierType,
          },
        }],
      },
    ];
  }
}
