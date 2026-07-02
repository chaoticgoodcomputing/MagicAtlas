namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// Counters that target activated or triggered abilities (or both), or the full
/// "spell, activated ability, or triggered ability" disjunction. Rule 701.6.
///
/// Handles these oracle-text shapes:
/// <list type="bullet">
///   <item>"Counter target activated ability."</item>
///   <item>"Counter target triggered ability."</item>
///   <item>"Counter target activated or triggered ability." (Stifle)</item>
///   <item>"Counter target spell, activated ability, or triggered ability." (Disallow)</item>
/// </list>
///
/// The target filter uses <c>CardTypes</c> to record which stack-object categories
/// are in scope: <c>"activatedAbility"</c>, <c>"triggeredAbility"</c>, and/or
/// <c>"spell"</c> — parallel to the way <see cref="CounterSpellRule"/> uses
/// <c>CardTypes: ["spell"]</c> for plain spell targets.
///
/// Priority 80: more specific than <see cref="CounterSpellRule"/> (priority 50)
/// and <see cref="CounterTargetTypeOrSubtypeSpellRule"/> (priority 80 by name-match
/// conflict resolved by the "activated/triggered" literal tokens not being colour
/// words, so no actual overlap there — but the explicit bump keeps ordering stable).
/// </summary>
[SpellRule(Priority = 80)]
public sealed class CounterAbilityRule : ISpellRule
{
  // Group <scope> captures one of:
  //   "activated ability"
  //   "triggered ability"
  //   "activated or triggered ability"
  //   "spell, activated ability, or triggered ability"
  private static readonly Regex Pattern = new(
    @"^Counter\s+target\s+"
    + @"(?<scope>"
    +   @"spell,\s+activated\s+ability,\s+or\s+triggered\s+ability"
    +   @"|activated\s+or\s+triggered\s+ability"
    +   @"|activated\s+ability"
    +   @"|triggered\s+ability"
    + @")\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var scope = m.Groups["scope"].Value.ToLowerInvariant().Trim();

    var cardTypes = scope switch
    {
      var s when s.StartsWith("spell") =>
        new[] { "spell", "activatedAbility", "triggeredAbility" },
      var s when s.StartsWith("activated or triggered") =>
        new[] { "activatedAbility", "triggeredAbility" },
      var s when s.StartsWith("activated") =>
        new[] { "activatedAbility" },
      _ => new[] { "triggeredAbility" },
    };

    effect = new CounterSpellEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = cardTypes },
      },
    };
    return true;
  }
}
