namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Combinators;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;

/// <summary>
/// Parser for static abilities using token-based combinators.
/// Handles keyword abilities (Flying, Vigilance, etc.) and other static effects.
/// </summary>
/// <remarks>
/// This parser uses monadic combinators from OracleParsers to parse keywords
/// directly from token sequences, avoiding string manipulation.
/// </remarks>
[OracleAbilityParser(AbilityKind.Static)]
public sealed class StaticAbilityParser : IAbilityParser
{
  private readonly OracleTokenizer _tokenizer = new();
  private readonly FallbackParser _fallback = new();

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var parsed = TryParse(clause, classification);
    if (parsed is { Count: > 0 })
    {
      return parsed;
    }
    return
    [
      _fallback.Parse(clause, classification, "Static ability parser not yet implemented"),
    ];
  }

  /// <summary>
  /// Attempts to parse static abilities from a clause.
  /// Returns a list of StaticAbility nodes (one per keyword or effect).
  /// </summary>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var tokens = clause.Tokens;

    // Try parsing as keyword list using token combinators
    var keywordAbilities = TryParseKeywordList(tokens);
    if (keywordAbilities != null && keywordAbilities.Count > 0)
    {
      return keywordAbilities;
    }

    // "[Self] attacks each combat if able." — Rule 508-style attack requirement.
    // Descriptive: records that the oracle line imposes a must-attack restriction
    // on the named object. Does not model runtime enforcement.
    var mustAttack = TryParseMustAttack(clause);
    if (mustAttack != null)
    {
      return mustAttack;
    }

    // "[filter] has \"[activated ability]\"." — Aura-style ability grant on the
    // enchanted/equipped object (Rule 113.6/113.10). The inner activated
    // ability is parsed by reusing ActivatedAbilityParser, so the recursive
    // Ability shape stays consistent with how the same ability would be
    // modeled if it appeared directly on a card.
    var grantedAbility = TryParseGrantedAbility(clause, classification);
    if (grantedAbility != null)
    {
      return grantedAbility;
    }

    // Try other static ability patterns
    // TODO: Add more patterns as needed:
    // - "Enchant [descriptor]"
    // - "This spell can't be countered"
    // - "This [permanent] doesn't untap during your untap step"
    // - Replacement effects

    return null;
  }

  /// <summary>
  /// Recognizes "[Self] attacks each combat if able." where [Self] is either
  /// the literal phrase "This creature"/"This permanent" or the card's own name
  /// (any leading word(s) before "attacks"). Produces a <see cref="StaticAbility"/>
  /// wrapping a <see cref="MustAttackEffect"/> targeting <c>Self</c>.
  /// </summary>
  /// <remarks>
  /// Card-name-as-subject is the standard oracle-text convention for self-reference
  /// in continuous abilities on a named permanent — the parser treats any leading
  /// word(s) before <c>attacks</c> as a synonym for <c>Self</c> when the rest of the
  /// line matches the restriction phrase.
  /// </remarks>
  private static IReadOnlyList<Ability>? TryParseMustAttack(OracleClause clause)
  {
    if (!_mustAttackPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effect = new MustAttackEffect { Target = ObjectReference.Self() },
      },
    ];
  }

  private static readonly Regex _mustAttackPattern = new(
    @"^\s*\S.*?\s+attacks\s+each\s+combat\s+if\s+able\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Recognizes lines of the form
  /// <c>[filter] has "[activated-ability text]"</c> — the Aura-style oracle
  /// shape for granting a permanent an activated ability (Rule 113.6/113.10).
  /// </summary>
  /// <remarks>
  /// The granted body is re-tokenized and handed to
  /// <see cref="ActivatedAbilityParser"/>, so the recursive
  /// <see cref="GainAbilityEffect.GainedAbility"/> matches the same shape
  /// the inner ability would have if it appeared directly on a card.
  /// We only return a hit when the inner parse succeeds — otherwise we
  /// fall through and let the fallback path surface a structured failure.
  /// </remarks>
  private IReadOnlyList<Ability>? TryParseGrantedAbility(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    var match = _grantedAbilityPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    var (target, affectedObjects) = ClassifyGrantTarget(filterText);
    if (target is null)
    {
      return null;
    }

    var innerAbility = TryParseGrantedBody(body);
    if (innerAbility is null)
    {
      // The body's shape isn't yet supported by ActivatedAbilityParser.
      // Surface as a parser miss — the fallback path will record the gap.
      return null;
    }

    return
    [
      new StaticAbility
      {
        AffectedObjects = affectedObjects,
        Effect = new GainAbilityEffect
        {
          Target = target,
          GainedAbility = innerAbility,
        },
      },
    ];
  }

  /// <summary>
  /// Hands the quoted body off to <see cref="ActivatedAbilityParser"/>.
  /// </summary>
  private Ability? TryParseGrantedBody(string body)
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

    var inner = new ActivatedAbilityParser().TryParse(innerClause, innerClassification);
    return inner;
  }

  /// <summary>
  /// Maps the noun-phrase left of "has" onto an ObjectReference target and
  /// the corresponding <c>AffectedObjects</c> filter for the wrapping
  /// <see cref="StaticAbility"/>. Today only the Aura-vocabulary phrases
  /// ("enchanted [type]") are recognized; equipped/etc. land naturally
  /// when their analogues come up.
  /// </summary>
  private static (ObjectReference? target, ObjectFilter? affectedObjects) ClassifyGrantTarget(
    string filterText
  )
  {
    var lower = filterText.ToLowerInvariant();
    if (lower.StartsWith("enchanted ") || lower.StartsWith("equipped "))
    {
      return (
        new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
        new ObjectFilter { Characteristics = [lower] }
      );
    }
    return (null, null);
  }

  // Anchors a single clause (no in-line newlines reach this layer — clauses
  // are split before us). Captures the noun-phrase subject and the quoted body
  // verbatim; nested quotes inside the body are unlikely in oracle text and
  // are out of scope for this first cut.
  private static readonly Regex _grantedAbilityPattern = new(
    @"^\s*(?<filter>[^""""]+?)\s+has\s+[""""](?<body>[^""""]+)[""""]\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  #region Keyword Parsing

  /// <summary>
  /// Parses comma-separated keyword abilities using token combinators.
  /// Example: "Flying, first strike, lifelink" → 3 separate StaticAbility nodes
  /// </summary>
  private IReadOnlyList<Ability>? TryParseKeywordList(TokenList<OracleToken> tokens)
  {
    // Try to parse using the OracleParsers.KeywordList combinator
    var parseResult = OracleParsers.KeywordList(tokens);

    if (!parseResult.HasValue)
    {
      return null;
    }

    // Convert StaticAbility[] to IReadOnlyList<Ability>
    return parseResult.Value.Cast<Ability>().ToList();
  }

  #endregion
}
