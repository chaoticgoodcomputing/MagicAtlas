namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "If this creature was kicked, it enters with N &lt;counterType&gt; counters on it
/// and with &lt;keyword&gt;." — the counters-only sibling
/// (<see cref="KickerConditionalEntersWithCountersRule"/>) extended with a trailing
/// bare-keyword grant, e.g. Pouncing Kavu: "...it enters with two +1/+1 counters on
/// it and with haste."
///
/// <para>
/// CR 702.33a/d/e: kicker is a static ability paid as an additional cost while
/// casting; "kicked" is the linked condition an object can later refer to.
/// CR 614.1c: "[This permanent] enters with ..." is a replacement effect (modeled
/// as <see cref="StaticTimingKind.AsThisEnters"/>). CR 122.6: counters granted as an
/// object enters are put on it as part of that replacement. The trailing keyword
/// (e.g. haste, CR 702.10a) is a second, static-ability-scoped effect on the same
/// ability — one oracle sentence, two conjoined effects — not a second ability.
/// </para>
/// </summary>
[StaticRule(Priority = 954)]
public sealed class KickerConditionalEntersWithCountersAndKeywordRule : IStaticRule
{
  // "If this creature was kicked, it enters with N <counterType> counters on it and
  // with <keyword>." Count/counterType mirror the counters-only sibling; the
  // trailing keyword is restricted to a small closed set of bare (parameterless)
  // keyword abilities we can resolve unambiguously — anything else returns null so
  // misses stay honest rather than silently mis-parsing.
  private static readonly Regex _pattern = new(
    @"^\s*If\s+this\s+creature\s+was\s+kicked,\s+it\s+enters\s+with\s+(?<count>\d+|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\s+and\s+with\s+(?<keyword>[a-z]+(?:\s+[a-z]+)?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var countText = match.Groups["count"].Value;
    if (!StaticRuleHelpers.TryParseSmallCount(countText.ToLowerInvariant(), out var intCount))
    {
      return null;
    }

    var counterType = match.Groups["counterType"].Value;

    var keyword = ResolveKeyword(match.Groups["keyword"].Value);
    if (keyword is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects =
        [
          new MagicAST.AST.Effects.Counter.PutCountersEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
            Count = MagicAST.AST.Quantities.LiteralQuantity.Of(intCount),
            CounterType = counterType,
          },
          new KeywordAbilityEffect { Keyword = keyword.Value },
        ],
        Condition = MagicAST.Parsing.ConditionParser.Parse("this creature was kicked"),
      },
    ];
  }

  private static KeywordAbility? ResolveKeyword(string text) => text.Trim().ToLowerInvariant() switch
  {
    "haste" => KeywordAbility.Haste,
    "flying" => KeywordAbility.Flying,
    "trample" => KeywordAbility.Trample,
    "vigilance" => KeywordAbility.Vigilance,
    "menace" => KeywordAbility.Menace,
    "reach" => KeywordAbility.Reach,
    "deathtouch" => KeywordAbility.Deathtouch,
    "lifelink" => KeywordAbility.Lifelink,
    "first strike" => KeywordAbility.FirstStrike,
    "double strike" => KeywordAbility.DoubleStrike,
    "hexproof" => KeywordAbility.Hexproof,
    "indestructible" => KeywordAbility.Indestructible,
    _ => null,
  };
}
