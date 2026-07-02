namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "Creatures you control can attack as though they didn't have defender." — High
/// Alert, Assault Formation family.
///
/// Rule 702.3b (verbatim): "A creature with defender can't attack." This static
/// ability overrides that restriction, allowing creatures with Defender to be
/// declared as attackers.
///
/// <para>
/// The pattern is fully anchored (^ … $) to prevent substring-matching another
/// oracle line that embeds this phrase inside a larger clause. The "they" pronoun
/// in High Alert's text ("as though they didn't have defender") is a plural
/// referring to the earlier "Creatures you control" subject; both forms are matched.
/// </para>
/// </summary>
[StaticRule(Priority = 955)]
public sealed class CanAttackIgnoringDefenderRule : IStaticRule
{
  // "Creatures you control can attack as though they didn't have defender."
  // Anchored. The "didn't have" contraction may appear as "didn't" (curly apostrophe)
  // or "didn't" (straight apostrophe) — the pattern accepts both via a character class.
  private static readonly Regex _controlledCreaturesPattern = new(
    @"^\s*Creatures\s+you\s+control\s+can\s+attack\s+as\s+though\s+they\s+didn['’]t\s+have\s+defender\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "This creature can attack as though it didn't have defender."
  private static readonly Regex _selfPattern = new(
    @"^\s*This\s+creature\s+can\s+attack\s+as\s+though\s+it\s+didn['’]t\s+have\s+defender\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var raw = clause.RawText;

    if (_controlledCreaturesPattern.IsMatch(raw))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new CanAttackIgnoringDefenderEffect
            {
              AppliesTo = new ObjectReference
              {
                Kind = ObjectReferenceKind.Each,
                Filter = new ObjectFilter
                {
                  CardTypes = ["creature"],
                  Controller = ControllerFilter.You,
                },
              },
            },
          ],
        },
      ];
    }

    if (_selfPattern.IsMatch(raw))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new CanAttackIgnoringDefenderEffect()],
        },
      ];
    }

    return null;
  }
}
