namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Skip your draw step." — a continuous static replacement effect (Symbiotic
/// Deployment). CR 614.10 (verbatim): "An effect that causes a player to skip an
/// event, step, phase, or turn is a replacement effect. \"Skip [something]\" is the
/// same as \"Instead of doing [something], do nothing.\" Once a step, phase, or
/// turn…" — so "Skip your draw step" is modeled as a <see cref="ReplacementEffect"/>
/// over the typed draw step (<see cref="TurnPartEvent"/> with
/// <see cref="TurnPart.Draw"/>), with <c>OriginalEventOccurs = false</c> and no
/// <c>Replacement</c> ("do nothing"). CR 500.10 (turn-structure context) frames the
/// draw step as the skipped part of the turn. It is a static replacement, not an
/// activated or triggered ability.
/// </summary>
/// <remarks>
/// Anchored to the whole clause so it cannot swallow a more specific sibling that
/// merely contains this surface phrase (e.g. a skip clause paired with a follow-on
/// instruction). The "your" qualifier is recorded as
/// <see cref="ControllerFilter.You"/> on the event's <see cref="TurnPartEvent.Whose"/>.
/// </remarks>
[StaticRule]
public sealed class SkipDrawStepRule : IStaticRule
{
  private static readonly Regex _skipDrawStepPattern = new(
    @"^\s*Skip\s+your\s+draw\s+step\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_skipDrawStepPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ReplacementEffect
          {
            Event = new TurnPartEvent
            {
              Part = TurnPart.Draw,
              Whose = ControllerFilter.You,
            },
            OriginalEventOccurs = false,
          },
        ],
      },
    ];
  }
}
