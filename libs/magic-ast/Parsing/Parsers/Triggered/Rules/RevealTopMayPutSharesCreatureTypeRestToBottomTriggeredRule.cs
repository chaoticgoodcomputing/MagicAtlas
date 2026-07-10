namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Reveal the top N cards of your library. You may put a card that shares a creature
/// type with it from among them into your hand. Put the rest on the bottom of your
/// library in a random order." — the Tajuru Paragon kicked-ETB reveal family.
///
/// <para>
/// The whole three-sentence fragment is one coherent game action — "from among them" and
/// "the rest" in the second and third sentences are back-references to the reveal in the
/// first — so this rule matches all three sentences as a single fragment and emits one
/// <see cref="RevealTopMayPutSharesCreatureTypeRestToBottomEffect"/>. CR 701.20 (Reveal);
/// CR 205.3m (creature types); CR 400.4 (the remainder placed on the bottom in a random
/// order).
/// </para>
///
/// <para>
/// "It" (the object the revealed card must share a creature type with) is the source
/// creature — <see cref="ObjectReference.Self"/> — since this line only ever appears as
/// the resolution of a self-referential "when this creature enters, if it was kicked"
/// trigger (CR 201.5/109.5).
/// </para>
///
/// <para>
/// Priority 95: must beat the generic multi-sentence bundle splitter (mirroring
/// <see cref="RevealTopPutMatchingToHandTriggeredRule"/>), which would otherwise try to
/// resolve each of the three sentences independently and fail.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class RevealTopMayPutSharesCreatureTypeRestToBottomTriggeredRule : ITriggeredRule
{
  // Matches: "reveal the top <count> cards of your library. You may put a card that
  // shares a creature type with it from among them into your hand. Put the rest on the
  // bottom of your library in a random order". Anchored end-to-end (no sibling rule
  // shares this three-sentence surface).
  private static readonly Regex _pattern = new(
    @"^reveal\s+the\s+top\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+of\s+your\s+library\.\s*"
    + @"You\s+may\s+put\s+a\s+card\s+that\s+shares\s+a\s+creature\s+type\s+with\s+it\s+from\s+among\s+them\s+into\s+your\s+hand\.\s*"
    + @"Put\s+the\s+rest\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+a\s+random\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(match.Groups["count"].Value);
    if (count is null)
    {
      return false;
    }

    effect = new RevealTopMayPutSharesCreatureTypeRestToBottomEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(count.Value),
      Filter = new ObjectFilter { SharesCreatureTypeWith = ObjectReference.Self() },
    };
    return true;
  }
}
