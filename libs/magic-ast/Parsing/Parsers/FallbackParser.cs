namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.Diagnostics;
using MagicAST.Parsing.Tokens;
using Superpower.Model;
// Use our own TextSpan, not Superpower's
using TextSpan = MagicAST.AST.TextSpan;

/// <summary>
/// Fallback parser that always succeeds by producing an UnparsedAbility.
/// This ensures the parsing pipeline never fails completely.
///
/// Registered for <see cref="AbilityKind.Unparsed"/> in the
/// <see cref="AbilityParserRegistry"/>. Also invoked internally by the other
/// concrete parsers when their <c>TryParse</c> step fails.
/// </summary>
[OracleAbilityParser(AbilityKind.Unparsed)]
public sealed class FallbackParser : IAbilityParser
{
  /// <summary>
  /// Creates an UnparsedAbility from a clause that couldn't be parsed.
  /// </summary>
  /// <param name="clause">The clause that failed to parse.</param>
  /// <param name="classification">The classification attempt.</param>
  /// <param name="error">Optional error message from the parsing attempt.</param>
  /// <param name="expected">Optional expected tokens/patterns.</param>
  /// <param name="lastAttemptedRule">
  /// Optional name of the parser rule that came closest to matching before
  /// dispatching to fallback. Convention: <c>"{ParserClassName}.{MethodName}"</c>.
  /// When omitted, defaults to <c>"FallbackParser.Parse"</c> — i.e. the
  /// classification routed directly to the fallback with no upstream rule attempt.
  /// </param>
  /// <param name="failurePosition">
  /// Optional character offset within the source oracle text at which the
  /// parser bailed. When omitted, defaults to <c>clause.SourceSpan.Start</c>.
  /// </param>
  /// <returns>An UnparsedAbility node with diagnostics.</returns>
  public UnparsedAbility Parse(
    OracleClause clause,
    ClauseClassification classification,
    string? error = null,
    IReadOnlyList<string>? expected = null,
    string? lastAttemptedRule = null,
    int? failurePosition = null
  )
  {
    var diagnostic = CreateDiagnostic(
      clause,
      classification,
      error,
      expected,
      lastAttemptedRule ?? "FallbackParser.Parse",
      failurePosition ?? clause.SourceSpan.Start
    );

    return new UnparsedAbility
    {
      SourceSpan = clause.SourceSpan,
      RawText = clause.RawText,
      Diagnostics = [diagnostic],
      AbilityWord = classification.AbilityWord,
    };
  }

  /// <inheritdoc/>
  IReadOnlyList<Ability> IAbilityParser.Parse(
    OracleClause clause,
    ClauseClassification classification
  ) => [Parse(clause, classification)];

  /// <summary>
  /// Creates a diagnostic for parse failure.
  /// </summary>
  private static Diagnostic CreateDiagnostic(
    OracleClause clause,
    ClauseClassification classification,
    string? error,
    IReadOnlyList<string>? expected,
    string lastAttemptedRule,
    int failurePosition
  )
  {
    var pattern = InferFailurePattern(clause, classification);
    var message = error ?? $"Failed to parse {classification.Kind} ability";

    return new Diagnostic
    {
      Severity = DiagnosticSeverity.Error,
      Message = message,
      Location = clause.SourceSpan,
      Expected = expected,
      RawText = clause.RawText,
      Pattern = pattern,
      LastAttemptedRule = lastAttemptedRule,
      FailurePosition = failurePosition,
    };
  }

