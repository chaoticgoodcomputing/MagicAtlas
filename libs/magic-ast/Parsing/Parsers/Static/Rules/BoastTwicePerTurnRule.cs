namespace MagicAST.Parsing.Parsers.Static;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// "Creatures you control can boast [N] times during each of your turns rather than once."
///
/// <para>
/// CR 702.142a: "A boast ability is a special kind of activated ability. 'Boast -
/// [Cost]: [Effect]' means '[Cost]: [Effect]. Activate only if this creature attacked
/// this turn and only once each turn.'" This static ability modifies that default
/// once-per-turn limit for controller-owned creatures.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the surface phrase "can boast … times during each of your turns
/// rather than once" is distinctive and will not match as a substring of any other
/// static rule.
/// </para>
/// </summary>
[StaticRule(Priority = 50)]
public sealed class BoastTwicePerTurnRule : IStaticRule
{
  // Matches "Creatures you control can boast N times during each of your turns rather than once."
  // where N is a word-number. Currently only "twice" (= 2) is printed in oracle text.
  private static readonly Regex _pattern = new(
    @"^\s*Creatures\s+you\s+control\s+can\s+boast\s+(?<count>twice|three\s+times|four\s+times|\d+\s+times)\s+during\s+each\s+of\s+your\s+turns\s+rather\s+than\s+once\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, int> _wordToCount =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["twice"] = 2,
      ["three times"] = 3,
      ["four times"] = 4,
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var countWord = m.Groups["count"].Value.Trim().ToLowerInvariant();
    int newLimit;
    if (!_wordToCount.TryGetValue(countWord, out newLimit))
    {
      // Try numeric form (e.g. "3 times")
      var numMatch = Regex.Match(countWord, @"^(\d+)\s+times$");
      if (!numMatch.Success || !int.TryParse(numMatch.Groups[1].Value, out newLimit))
      {
        return null;
      }
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ModifyBoastActivationLimitEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
              },
            },
            NewLimit = newLimit,
          },
        ],
      },
    ];
  }
}
