namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// Dice-advantage replacement effect — the "Grant an Advantage" template (Pixie Guide,
/// Wyll, Blade of Frontiers, Barbarian Class):
/// "If you would roll one or more dice, instead roll that many dice plus one and ignore
/// the lowest roll."
///
/// <para>
/// CR 614.1: "If [event] would happen, [modified event] instead" is a replacement effect
/// (a continuous shield that watches for the roll event), NOT a triggered ability. The
/// replaced event is a <see cref="DiceRollEvent"/> by you ("one or more" →
/// <c>MinimumQuantity</c> 1; the rolling player on <c>Controller</c> =
/// <see cref="ObjectReference.You"/>). The modification — "roll that many dice plus one
/// and ignore the lowest roll" — is the atomic <c>ReplacementModifier{ Type: "advantage" }</c>:
/// roll N+1 dice and discard the lowest result (CR 706.6 — an ignored roll is treated as
/// never having happened). <c>OriginalEventOccurs = false</c> (the N-dice roll is replaced
/// by the N+1 roll, not performed in addition), mirroring
/// <see cref="CounterDoublingReplacementRule"/>/<see cref="MillDoublingReplacementRule"/>.
/// </para>
///
/// <para>
/// Pixie Guide / Barbarian Class front the line with the "Grant an Advantage" ability
/// word (CR 207.2c). The classifier captures it on <c>classification.AbilityWord</c>; the
/// em-dash prefix is stripped here (same convention as <see cref="AsLongAsStaticGrantRule"/>)
/// and re-emitted on the resulting <see cref="StaticAbility.AbilityWord"/>. Wyll prints the
/// same line with no ability word.
/// </para>
///
/// <para>Anchored (^…$). Priority 977 — alongside the other replacement rules; the exact
/// phrase cannot be confused with any other static shape.</para>
/// </summary>
[StaticRule(Priority = 977)]
public sealed class DiceAdvantageReplacementRule : IStaticRule
{
  // "If you would roll one or more dice, instead roll that many dice plus one and ignore the lowest roll."
  private static readonly Regex _advantagePattern = new(
    @"^\s*If\s+you\s+would\s+roll\s+one\s+or\s+more\s+dice,\s+instead\s+roll\s+that\s+many\s+dice\s+plus\s+one\s+and\s+ignore\s+the\s+lowest\s+roll\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Peel a "Grant an Advantage — " ability-word prefix (CR 207.2c). The classifier has
    // already captured the word on classification.AbilityWord; strip the em-dash prefix so
    // the body anchors on "^\s*If you would roll" (same convention as AsLongAsStaticGrantRule).
    var body = clause.RawText;
    var abilityWord = classification.AbilityWord;
    if (abilityWord is not null)
    {
      var emDashIdx = body.IndexOf('—');
      if (emDashIdx >= 0)
      {
        body = body[(emDashIdx + 1)..].TrimStart();
      }
    }

    if (!_advantagePattern.IsMatch(body))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        AbilityWord = abilityWord,
        Effects =
        [
          new ReplacementEffect
          {
            Event = new DiceRollEvent
            {
              MinimumQuantity = 1,
              Controller = ObjectReference.You(),
            },
            OriginalEventOccurs = false,
            Modifier = new ReplacementModifier { Type = "advantage" },
          },
        ],
      },
    ];
  }
}
