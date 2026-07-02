namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Foods you control have all activated abilities of all creature cards exiled
/// with this creature." — the Hazel's Brewmaster continuous static ability that
/// copies a dynamic set of activated abilities from exiled creature cards onto
/// Foods the controller controls.
///
/// <para>
/// <b>CR 613.1f</b> (layer 6 — ability-granting continuous effects): this static
/// ability continuously grants abilities to the target permanents while the source
/// is on the battlefield.
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent matching substrings of longer ability lines.
/// Must run before <see cref="BareKeywordGrantRule"/> (which tries to match
/// "Foods you control have …") to claim the more-specific "all activated abilities"
/// surface before the keyword-grant fallback can attempt it. Priority 996 (above
/// BareKeywordGrantRule at 967).
/// </para>
/// </summary>
[StaticRule(Priority = 996)]
public sealed class FoodsHaveActivatedAbilitiesOfExiledCreaturesRule : IStaticRule
{
  // "Foods you control have all activated abilities of all creature cards exiled
  //  with this creature."
  // Anchored and compiled. Case-insensitive so "Foods" / "foods" both match.
  private static readonly Regex Pattern = new(
    @"^\s*Foods\s+you\s+control\s+have\s+all\s+activated\s+abilities\s+of\s+all\s+creature\s+cards\s+exiled\s+with\s+this\s+creature\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!Pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    // Target: "Foods you control" → Each Food you control on the battlefield.
    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        Subtypes = ["Food"],
        Controller = ControllerFilter.You,
      },
    };

    // Source filter: "all creature cards exiled with this creature"
    // CardTypes=["creature"], Zone=Exile, ExiledWith={Kind:Self}.
    // Per ADR 0004 "reference, not resolution": the ExiledWith reference names the
    // linking object (Self = this creature); the engine evaluates which cards are
    // currently exiled with it.
    var sourceFilter = new ObjectFilter
    {
      CardTypes = ["creature"],
      Zone = Zone.Exile,
      ExiledWith = ObjectReference.Self(),
    };

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilitiesFromExiledCardsEffect
        {
          Target = target,
          AbilityKind = "activated",
          SourceFilter = sourceFilter,
        }],
      },
    ];
  }
}
