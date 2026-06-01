namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;

/// <summary>
/// "open an Attraction." — keyword action (Rule 701.51) on the triggered side.
/// Matches the bare keyword action when it is the entire effect body of a
/// triggered ability (e.g. Seasoned Buttoneer's ETB trigger). The trailing
/// parenthetical reminder is stripped by the dispatcher before this rule runs
/// (Rule 207.2).
///
/// CR 701.51b: "To open an Attraction, move the top card of your Attraction deck
/// off the Attraction deck, turn it face up, and put it onto the battlefield under
/// your control."
/// CR 603.6a: Enters-the-battlefield abilities trigger when a permanent enters the
/// battlefield. These are written, "When [this object] enters, . . ."
///
/// Emits the <see cref="OpenAttractionEffect"/> node.
/// </summary>
[TriggeredRule]
public sealed class OpenAttractionTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!trimmed.Equals("open an Attraction", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new OpenAttractionEffect(), false);
    return true;
  }
}
