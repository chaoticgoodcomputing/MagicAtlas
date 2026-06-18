namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// "untap it and all [Subtype] you control" — the compound untap form that
/// untaps the source permanent (referred to by "it") AND every permanent of
/// a named subtype the controller controls.
///
/// <para>Produces a <see cref="CompositeEffect"/> containing two
/// <see cref="UntapEffect"/> instances:</para>
/// <list type="number">
///   <item><c>untap it</c> — self-reference (<see cref="ObjectReferenceKind.Self"/>)</item>
///   <item><c>untap all [Subtype] you control</c> — <see cref="ObjectReferenceKind.Each"/>
///     with <see cref="ObjectFilter.Controller"/> = <see cref="ControllerFilter.You"/> and
///     the named subtype on <see cref="ObjectFilter.Subtypes"/>.</item>
/// </list>
///
/// <para>CR 701.26b: "To untap a permanent, rotate it back to the upright position from a
/// sideways position. Only tapped permanents can be untapped."</para>
///
/// <para>Priority 55 — above the generic <see cref="UntapSelfRule"/> (Priority 50) and
/// above <see cref="UntapAllTriggeredRule"/> (Priority 50) so this compound form is
/// matched first. The pattern is anchored (^...$) so it cannot silently consume a text
/// that only partially matches (e.g. "untap it" alone).</para>
/// </summary>
[TriggeredRule(Priority = 55)]
public sealed class UntapSelfAndAllSubtypeTriggeredRule : ITriggeredRule
{
  // "untap it and all <Subtype> you control"
  // Anchored to prevent cross-match with "untap it" alone.
  private static readonly Regex _pattern = new(
    @"^untap\s+it\s+and\s+all\s+(?<subtype>[A-Za-z]+(?:\s+[A-Za-z]+)*)\s+you\s+control\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var subtypeRaw = m.Groups["subtype"].Value.Trim();
    // Normalise to title case (first letter upper, rest preserved).
    var subtype = char.ToUpperInvariant(subtypeRaw[0]) + subtypeRaw[1..];

    var untapSelf = new UntapEffect
    {
      Target = ObjectReference.Self(),
    };

    var untapAllSubtype = new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
        },
      },
    };

    effect = new CompositeEffect
    {
      Effects = [untapSelf, untapAllSubtype],
    };
    return true;
  }
}
