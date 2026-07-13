namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Snow [permanents/creatures/lands/artifacts] don't untap during their
/// controllers' untap steps." — a static continuous effect (CR 502.3) that
/// suppresses the untap-step untapping of an entire SUPERTYPE-qualified class
/// of permanents, regardless of who controls them or the ability's source.
/// Canonical card: Freyalise's Radiance ("Snow permanents don't untap during
/// their controllers' untap steps.").
///
/// <para>
/// Sibling of <see cref="SubjectDoesntUntapDuringControllersUntapStepsRule"/>,
/// which covers the same effect shape with an optional leading COLOR qualifier
/// (or a bare basic-land subtype). This rule covers the leading SUPERTYPE
/// qualifier instead — "Snow" (CR 205.4a) is the only printed supertype known
/// to precede this exact template, but the rule generalizes over the four
/// card-type nouns (permanents/creatures/lands/artifacts) the same way its
/// color-qualified sibling does, and the supertype word is looked up so a
/// future "Legendary creatures don't untap…" sibling is covered without a new
/// rule. Since "Snow" never leads the color-qualifier sibling's alternation,
/// the two patterns are mutually exclusive — no collision risk.
/// </para>
///
/// <para>
/// Reuses the general <see cref="DoesntUntapEffect"/> node (Target +
/// WhoseUntapStep) rather than a new discriminator, keyed here to
/// <c>Target.Filter = { CardTypes:["permanent"|...], Supertypes:["Snow"] }</c>
/// — the same node other "doesn't untap" rules (self/enchanted/triggered
/// forms) already use, so any future rule needing a supertype- or
/// filter-scoped "doesn't untap" (e.g. a single-target Aura variant) can reuse
/// it without a new node.
/// </para>
///
/// <para>
/// CR 502.3 (verbatim): "Third, the active player determines which permanents
/// they control will untap. Then they untap them all simultaneously. This
/// turn-based action doesn't use the stack. Normally, all of a player's
/// permanents untap, but effects can keep one or more of a player's permanents
/// from untapping."
/// </para>
///
/// <para>
/// CR 205.4a (verbatim, supertypes): "The supertypes are basic, legendary,
/// ongoing, snow, and world." Snow (CR 205.4g) marks a permanent as a
/// snow-qualified object; it's a printed characteristic, not a color or card
/// type, so it's recorded on <see cref="ObjectFilter.Supertypes"/> alongside
/// the card-type noun, mirroring <see cref="EachSupertypeCreatureAnthemModifyPTRule"/>'s
/// "Each [supertype] creature you control" precedent.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full oracle sentence is matched exactly so this rule
/// cannot fire on a substring of a more specific sibling clause.
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class SupertypeSubjectDoesntUntapDuringControllersUntapStepsRule : IStaticRule
{
  // Anchored full-sentence match:
  // "<Supertype> Lands|Creatures|Artifacts|Permanents don't untap during
  // their controllers' untap steps."
  // Apostrophe class tolerates both the straight (U+0027) and curly (U+2019)
  // apostrophe forms used across printings.
  private static readonly Regex _pattern = new(
    @"^\s*(?<supertype>Snow|Legendary|Basic|World)\s+(?<subject>Lands|Creatures|Artifacts|Permanents)\s+don[’']t\s+untap\s+during\s+their\s+controllers[’']?\s+untap\s+steps\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  private static readonly IReadOnlyDictionary<string, string> _subjectToCardType =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Lands"] = "land",
      ["Creatures"] = "creature",
      ["Artifacts"] = "artifact",
      ["Permanents"] = "permanent",
    };

  private static readonly IReadOnlyDictionary<string, string> _supertypeNameToValue =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Snow"] = "Snow",
      ["Legendary"] = "Legendary",
      ["Basic"] = "Basic",
      ["World"] = "World",
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var subject = match.Groups["subject"].Value;
    var supertypeWord = match.Groups["supertype"].Value;

    if (!_subjectToCardType.TryGetValue(subject, out var cardType) ||
        !_supertypeNameToValue.TryGetValue(supertypeWord, out var supertype))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new DoesntUntapEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { CardTypes = [cardType], Supertypes = [supertype] },
            },
            WhoseUntapStep = "their controllers'",
          },
        ],
      },
    ];
  }
}
