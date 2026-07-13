namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Activated abilities of sources with the chosen name can't be activated unless
/// they're mana abilities." — Pithing Needle's static ability. A continuous
/// restriction (CR 604.2 / CR 611.1) that prevents any activated ability
/// (CR 602.5: "A player can't begin to activate an ability that's prohibited from
/// being activated.") of a source (CR 609.7: "The source of an ability is the
/// object that generated it.") whose name matches the chosen card name from being
/// put on the stack — with a carve-out for mana abilities (CR 605.1a: "A mana
/// ability is an activated ability that meets [certain] criteria…").
///
/// <para>
/// Modelled as a <see cref="StaticAbility"/> whose <c>AffectedObjects</c> is an
/// <see cref="ObjectFilter"/> scoped by
/// <see cref="ObjectFilter.ChosenCharacteristic"/> =
/// <see cref="ChosenCharacteristicKind.CardName"/> — "sources with the chosen name"
/// — the structured consumer side of the CR 607 linked ability whose producer is
/// the paired "As this artifact enters, choose a card name." replacement (CR 614.12;
/// see <see cref="ChooseCardNameOnEntryRule"/>). No <c>CardTypes</c>/<c>Zone</c>
/// restriction: a "source" is any object in any zone, so the chosen-name predicate
/// alone names the affected objects. The <c>Effects</c> list holds a single
/// <see cref="CantActivateAbilitiesEffect"/> with
/// <c>ExceptManaAbilities = true</c> (the "unless they're mana abilities" carve-out,
/// modeled structurally rather than as free text) and no <c>Target</c> (the
/// affected-objects filter already names the locked objects). Mirrors
/// <see cref="ArtifactsActivatedAbilitiesLockedRule"/> with a chosen-name scope and
/// a mana-ability exception.
/// </para>
///
/// <para>
/// Anchored end-to-end (^…$) so it cannot fire as a substring of a longer clause
/// and is disjoint from the artifact-scoped
/// <see cref="ArtifactsActivatedAbilitiesLockedRule"/> /
/// <see cref="OpponentArtifactsActivatedAbilitiesLockedRule"/> templates.
/// </para>
/// </summary>
[StaticRule(Priority = 979)]
public sealed class ChosenNameSourcesActivatedAbilitiesLockedRule : IStaticRule
{
  // Anchored so it can't match inside a longer clause. Accepts optional trailing period.
  private static readonly Regex _pattern = new(
    @"^\s*Activated\s+abilities\s+of\s+sources\s+with\s+the\s+chosen\s+name\s+can['’]?t\s+be\s+activated\s+unless\s+they['’]?re\s+mana\s+abilities\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        AffectedObjects = new ObjectFilter
        {
          ChosenCharacteristic = ChosenCharacteristicKind.CardName,
        },
        Effects =
        [
          new CantActivateAbilitiesEffect { ExceptManaAbilities = true },
        ],
      },
    ];
  }
}
