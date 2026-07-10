namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "untap each [type] you control" — mass-untap triggered effect using the
/// "each" quantifier (Port Razer: "untap each creature you control").
///
/// <para>
/// Sibling of <see cref="UntapAllTriggeredRule"/>, which covers the "all"
/// phrasing ("untap all lands you control", etc.). Kept as a separate,
/// independently anchored rule rather than folding "each" into the shared
/// "all" regex, so this addition cannot alter behaviour for any card already
/// covered by <see cref="UntapAllTriggeredRule"/>'s gold fixtures.
/// </para>
///
/// <para>
/// Produces an <see cref="UntapEffect"/> whose Target is
/// <see cref="ObjectReferenceKind.Each"/> filtered to the named card type with
/// <see cref="ControllerFilter.You"/> when "you control" is present, or no
/// controller filter otherwise.
/// </para>
///
/// <para>
/// Rule 701.26 (Tap and Untap). Pattern anchored (^...$) so it only matches
/// the standalone sentence — used by the sentence-bundle dispatcher to combine
/// with a following sentence (e.g. "After this phase, there is an additional
/// combat phase.", matched independently by
/// <see cref="AdditionalCombatPhaseTriggeredRule"/>) into a flat effects list.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class UntapEachTriggeredRule : ITriggeredRule
{
  // Named groups:
  //   type      — the card-type noun (land, creature, permanent, artifact, …)
  //   controller — present when "you control" is in the text
  private static readonly Regex Pattern = new(
    @"^untap\s+each\s+(?<type>[a-z]+)s?\s*(?<controller>you\s+control)?\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var cardType = m.Groups["type"].Value.ToLowerInvariant().TrimEnd('s');
    var hasController = m.Groups["controller"].Success;

    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = [cardType],
        Controller = hasController ? ControllerFilter.You : null,
      },
    };

    effect = new UntapEffect { Target = target };
    return true;
  }
}
