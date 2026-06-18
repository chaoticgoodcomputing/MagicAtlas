namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Mycosynth Lattice "all cards…, spells, and permanents are colorless"
/// oracle template: a continuous layer-5 effect that makes all game objects
/// colorless.
///
/// <para>
/// The oracle line enumerates three disjoint groups that together cover every
/// game object: cards not on the battlefield (hand, library, graveyard, exile,
/// command zone), spells (on the stack), and permanents (on the battlefield).
/// Each group is modelled as a separate <see cref="ChangeColorEffect"/> with
/// <c>Colors: []</c> (colorless) under a single <see cref="StaticAbility"/>,
/// honouring the oracle sentence as written rather than collapsing it to a
/// single "everything" filter.
/// </para>
///
/// <para>
/// CR 105.3 (verbatim): "Effects may change an object's color or give a color
/// to a colorless object. If an effect gives an object a new color, the new
/// color replaces all previous colors the object had (unless the effect said the
/// object became that color 'in addition' to its other colors)." A layer-5
/// effect setting Colors = [] makes the object colorless (CR 105.2c: "A colorless
/// object has no color.").
/// </para>
///
/// <para>
/// Priority 968 — below <see cref="AllObjectsAddTypeRule"/> (969) and
/// <see cref="NonlandCreatureTypeGrantRule"/> (970).
/// </para>
/// </summary>
[StaticRule(Priority = 968)]
public sealed class AllObjectsAreColorlessRule : IStaticRule
{
  // Anchored exact match for the Mycosynth Lattice colorless sentence.
  // CR 105.3 / CR 613.1e layer-5 continuous effect.
  private static readonly Regex _pattern = new(
    @"^\s*All\s+cards\s+that\s+aren't\s+on\s+the\s+battlefield,\s+spells,\s+and\s+permanents\s+are\s+colorless\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    // Three sibling ChangeColorEffect nodes, one per enumerated group.
    // Colors = [] encodes "colorless" (CR 105.2c).
    return
    [
      new StaticAbility
      {
        Effects =
        [
          // Group 1: cards not on the battlefield (hand, library, graveyard, exile,
          // command zone). The current ObjectFilter schema has no ExcludedZone axis,
          // so the zone restriction is captured by the card type alone; the engine
          // resolves the zone exclusion from context.
          new ChangeColorEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["card"],
              },
            },
            Colors = [],
          },
          // Group 2: spells (objects on the stack).
          new ChangeColorEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["spell"],
              },
            },
            Colors = [],
          },
          // Group 3: permanents (objects on the battlefield).
          new ChangeColorEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["permanent"],
              },
            },
            Colors = [],
          },
        ],
      },
    ];
  }
}
