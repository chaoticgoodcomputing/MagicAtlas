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
/// The filter encodes the keyword absence as a typed <see cref="OtherCharacteristic"/>
/// residual ("withoutFlying", "withoutFlanking", etc.) per the convention established
/// by Falter (M10). <see cref="OtherCharacteristic"/> is <c>IResidual</c>, not
/// <c>IUnparsed</c> — a deliberate scope deferral (ADR 0001), not a parse failure.
/// </para>
///
/// <para>
/// "without flying" is not a structured negation of the keyword-presence axis on
/// <see cref="ObjectFilter.Characteristics"/> because there is currently no
/// <c>NegatedKeywordCharacteristic</c> node; the "withoutX" residual is the
/// approved frontier encoding. See: Falter (M10), Cosmotronic Wave (GRN).
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

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
        Characteristics =
        [
          Characteristic.Other($"without{char.ToUpperInvariant(keyword[0])}{keyword[1..]}"),
        ],
      },
    };
  }
}
