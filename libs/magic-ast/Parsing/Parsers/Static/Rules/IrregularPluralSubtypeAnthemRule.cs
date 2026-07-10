namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Other &lt;IrregularPluralSubtype&gt; you control get +N/+M." — the bare
/// plural-subtype anthem shape (CR 611.3: a static ability generating a
/// continuous +N/+M effect) for creature subtypes whose plural form does NOT
/// end in a literal "s" (Mouse → Mice, Locus → Loci; Djinn is invariant
/// singular/plural — e.g. Mabel, Heir to Cragflame: "Other Mice you control get
/// +1/+1.").
///
/// <para>
/// <see cref="LordPTBuffRule"/>'s bare-plural-subtype branch already carries a
/// <c>_subtypeIrregularPlurals</c> table with entries for "Mice", "Loci", and
/// "Djinn", but its surrounding regex (<c>^(?&lt;sub&gt;[A-Z][a-z]+)s$</c>)
/// requires the whole word to end in a literal "s" before that table is ever
/// consulted. "Elves"/"Dwarves"/"Wolves" satisfy that (they end in "s") and
/// resolve correctly today; "Mice"/"Loci"/"Djinn" do not end in "s" at all, so
/// those three table entries are structurally unreachable in the shared rule.
/// Rather than editing that shared rule body (and risking the sibling shapes it
/// already owns), this is a disjoint, fully-anchored sibling that owns only the
/// irregular no-trailing-s subset; it declines (returns null) for every other
/// bare-plural-subtype shape, which <see cref="LordPTBuffRule"/> continues to
/// own unchanged.
/// </para>
///
/// <para>CR 611.3 (static ability generates the continuous effect); CR 109.5
/// ("another" / "other" — self-exclusion, <see cref="ObjectFilter.ExcludeSelf"/>).
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class IrregularPluralSubtypeAnthemRule : IStaticRule
{
  private static readonly IReadOnlyDictionary<string, string> _irregularPlurals =
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["Mice"] = "Mouse",
      ["Loci"] = "Locus",
      ["Djinn"] = "Djinn",
    };

  private static readonly Regex _pattern = new(
    @"^\s*Other\s+(?<sub>Mice|Loci|Djinn)\s+(?<ctrl>you\s+control|your\s+opponents\s+control)\s+get\s+\+(?<p>\d+)/\+(?<t>\d+)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var subtype = _irregularPlurals[match.Groups["sub"].Value];
    var controller = match.Groups["ctrl"].Value.StartsWith("you", StringComparison.OrdinalIgnoreCase)
      ? ControllerFilter.You
      : ControllerFilter.Opponent;
    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Subtypes = [subtype],
              Controller = controller,
              ExcludeSelf = true,
            },
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
