namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Dies trigger on a creature a player controls that lacks a specific keyword ability —
/// the Luminous Broodmoth family. Oracle text: "a creature you control without flying
/// dies", "a creature you control without flying or reach dies", etc.
///
/// <para>
/// "without &lt;keyword&gt;" is the first-class keyword-absence axis (CR 702.9 flying,
/// etc.): a recognised keyword routes to <see cref="ObjectFilter.LacksKeywords"/> rather
/// than the <see cref="OtherCharacteristic"/> residual, per the convention established by
/// <c>CreaturesCantBlockThisTurnRule</c> (Falter, M10). Unknown keywords keep the honest
/// free-text fallback.
/// </para>
///
/// CR 700.4 ("dies" = moves from battlefield to graveyard, see also TriggerEvent.Dies).
/// Priority above generic DiesConditionRule (991) so this more specific guard fires first.
/// </summary>
[TriggerConditionRule(Priority = 992)]
public sealed class CreatureWithoutKeywordDiesConditionRule : ITriggerConditionRule
{
  // Matches "a creature you control without <keyword> dies"
  // The <keyword> group captures the lowercase keyword name (e.g. "flying", "flanking").
  private static readonly Regex _pattern = new(
    @"\ba\s+creature\s+you\s+control\s+without\s+(?<keyword>[a-z]+)\s+dies\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    var m = _pattern.Match(lower);
    if (!m.Success)
    {
      return null;
    }

    var keyword = m.Groups["keyword"].Value.ToLowerInvariant();
    var filter = Enum.TryParse<KeywordAbility>(keyword, ignoreCase: true, out var kw)
      ? new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You, LacksKeywords = [kw] }
      : new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
        Characteristics =
        [
          Characteristic.Other($"without{char.ToUpperInvariant(keyword[0])}{keyword[1..]}"),
        ],
      };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = filter,
    };
  }
}
