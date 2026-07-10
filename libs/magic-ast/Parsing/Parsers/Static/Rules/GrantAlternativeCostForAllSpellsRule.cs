namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "You may pay {cost} rather than pay the mana cost for spells you
/// cast." — the Fist of Suns shape: an UNFILTERED grant, applying to every
/// spell the controller casts (as opposed to <see cref="GrantAlternativeCostRule"/>'s
/// Rooftop Storm shape, which restricts the grant to a named type/subtype).
///
/// <para>
/// CR 118.9 (verbatim): "Some spells have alternative costs. An alternative cost is
/// a cost listed in a spell's text, or applied to it from another effect, that its
/// controller may pay rather than paying the spell's mana cost. Alternative costs are
/// usually phrased, 'You may [action] rather than pay [this object's] mana cost,' or
/// 'You may cast [this object] without paying its mana cost.'"
/// </para>
///
/// <para>
/// The regex is anchored (^…$) — Fist of Suns's full oracle text is a single
/// ability line and no sibling trigger or effect shares this opening. It requires
/// "for spells you cast" to follow "the mana cost" with NO intervening filter noun;
/// that absence is exactly what distinguishes this shape from Rooftop Storm's
/// (whose filter phrase is mandatory in <see cref="GrantAlternativeCostRule"/>'s
/// pattern), so the two rules cannot collide on the same input.
/// </para>
/// </summary>
[StaticRule(Priority = 985)]
public sealed class GrantAlternativeCostForAllSpellsRule : IStaticRule
{
  // Matches the Fist of Suns shape:
  //   "You may pay {N} rather than pay the mana cost for spells you cast."
  // where {N} is one or more mana symbols (e.g. "{W}{U}{B}{R}{G}"). No filter
  // noun phrase precedes "spells you cast" — the grant applies to every spell
  // the controller casts. Anchored ^ and $ to prevent substring matches.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s+rather\s+than\s+pay\s+the\s+mana\s+cost\s+for\s+spells\s+you\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var costStr = match.Groups["cost"].Value;
    ManaCost cost;
    try
    {
      var parsed = new ManaCostParser().Parse(costStr);
      cost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    // Unfiltered — every spell the controller casts (CardTypes: ["spell"],
    // Controller: You), matching the rooted-filter convention used elsewhere
    // for "spells you cast" (see StaticRuleHelpers.BuildTypeSpellFilter).
    var filter = new ObjectFilter
    {
      CardTypes = ["spell"],
      Controller = ControllerFilter.You,
    };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new GrantAlternativeCostEffect
          {
            AlternativeCost = cost,
            AffectedSpells = filter,
          },
        ],
      },
    ];
  }
}
