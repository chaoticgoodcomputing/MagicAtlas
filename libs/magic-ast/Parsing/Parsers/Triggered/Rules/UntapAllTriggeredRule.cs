namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "untap all [types] you control" — mass-untap triggered effect.
///
/// Covers patterns such as:
/// <list type="bullet">
///   <item>"untap all lands you control" (Bear Umbra)</item>
///   <item>"untap all creatures you control"</item>
///   <item>"untap all permanents you control"</item>
/// </list>
///
/// Produces an <see cref="UntapEffect"/> whose Target is
/// <see cref="ObjectReferenceKind.Each"/> filtered to the named card type with
/// <see cref="ControllerFilter.You"/> when "you control" is present, or no
/// controller filter otherwise (e.g. "untap all creatures").
///
/// Rule 701.26 (Tap and Untap).
/// </summary>
[TriggeredRule]
public sealed class UntapAllTriggeredRule : ITriggeredRule
{
  // Named groups:
  //   type      — the card-type noun (land, creature, permanent, artifact, …)
  //   controller — present when "you control" is in the text
  private static readonly Regex Pattern = new(
    @"^untap\s+all\s+(?<type>[a-z]+)s?\s*(?<controller>you\s+control)?",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
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