  // --- Line-split artifact patterns (multi-line abilities whose body lines
  //     get individually re-presented to the parser by the triage flow's
  //     per-line splitter). Tag and de-emphasize — NOT actionable as parser
  //     work; they're already consumed at the ability level.
  private static readonly Regex LevelUpStanzaBodyPt = new(@"^\d+/\d+$", RegexOptions.Compiled);
  private static readonly Regex LevelUpStanzaHeader = new(
    @"^level\s+\d+(\s*[-+–]\s*\d+|\+)?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // --- Spell sub-patterns (UnparsedSpell partitioning).
  private static readonly Regex CounterSpellLead = new(
    @"^counter\s+target",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex DestroyTargetLead = new(
    @"^destroy\s+(target|all)",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex ExileTargetLead = new(
    @"^exile\s+(target|all)",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex ReturnTargetLead = new(
    @"^return\s+(target|all)",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex DealDamageLead = new(
    @"^\S+\s+deals?\s+\w+\s+damage\s+to",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex CreateTokenLead = new(
    @"^create\s+\S+\s+(\d+/\d+|[a-z]+\s+\d+/\d+).*\btoken",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // --- Triggered sub-patterns (UnparsedTriggered partitioning). Match the
  //     trigger condition, not the effect — that's what differentiates the
  //     parser surface that would handle this family.
  private static readonly Regex EnterTrigger = new(
    @"^(when|whenever)\s+\S+\s+enters\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex DieTrigger = new(
    @"^(when|whenever)\s+\S+\s+dies\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex AttackTrigger = new(
    @"^(when|whenever)\s+\S+\s+attacks?\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex CombatDamageTrigger = new(
    @"^(when|whenever)\s+\S+\s+deals?\s+combat\s+damage\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex BeginningOfTurnTrigger = new(
    @"^at\s+the\s+beginning\s+of\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // --- Static sub-patterns (UnparsedStatic partitioning).
  private static readonly Regex LordPtBuff = new(
    @"\bget\s+[+-]\d+/[+-]\d+\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex ProtectionFromStatic = new(
    @"\bprotection\s+from\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
  private static readonly Regex AsLongAsStatic = new(
    @"\bas\s+long\s+as\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <summary>
  /// Infers a failure pattern category for aggregation. Ordering matters:
  /// more-specific structural patterns first, then real-failure sub-patterns
  /// keyed off the inferred ability kind, then the confidence-based catchall.
  /// </summary>
  /// <remarks>
  /// Sub-patterns are grouped by which parser surface would close them so the
  /// triage aggregator can present family-contract candidates directly. If a
  /// new sub-pattern doesn't map to a single parser rule shape, don't add it
  /// here — keep the catchall doing its job.
  /// </remarks>
  private static string InferFailurePattern(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    var tokens = clause.Tokens.ToList();
    var raw = clause.RawText;
    var trimmed = raw.Trim();
    var text = raw.ToLowerInvariant();

    // ─── Line-split artifacts (de-emphasize, NOT parser work) ───
    // Level Up stanza body fragments — bare "3/3", "5/5" or "LEVEL 1-2", "LEVEL 7+".
    // The whole stanza is consumed by LevelUpAbilityParser at ability scope;
    // the triage line-splitter treats each body line as an unparseable clause.
    if (LevelUpStanzaBodyPt.IsMatch(trimmed))
    {
      return "LevelUpStanzaBody";
    }
    if (LevelUpStanzaHeader.IsMatch(trimmed))
    {
      return "LevelUpStanzaBody";
    }
    // Modal option fragments — "• Scry 1.", "• Draw a card." — body of a
    // Modal/Choose-one ability that gets line-split by the triage flow.
    if (trimmed.StartsWith('•'))
    {
      return "ModalOptionFragment";
    }

    // ─── Existing structural patterns (preserved) ───
    if (text.StartsWith("level up"))
    {
      return "LevelUp";
    }
    if (text.Contains("class level"))
    {
      return "ClassLevel";
    }
    if (text.StartsWith("(as this saga enters") || text.Contains("read ahead"))
    {
      return "Saga";
    }
    if (text.StartsWith("partner with"))
    {
      return "PartnerWith";
    }
    if (clause.Tokens.Any(t => t.Kind == OracleToken.QuotedText))
    {
      return "NestedAbility";
    }
    if (text.Contains("target") && text.Contains("or"))
    {
      return "ComplexTargeting";
    }
    if (text.Contains("if you") || text.Contains("if a") || text.Contains("if an"))
    {
      return "ConditionalEffect";
    }
    if (tokens.Any(t => t.Kind == OracleToken.VariableMana))
    {
      return "VariableEffect";
    }

    // ─── Sub-pattern partitioning by inferred ability kind ───
    // Each sub-pattern below corresponds to ONE plausible parser rule surface
    // (one [SpellRule], [TriggeredRule], etc.) that would close the family.
    var kind = classification.Kind;

    if (kind == AbilityKind.Spell)
    {
      if (CounterSpellLead.IsMatch(text))
        return "CounterSpell";
      if (DestroyTargetLead.IsMatch(text))
        return "DestroyTargetSpell";
      if (ExileTargetLead.IsMatch(text))
        return "ExileTargetSpell";
      if (ReturnTargetLead.IsMatch(text))
        return "ReturnTargetSpell";
      if (DealDamageLead.IsMatch(text))
        return "DealDamageSpell";
      if (CreateTokenLead.IsMatch(text))
        return "CreateTokenSpell";
    }

    if (kind == AbilityKind.Triggered)
    {
      if (EnterTrigger.IsMatch(text))
        return "EnterTrigger";
      if (DieTrigger.IsMatch(text))
        return "DieTrigger";
      if (AttackTrigger.IsMatch(text))
        return "AttackTrigger";
      if (CombatDamageTrigger.IsMatch(text))
        return "CombatDamageTrigger";
      if (BeginningOfTurnTrigger.IsMatch(text))
        return "BeginningOfTurnTrigger";
    }

    if (kind == AbilityKind.Static)
    {
      if (LordPtBuff.IsMatch(text))
        return "LordPowerToughnessBuff";
      if (ProtectionFromStatic.IsMatch(text))
        return "ProtectionFrom";
      if (AsLongAsStatic.IsMatch(text))
        return "AsLongAsCondition";
    }

    // ─── Confidence-based catchall ───
    return classification.Confidence switch
    {
      < 0.5 => "UnknownStructure",
      < 0.7 => "AmbiguousStructure",
      _ => $"Unparsed{kind}",
    };
  }
}
