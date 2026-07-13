namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "You may pay {cost} rather than pay this spell's mana cost." — the
/// Verdant Mastery / Bringer cycle shape: a self-referential alternative cost
/// printed on the spell itself, as opposed to <see cref="GrantAlternativeCostForAllSpellsRule"/>'s
/// Fist of Suns shape (a permanent granting the option to every OTHER spell the
/// controller casts) and <see cref="GrantAlternativeCostRule"/>'s Rooftop Storm
/// shape (a permanent granting the option to a filtered class of other spells).
///
/// <para>
/// CR 118.9 (verbatim): "Some spells have alternative costs. An alternative cost is
/// a cost listed in a spell's text, or applied to it from another effect, that its
/// controller may pay rather than paying the spell's mana cost. Alternative costs are
/// usually phrased, 'You may [action] rather than pay [this object's] mana cost,' or
/// 'You may cast [this object] without paying its mana cost.'" CR 604.5 confirms these
/// abilities function while the spell itself is on the stack.
/// </para>
///
/// <para>
/// Reuses <see cref="GrantAlternativeCostEffect"/> (the Fist of Suns representation)
/// rather than introducing a new discriminator: the only difference is the scope of
/// <see cref="GrantAlternativeCostEffect.AffectedSpells"/> — here a self-only filter
/// (<see cref="ObjectFilter.IsSelf"/>, CR 109's "this spell") instead of "spells you
/// cast" or a filtered class thereof. The regex requires "this spell's mana cost"
/// verbatim (not "the mana cost for … spells you cast"), so it cannot collide with
/// either sibling rule's anchored pattern.
/// </para>
/// </summary>
[StaticRule(Priority = 985)]
public sealed class GrantAlternativeCostForThisSpellRule : IStaticRule
{
  // Matches: "You may pay {N} rather than pay this spell's mana cost." where {N}
  // is one or more mana symbols. Anchored ^ and $ to prevent substring matches and
  // to keep this narrow to the bare self-referential shape (no "and <action>"
  // clause between the cost and "rather than", which would be the Borderpost
  // family's shape, handled elsewhere via AttributeExtractor).
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s+rather\s+than\s+pay\s+this\s+spell's\s+mana\s+cost\.?\s*$",
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

    // Self-only — "this spell's mana cost" refers to the spell the ability is
    // printed on (CR 109), not a filtered class of other spells.
    var filter = new ObjectFilter { CardTypes = ["spell"], IsSelf = true };

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
