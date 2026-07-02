namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Look at the top N cards of your library, then put them back in any order." —
/// Sensei's Divining Top's "{1}:" ability. The activated-ability sibling of
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.LookAtTopCardsTriggeredRule"/>.
///
/// <para>
/// This is a SINGLE <see cref="LookAtCardsEffect"/>: the ", then put them back in any
/// order" clause is the look's disposition (carried by
/// <see cref="LookAtCardsEffect.PutBackInAnyOrder"/>), not a second sibling effect.
/// CR 701.12 (look) does not define a default disposition, so the reorder is explicit.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 950)]
public sealed class LookAtTopCardsPutBackEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Look\s+at\s+the\s+top\s+(?<count>\d+|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+of\s+your\s+library,\s*then\s+put\s+them\s+back\s+in\s+any\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = Pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var count = ParseCount(match.Groups["count"].Value.ToLowerInvariant());
    if (count is null)
    {
      return null;
    }

    return new LookAtCardsEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(count.Value),
      Zone = Zone.Library,
      Location = "Top",
      PutBackInAnyOrder = true,
    };
  }

  private static int? ParseCount(string raw) =>
    raw switch
    {
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ when int.TryParse(raw, out var n) => n,
      _ => null,
    };
}
