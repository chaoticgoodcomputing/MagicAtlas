namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Creatures your opponents control lose &lt;keyword&gt; and can't have or gain
/// &lt;keyword&gt;." — the Archetype-cycle opponent ability-lock (e.g. Archetype of
/// Imagination's "lose flying and can't have or gain flying").
///
/// <para>
/// Per CR 113.11 the "can't have" construct DEFINES the loss as well: "These effects
/// say that the object 'can't have' that ability. If the object has that ability, it
/// loses it. It's also impossible for an effect or keyword counter to add that ability
/// to the object." So "lose flying and can't have or gain flying" is ONE continuous
/// effect (a can't-have lock), captured by a single
/// <see cref="CantHaveOrGainKeywordEffect"/> — the removal is subsumed, not double
/// modelled. CR 611.3 is the static-ability continuous-effect authority.
/// </para>
///
/// <para>
/// The keyword is recognised by REUSING <see cref="StaticRuleHelpers.MapKeywordToStaticAbility"/>
/// and lifting its <c>KeywordSource</c> so unrecognised keywords return null (dispatch
/// keeps cascading — no false parse). Priority 985 is very specific; the anchored
/// pattern guards against sibling shadowing.
/// </para>
/// </summary>
[StaticRule(Priority = 985)]
public sealed class OpponentsCreaturesCantHaveOrGainKeywordRule : IStaticRule
{
  // Anchored full-line match. Both keyword captures are compared for equality
  // (a real Archetype line repeats the same keyword). Allows both a straight (')
  // and curly (’) apostrophe in "can't".
  private static readonly Regex _cantHaveOrGainPattern = new(
    @"^\s*Creatures\s+your\s+opponents\s+control\s+lose\s+(?<kw1>[a-z][a-z ]+?)\s+and\s+can['’]?t\s+have\s+or\s+gain\s+(?<kw2>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);

    var match = _cantHaveOrGainPattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var kw1 = match.Groups["kw1"].Value.Trim().ToLowerInvariant();
    var kw2 = match.Groups["kw2"].Value.Trim().ToLowerInvariant();

    // Defensive: a real Archetype line locks the SAME keyword it removes.
    if (kw1 != kw2)
    {
      return null;
    }

    // Reuse existing keyword recognition; null (unrecognised) → decline so
    // dispatch keeps cascading (no false parse).
    var keyword = StaticRuleHelpers.MapKeywordToStaticAbility(kw1)?.KeywordSource;
    if (keyword is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new CantHaveOrGainKeywordEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.Opponent,
            },
          },
          Keyword = keyword.Value,
        }],
      },
    ];
  }
}
