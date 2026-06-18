namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Mycosynth Lattice "all permanents are [type] in addition to their other
/// types" oracle template: a static continuous effect that additively grants one or
/// more card types to all permanents (or a broadly-scoped subject without controller
/// qualification).
///
/// <para>
/// Handled patterns:
/// <list type="bullet">
///   <item>"All permanents are artifacts in addition to their other types."
///     → <see cref="AddTypeEffect"/> with Target={Each, permanents}, AddedCardTypes=["artifact"].</item>
/// </list>
/// </para>
///
/// <para>
/// CR 205.1b (verbatim): "Some effects change an object's card type, supertype, or
/// subtype but specify that the object retains a prior card type, supertype, or
/// subtype. In such cases, all the object's prior card types, supertypes, and
/// subtypes are retained. This rule applies to effects that use phrases such as
/// 'in addition to its other types' or that state that something is 'still a
/// [type, supertype, or subtype].'"
/// </para>
///
/// <para>
/// Priority 969 — one below <see cref="NonlandCreatureTypeGrantRule"/> (970) which
/// handles the more-specific controller-scoped shape, so this broader rule fires
/// only when that rule declines (i.e., the subject is not "Nontoken creatures you
/// control").
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class AllObjectsAddTypeRule : IStaticRule
{
  // "All <subject> are <types> in addition to their other types."
  // Subject: "permanents", and future broad groups without controller qualification.
  private static readonly Regex _pattern = new(
    @"^\s*All\s+(?<subject>[A-Za-z][A-Za-z ]*[A-Za-z])\s+are\s+(?<types>[A-Za-z](?:[A-Za-z ]*[A-Za-z])?)\s+in\s+addition\s+to\s+their\s+other\s+types\.?\s*(?<reminder>\([^)]+\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known card types (CR 205.2) for type-token classification.
  private static readonly HashSet<string> _cardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "Artifact", "Battle", "Conspiracy", "Creature", "Enchantment", "Instant",
    "Land", "Phenomenon", "Plane", "Planeswalker", "Scheme", "Sorcery",
    "Tribal", "Vanguard",
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var subject = m.Groups["subject"].Value.Trim();
    var typesRaw = m.Groups["types"].Value.Trim();
    var reminderRaw = m.Groups["reminder"].Value.Trim();

    var target = TryBuildSubjectFilter(subject);
    if (target is null)
    {
      return null;
    }

    var (addedCardTypes, addedSubtypes) = ClassifyTypeTokens(typesRaw);
    if (addedCardTypes is null && addedSubtypes is null)
    {
      return null;
    }

    Parenthetical? reminder = string.IsNullOrEmpty(reminderRaw)
      ? null
      : new Parenthetical { Text = reminderRaw };

    return
    [
      new StaticAbility
      {
        Reminder = reminder,
        Effects = [new AddTypeEffect
        {
          Target = target,
          AddedCardTypes = addedCardTypes,
          AddedSubtypes = addedSubtypes,
        }],
      },
    ];
  }

  /// <summary>
  /// Builds an <see cref="ObjectReference"/> for a broad "All [group]" subject.
  /// Handles groups that do not include a controller qualifier.
  /// </summary>
  private static ObjectReference? TryBuildSubjectFilter(string subject)
  {
    // "permanents" — all permanents (CardTypes: ["permanent"], no controller)
    if (subject.Equals("permanents", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
        },
      };
    }

    return null;
  }

  /// <summary>
  /// Splits a space-separated type list (e.g. "artifacts", "artifact creatures")
  /// into the card-type and subtype buckets.
  /// </summary>
  private static (IReadOnlyList<string>? CardTypes, IReadOnlyList<string>? Subtypes) ClassifyTypeTokens(
    string typesRaw
  )
  {
    var tokens = typesRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens.Length == 0)
    {
      return (null, null);
    }

    var addedCardTypes = new List<string>();

    foreach (var token in tokens)
    {
      // Strip plural 's' for classification ("artifacts" → "artifact").
      var singular = token.TrimEnd('s');

      if (_cardTypes.Contains(token) || _cardTypes.Contains(singular))
      {
        var canonical = _cardTypes.Contains(token) ? token : singular;
        addedCardTypes.Add(canonical.ToLowerInvariant());
      }
      else
      {
        return (null, null);
      }
    }

    return (
      addedCardTypes.Count > 0 ? addedCardTypes : null,
      null
    );
  }
}
