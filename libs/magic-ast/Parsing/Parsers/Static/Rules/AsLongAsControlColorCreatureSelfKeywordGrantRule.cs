namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "This creature has [keyword] as long as you control a [color] creature." — the
/// conditional self-keyword-grant gated on a color-qualified control count
/// (Wingrattle Scarecrow: "This creature has flying as long as you control a blue
/// creature."; "This creature has persist as long as you control a black creature.
/// (When this creature dies, if it had no -1/-1 counters on it, return it to the
/// battlefield under its owner's control with a -1/-1 counter on it.)"). CR 702.79a
/// (Persist, verbatim): "Persist is a triggered ability. 'Persist' means 'When this
/// permanent is put into a graveyard from the battlefield, if it had no -1/-1
/// counters on it, return it to the battlefield under its owner's control with a
/// -1/-1 counter on it.'"
///
/// <para>
/// CR 611.3a (continuous effect from a static ability, not "locked in" — it "applies at
/// any given moment to whatever its text indicates"): the granted keyword applies only while
/// the stated condition is true. The condition is a <see cref="CountCondition"/>
/// over an <see cref="ObjectFilter"/> keyed on <c>CardTypes:["creature"]</c>,
/// <c>Colors:[&lt;color&gt;]</c>, and <c>Controller: You</c> — "you control a
/// [color] creature" is an existence check (at least one), so the threshold is
/// GreaterThanOrEqual 1 — wrapped in an <see cref="AsLongAsDuration"/>. The keyword
/// itself resolves through <see cref="StaticRuleHelpers.MapKeywordToStaticAbility"/>,
/// the same shared keyword-to-static-ability table every other conditional grant
/// rule uses, so this rule generalizes over any keyword that table supports (not
/// just flying/persist).
/// </para>
///
/// <para>
/// Sibling of <see cref="AsLongAsStaticGrantRule"/>'s generic suffix-form
/// "[subject] has [keyword] as long as [condition]" shape, but that generic rule's
/// suffix pattern has no reminder-stripping step before matching the condition
/// clause, so a keyword whose reminder text rides after the "as long as" clause
/// (e.g. Persist) would have the reminder text swallowed whole into the condition.
/// This dedicated rule captures the trailing parenthetical separately and attaches
/// it to the outer <see cref="StaticAbility"/> (CR 207.2), mirroring
/// <see cref="NontokenCreaturesHaveKeywordRule"/>'s reminder handling. Sits above
/// <see cref="AsLongAsStaticGrantRule"/> (968) so this more specific shape is tried
/// first. Fully anchored (^…$) on the specific "This creature has [keyword] as long
/// as you control a [color] creature" surface, so it cannot collide with any other
/// "as long as" shape.
/// </para>
/// </summary>
[StaticRule(Priority = 970)]
public sealed class AsLongAsControlColorCreatureSelfKeywordGrantRule : IStaticRule
{
  private static readonly IReadOnlyDictionary<string, string> ColorWordToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  private static readonly Regex _pattern = new(
    @"^\s*This\s+creature\s+has\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\s+as\s+long\s+as\s+you\s+control\s+a\s+(?<color>white|blue|black|red|green)\s+creature\.?\s*(?<reminder>\([^)]+\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var keyword = match.Groups["kw"].Value.Trim();
    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(keyword);
    if (grantedAbility is null)
    {
      // Keyword not yet supported — decline so an honest fallback handles it.
      return null;
    }

    var colorCode = ColorWordToCode[match.Groups["color"].Value];
    var condition = new CountCondition
    {
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Colors = [colorCode],
        Controller = ControllerFilter.You,
      },
      Count = new Comparison { Operator = ComparisonOperator.GreaterThanOrEqual, Value = 1 },
    };

    var reminderRaw = match.Groups["reminder"].Value.Trim();
    Parenthetical? reminder = string.IsNullOrEmpty(reminderRaw)
      ? null
      : new Parenthetical { Text = reminderRaw };

    return
    [
      new StaticAbility
      {
        Reminder = reminder,
        Effects =
        [
          new GainAbilityEffect
          {
            Target = ObjectReference.Self(),
            GainedAbility = grantedAbility,
            Duration = new AsLongAsDuration { Condition = condition },
          },
        ],
      },
    ];
  }
}
