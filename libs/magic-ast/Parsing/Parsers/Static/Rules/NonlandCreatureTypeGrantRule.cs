namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Ashaya-family oracle template: a static continuous effect that
/// additively grants one or more card types and subtypes to a group of permanents
/// "in addition to their other types."
///
/// <para>
/// Handled pattern:
/// <list type="bullet">
///   <item>"Nontoken creatures you control are Forest lands in addition to their other types."
///     → <see cref="AddTypeEffect"/> with Target={Each, nontoken creatures you control},
///     AddedCardTypes=["land"], AddedSubtypes=["Forest"].</item>
///   <item>"Nontoken artifacts you control are lands in addition to their other types."
///     (Toph, the First Metalbender) → <see cref="AddTypeEffect"/> with
///     Target={Each, nontoken artifacts you control}, AddedCardTypes=["land"].</item>
/// </list>
/// </para>
///
/// <para>
/// CR 205.1b (verbatim): "Some effects change an object's card type, subtype, and/or
/// supertype but specify that the object retains a prior card type, subtype, and/or
/// supertype. In such cases, all the object's prior card types, subtypes, and supertypes
/// are retained, and the effect causes the object to gain or lose other card types,
/// subtypes, and/or supertypes."
/// </para>
///
/// <para>
/// Priority 970 — below the P/T-defining rules and keyword lists (976–1000) and above
/// the broad grant/anthem fallbacks (967–968), so this dedicated shape fires first
/// against the "nontoken creatures ... are [type]" template.
/// </para>
/// </summary>
[StaticRule(Priority = 970)]
public sealed class NonlandCreatureTypeGrantRule : IStaticRule
{
  // "<Subject> are <types> in addition to their other types."
  // Subject: "Nontoken creatures you control" (and similar "Noncreature permanents you
  // control", "Creatures you control", etc.) — currently only the Nontoken-creature
  // shape is structured; the filter is built in TryBuildSubjectFilter.
  // Types: one or more space-separated type tokens before "in addition to their other types".
  // A trailing parenthetical reminder is tolerated.
  private static readonly Regex _pattern = new(
    @"^\s*(?<subject>.+?)\s+are\s+(?<types>[A-Za-z](?:[A-Za-z ]*[A-Za-z])?)\s+in\s+addition\s+to\s+their\s+other\s+types\.?\s*(?<reminder>\([^)]+\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known basic-land subtypes (CR 305.6).
  private static readonly HashSet<string> _basicLandSubtypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "Plains", "Island", "Swamp", "Mountain", "Forest",
  };

  // Known card types (CR 205.2).
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

  // "Nontoken <cardType>(s) you control" — IsToken:false, CardTypes:[<cardType>],
  // Controller:You. Anchored to this exact "Nontoken <type> you control" shape (no
  // free-floating substring match) so it cannot collide with a differently-scoped
  // subject clause elsewhere in the corpus. Currently recognises "creature" (Ashaya,
  // Soul of the Wild) and "artifact" (Toph, the First Metalbender).
  private static readonly Regex _nontokenSubjectPattern = new(
    @"^Nontoken (?<type>creatures?|artifacts?) you control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Builds an <see cref="ObjectReference"/> for the subject noun phrase.
  /// Currently handles: "Nontoken creatures you control" and "Nontoken artifacts you
  /// control" (singular or plural). Returns null for unrecognised subjects.
  /// </summary>
  private static ObjectReference? TryBuildSubjectFilter(string subject)
  {
    var m = _nontokenSubjectPattern.Match(subject.Trim());
    if (!m.Success)
    {
      return null;
    }

    var cardType = m.Groups["type"].Value.TrimEnd('s').ToLowerInvariant();

    return new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = [cardType],
        IsToken = false,
        Controller = ControllerFilter.You,
      },
    };
  }

  /// <summary>
  /// Splits a space-separated type list (e.g. "Forest lands", "artifact creatures")
  /// into the card-type and subtype buckets. Returns (null, null) for unrecognised
  /// input so the rule falls through gracefully.
  /// </summary>
  private static (IReadOnlyList<string>? CardTypes, IReadOnlyList<string>? Subtypes) ClassifyTypeTokens(
    string typesRaw
  )
  {
    // Tokenise by whitespace.
    var tokens = typesRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens.Length == 0)
    {
      return (null, null);
    }

    var addedCardTypes = new List<string>();
    var addedSubtypes = new List<string>();

    foreach (var token in tokens)
    {
      // Strip plural 's' for classification ("lands" → "land", "Forests" → "Forest").
      var singular = token.TrimEnd('s');

      if (_cardTypes.Contains(token) || _cardTypes.Contains(singular))
      {
        // Card type token — lowercase to match ObjectFilter.CardTypes convention.
        var canonical = _cardTypes.Contains(token) ? token : singular;
        addedCardTypes.Add(canonical.ToLowerInvariant());
      }
      else if (_basicLandSubtypes.Contains(token) || _basicLandSubtypes.Contains(singular))
      {
        // Land-subtype token — PascalCase to match ObjectFilter.Subtypes convention.
        var canonical = _basicLandSubtypes.Contains(token) ? token : singular;
        // Return as PascalCase (the set already stores PascalCase).
        addedSubtypes.Add(char.ToUpperInvariant(canonical[0]) + canonical[1..].ToLowerInvariant());
      }
      else
      {
        // Unrecognised token — cannot produce a structured node; fall through.
        return (null, null);
      }
    }

    return (
      addedCardTypes.Count > 0 ? addedCardTypes : null,
      addedSubtypes.Count > 0 ? addedSubtypes : null
    );
  }
}
