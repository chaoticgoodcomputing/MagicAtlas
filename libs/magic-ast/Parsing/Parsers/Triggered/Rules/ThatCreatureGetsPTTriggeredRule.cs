namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that creature gets +N/+M [until end of turn]" — triggered P/T modification on
/// the creature named by the trigger condition, not the ability's own source.
/// Primal Forcemage: "Whenever another creature you control enters, that creature
/// gets +3/+3 until end of turn." The subject "that creature" back-references the
/// object the trigger's Filter identified (CR 603.2 — the triggering event names
/// the object), mapping to <see cref="ObjectReferenceKind.ThatCreature"/>. This is
/// the sibling of the "it"/"this creature"/"target creature" subjects handled by
/// <see cref="ModifyPTTriggeredRule"/>, which does not recognise "that creature".
///
/// <para>
/// The P/T buff itself is a continuous effect modifying characteristics
/// (CR 611.1), carried as a <see cref="ModifyPTEffect"/> with an "until end of
/// turn" <see cref="UntilTimeDuration"/> when the clock clause is present.
/// </para>
///
/// <para>
/// Anchored on the WHOLE fragment (^…$) so the more-specific siblings in this
/// surface family are never mis-claimed: "…until end of turn instead"
/// (replacement modifier), "…and gains &lt;keyword&gt; until end of turn"
/// (composite pump + grant), "…for each …" (variable modifier), and
/// "…and fights …" all carry trailing text past the P/T clause and therefore
/// fail the anchor, falling through to their own handlers.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThatCreatureGetsPTTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+creature\s+gets\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)(?<dur>\s+until\s+end\s+of\s+turn)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    MagicAST.AST.Effects.Duration? duration =
      match.Groups["dur"].Success ? UntilTimeDuration.EndOfTurn : null;

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = duration,
    };
    return true;
  }
}
