namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// Parses the Ygra-family static ability template:
/// "Other creatures are [Subtype] [card-types] in addition to their other types and have
/// &quot;[quoted activated ability]&quot;."
///
/// <para>
/// This oracle template combines a layer-4 additive type grant (CR 205.1a) with a layer-6
/// ability grant (CR 613.1f) in a single printed sentence. MAST represents this as ONE
/// <see cref="StaticAbility"/> carrying two effects in order:
/// <list type="number">
///   <item><see cref="AddTypeEffect"/> — grants the named card types and subtypes.</item>
///   <item><see cref="GainAbilityEffect"/> — grants the quoted activated ability.</item>
/// </list>
/// Both effects share the same target filter ("Other creatures" = all creatures excluding
/// the source, <see cref="ObjectFilter.ExcludeSelf"/> = true, no controller restriction
/// — this affects ALL creatures on the battlefield except the source).
/// </para>
///
/// <para>
/// Example oracle text: "Other creatures are Food artifacts in addition to their other
/// types and have &quot;{2}, {T}, Sacrifice this permanent: You gain 3 life.&quot;"
/// (Ygra, Eater of All, BLB).
/// </para>
///
/// <para>
/// CR 205.1a (verbatim): "Some effects change an object's card type, subtype, and/or
/// supertype but specify that the object retains a prior card type, subtype, and/or
/// supertype."
/// CR 613.1d (layer 4 — type-changing effects), CR 613.1f (layer 6 — ability-granting).
/// CR 111.10b: "A Food token is a colorless Food artifact token with '{2}, {T}, Sacrifice
/// this token: You gain 3 life.'" (The CR definition of the Food subtype.)
/// </para>
///
/// <para>
/// Priority 968 — below the controller-scoped type-grant rules (969–970) and above the
/// fallback anthem rules so this dedicated shape fires before the generic type-grant rules
/// decline it (they require "you control"). Anchored (^…$) to prevent substring overlap.
/// </para>
/// </summary>
[StaticRule(Priority = 968)]
public sealed class OtherCreaturesAreSubtypeAndHaveAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // Matches: "Other creatures are <types> in addition to their other types and have "<body>"."
  // <types>: one or more space-separated type/subtype tokens (e.g. "Food artifacts").
  // <body>: the quoted activated ability text (straight double-quote or curly quotes).
  // Anchored at both ends. IgnoreCase for robustness.
  private static readonly Regex _pattern = new(
    "^\\s*Other\\s+creatures\\s+are\\s+"
    + "(?<types>[A-Za-z](?:[A-Za-z ]*[A-Za-z])?)"
    + "\\s+in\\s+addition\\s+to\\s+their\\s+other\\s+types\\s+and\\s+have\\s+"
    + "[\"\\u201C\\u201D](?<body>[^\"\\u201C\\u201D]+)[\"\\u201C\\u201D]\\.?\\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known card types (CR 205.2) for classifying type tokens.
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

    var typesRaw = m.Groups["types"].Value.Trim();
    var body = m.Groups["body"].Value.Trim();

    if (body.Length == 0)
    {
      return null;
    }

    var (addedCardTypes, addedSubtypes) = ClassifyTypeTokens(typesRaw);
    if (addedCardTypes is null && addedSubtypes is null)
    {
      return null;
    }

    // Parse the quoted ability body as an activated ability.
    var grantedAbility = TryParseActivatedBody(body);
    if (grantedAbility is null)
    {
      return null;
    }

    // Target: "Other creatures" = all creatures except the source (no controller filter).
    // ExcludeSelf = true encodes the "Other" qualifier (CR 109.5 — excludes the source object).
    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        ExcludeSelf = true,
      },
    };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new AddTypeEffect
          {
            Target = target,
            AddedCardTypes = addedCardTypes,
            AddedSubtypes = addedSubtypes,
          },
          new GainAbilityEffect
          {
            Target = target,
            GainedAbility = grantedAbility,
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Splits a space-separated type list (e.g. "Food artifacts") into the card-type
  /// and subtype buckets. Returns (null, null) for unrecognised input.
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

    var cardTypes = new List<string>();
    var subtypes = new List<string>();

    foreach (var token in tokens)
    {
      // Strip trailing plural 's' for classification ("artifacts" → "artifact").
      var singular = token.TrimEnd('s');

      if (_cardTypes.Contains(token) || _cardTypes.Contains(singular))
      {
        // Card type token — lowercase to match ObjectFilter.CardTypes convention.
        var canonical = _cardTypes.Contains(token) ? token : singular;
        cardTypes.Add(canonical.ToLowerInvariant());
      }
      else if (char.IsUpper(token[0]))
      {
        // Capitalised non-card-type token → artifact subtype (e.g. "Food", "Treasure").
        // Subtypes are proper-noun-ish (CR 205.3) and PascalCase by convention.
        // Singularize trailing 's' for subtypes too (defensive).
        var sub = singular.Length > 0 && char.IsUpper(singular[0]) ? singular : token;
        subtypes.Add(char.ToUpperInvariant(sub[0]) + sub[1..]);
      }
      else
      {
        // Unrecognised token — cannot produce a structured node; fall through.
        return (null, null);
      }
    }

    return (
      cardTypes.Count > 0 ? cardTypes : null,
      subtypes.Count > 0 ? subtypes : null
    );
  }

  private Ability? TryParseActivatedBody(string body)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue
      ? tokenResult.Value
      : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(0, body.Length),
    };
    var innerClassification = new ClauseClassification
    {
      Kind = AbilityKind.Activated,
      Confidence = 1.0,
    };

    return new ActivatedAbilityParser().TryParse(innerClause, innerClassification);
  }
}
