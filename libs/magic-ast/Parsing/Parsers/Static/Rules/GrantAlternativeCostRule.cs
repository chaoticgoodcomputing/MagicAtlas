namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "You may pay {cost} rather than pay the mana cost for [type] [subtype]
/// creature spells you cast." — the Rooftop Storm shape.
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
/// The surface phrase "You may pay {0} rather than pay the mana cost for" is fully
/// anchored (^…$) because Rooftop Storm's full oracle text is a single ability line;
/// no sibling trigger or effect shares this opening. The filter noun phrase
/// "<em>subtype</em> creature" (or just "<em>subtype</em>") before "spells you cast"
/// is captured and routed through
/// <see cref="StaticRuleHelpers.BuildTypeSpellFilter(string)"/> to emit a structured
/// <see cref="ObjectFilter"/>.
/// </para>
/// </summary>
[StaticRule(Priority = 985)]
public sealed class GrantAlternativeCostRule : IStaticRule
{
  // Matches the Rooftop Storm shape:
  //   "You may pay {N} rather than pay the mana cost for <filter> spells you cast."
  // where {N} is one or more mana symbols (e.g. "{0}", "{1}{G}") and <filter>
  // is one or more words describing the spell type/subtype (e.g. "Zombie creature",
  // "Dragon", "instant or sorcery"). Anchored ^ and $ to prevent substring matches.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s+rather\s+than\s+pay\s+the\s+mana\s+cost\s+for\s+(?<filter>.+?)\s+spells\s+you\s+cast\.?\s*$",
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
      // {0} is valid: GenericAmount=0; allow it (the ManaCostParser represents {0}
      // as one Generic symbol with GenericAmount=0, matching the MoxOpal fixture).
      cost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    // Build the filter from the noun phrase that precedes "spells you cast".
    // "Zombie creature" → filter on spell + creature + Zombie subtype;
    // "Dragon" → filter on spell + Dragon subtype; etc.
    var filterPhrase = match.Groups["filter"].Value.Trim();

    // Decompose a "[subtype] creature" compound phrase into subtype + creature-type
    // so the filter carries both CardTypes: ["spell","creature"] and
    // Subtypes: ["Zombie"] (Rooftop Storm's precise filter).
    ObjectFilter? filter = null;
    var creatureCompound = Regex.Match(
      filterPhrase,
      @"^(?<subtype>[A-Za-z]+)\s+creature$",
      RegexOptions.IgnoreCase
    );
    if (creatureCompound.Success)
    {
      var subtype = StaticRuleHelpers.Capitalize(creatureCompound.Groups["subtype"].Value);
      filter = new ObjectFilter
      {
        CardTypes = ["spell", "creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      };
    }
    else
    {
      // Fallback: single-noun filter (e.g. "Dragon") via shared helper.
      filter = StaticRuleHelpers.BuildTypeSpellFilter(filterPhrase);
    }

    if (filter is null)
    {
      return null;
    }

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
