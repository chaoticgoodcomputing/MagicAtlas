namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "untap up to N lands" — bounded non-targeted land-untap triggered effect.
///
/// <para>
/// CR 603.6a: "Enters-the-battlefield abilities trigger when a permanent enters
/// the battlefield. These are written, 'When [this object] enters, …'" — the trigger
/// condition is handled by <see cref="EntersConditionRule"/>; this rule handles
/// only the effect clause.
/// </para>
///
/// <para>
/// Examples: "untap up to five lands" (Peregrine Drake). The count "up to N" is an
/// <see cref="UpToQuantity"/> on the target reference — the player-chosen, non-targeted
/// set of lands (no "target" keyword in oracle, so <see cref="ObjectReferenceKind.Any"/>
/// rather than <see cref="ObjectReferenceKind.Target"/>). No controller restriction is
/// stated in the text, so none is applied. Rule 701.26 (Tap and Untap).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class UntapUpToNLandsTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^untap\s+up\s+to\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+lands?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.').Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(m.Groups["count"].Value);
    if (count is null)
    {
      return false;
    }

    effect = new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter
        {
          CardTypes = ["land"],
        },
        Quantity = new UpToQuantity { Maximum = count.Value, Minimum = 0 },
      },
    };
    return true;
  }
}
