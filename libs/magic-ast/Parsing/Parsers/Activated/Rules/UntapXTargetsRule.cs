namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Untap X target lands." — variable-count untap-targets activated-ability effect,
/// where the count is the ability's own activation-cost X (CR 107.3 / 107.3a: the
/// controller chooses X as the ability is activated; here X also determines how many
/// lands are untapped — CR 107.3c, "the value of X is used in more than one place").
/// Candelabra of Tawnos: "{X}, {T}: Untap X target lands."
///
/// <para>
/// The variable lives on <see cref="UntapEffect.Count"/> as a <see cref="VariableQuantity"/>
/// (NOT on <see cref="ObjectReference.Quantity"/>, which is reserved for the "up to N
/// target" phrasing handled by <see cref="UntapUpToNTargetPermanentsRule"/>). Mirrors the
/// tap-side sibling shape already established by the spell-side
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.TapXTargetsRule"/> and the
/// activated-side <see cref="TapEffectRule"/>'s variable-count handling (Mishra's Helix:
/// "{X}, {T}: Tap X target lands."), applied to untap instead of tap.
/// </para>
///
/// <para>
/// CR 701.26 / 701.26b: "To untap a permanent, rotate it back to the upright position
/// from a sideways position. Only tapped permanents can be untapped."
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): matches only the full "Untap [X|Y|Z] target TYPE(S)." shape,
/// so it never collides with sibling untap phrasings that use a different quantifier —
/// "Untap up to N target permanents" (<see cref="UntapUpToNTargetPermanentsRule"/>),
/// "Untap another target …" / "Untap N other target …"
/// (<see cref="UntapAnotherOrCountTargetActivatedEffectRule"/>), or the plain
/// "Untap target …" shapes (<see cref="UntapTargetCardTypeActivatedEffectRule"/>,
/// <see cref="UntapEffectRule"/>). Priority 996: above every other untap effect rule in
/// this directory (995/994/993), since none of those patterns start with a bare
/// variable-letter token before "target" anyway, but the ordering keeps this rule the
/// deliberate first stop for the "X target" shape.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class UntapXTargetsRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Untap\s+(?<var>[XYZ])\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var m = Pattern.Match(effectText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var typesPhrase = m.Groups["types"].Value;
    var types = Regex
      .Split(typesPhrase, @"\s*,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();

    if (types.Count == 0)
    {
      return null;
    }

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = types },
      },
      Count = new VariableQuantity { Name = m.Groups["var"].Value.ToUpperInvariant() },
    };
  }
}
