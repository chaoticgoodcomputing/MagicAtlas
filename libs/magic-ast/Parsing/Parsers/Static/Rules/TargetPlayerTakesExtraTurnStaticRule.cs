namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Target player takes an extra turn after this one." — schedules an additional
/// full turn for a targeted player (rather than the controller) to be taken
/// immediately after the current turn (Walk the Aeons).
///
/// <para>
/// CR 500.7 (verbatim): "Some effects can give a player extra turns. They do this
/// by adding the turns directly after the specified turn. If a player is given
/// multiple extra turns, the extra turns are added one at a time. If multiple
/// players are given extra turns, the extra turns are added one at a time, in
/// APNAP order (see rule 101.4). The most recently created turn will be taken
/// first." MAST records the verb and the targeted-player reference; the
/// turn-ordering bookkeeping is engine territory (ADR 0001).
/// </para>
///
/// <para>
/// This clause is NOT intercepted by <see cref="AbilityClassifier"/>'s "Take
/// [an/N] extra turn(s) after this one" spell route — that route is anchored on
/// the bare imperative "Take …" opener (controller takes the turn); "Target
/// player takes …" has a distinct subject and third-person verb, so it falls
/// through to the classifier's declarative-statement default (<see
/// cref="AbilityKind.Static"/>) and is handled here instead, mirroring the
/// sibling spell-form rule <see cref="Spell.Rules.TakeExtraTurnSpellRule"/> and
/// the activated-cost form <see cref="Activated.Rules.TakeExtraTurnEffectRule"/>.
/// </para>
///
/// <para>
/// ANCHOR: pattern is anchored (^...$) so it cannot match as a substring of a
/// longer, more complex ability line.
/// </para>
/// </summary>
[StaticRule]
public sealed class TargetPlayerTakesExtraTurnStaticRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Target\s+player\s+takes\s+an\s+extra\s+turn\s+after\s+this\s+one\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new TakeExtraTurnEffect
          {
            Player = ObjectReference.Target(ObjectFilter.Player()),
          },
        ],
      },
    ];
  }
}
