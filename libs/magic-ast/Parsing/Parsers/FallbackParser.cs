namespace MagicAST.Parsing.Parsers;

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
/// </summary>
public sealed class FallbackParser
{
  /// <summary>
  /// Creates an UnparsedAbility from a clause that couldn't be parsed.
  /// </summary>
  /// <param name="clause">The clause that failed to parse.</param>
  /// <param name="classification">The classification attempt.</param>
  /// <param name="error">Optional error message from the parsing attempt.</param>
  /// <param name="expected">Optional expected tokens/patterns.</param>
  /// <returns>An UnparsedAbility node with diagnostics.</returns>
  public UnparsedAbility Parse(
    OracleClause clause,
    ClauseClassification classification,
    string? error = null,
    IReadOnlyList<string>? expected = null
  )
  {
    var diagnostic = CreateDiagnostic(clause, classification, error, expected);

    return new UnparsedAbility
    {
      SourceSpan = clause.SourceSpan,
      RawText = clause.RawText,
      Diagnostics = [diagnostic],
      AbilityWord = classification.AbilityWord,
    };
  }

  /// <summary>
  /// Creates a diagnostic for parse failure.
  /// </summary>
  private static Diagnostic CreateDiagnostic(
    OracleClause clause,
    ClauseClassification classification,
    string? error,
    IReadOnlyList<string>? expected
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
    };
  }

  /// <summary>
  /// Infers a failure pattern category for aggregation.
  /// </summary>
  private static string InferFailurePattern(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    var tokens = clause.Tokens.ToList();
    var text = clause.RawText.ToLowerInvariant();

    // Check for common patterns that need specific parser support

    // Level up cards
    if (text.StartsWith("level up"))
    {
      return "LevelUp";
    }

    // Class cards
    if (text.Contains("class level"))
    {
      return "ClassLevel";
    }

    // Saga cards
    if (text.StartsWith("(as this saga enters") || text.Contains("read ahead"))
    {
      return "Saga";
    }

    // Partner with
    if (text.StartsWith("partner with"))
    {
      return "PartnerWith";
    }

    // Nested/quoted abilities
    if (clause.Tokens.Any(t => t.Kind == OracleToken.QuotedText))
    {
      return "NestedAbility";
    }

    // Complex targeting
    if (text.Contains("target") && text.Contains("or"))
    {
      return "ComplexTargeting";
    }

    // Conditional effects
    if (text.Contains("if you") || text.Contains("if a") || text.Contains("if an"))
    {
      return "ConditionalEffect";
    }

    // X spells/effects
    if (tokens.Any(t => t.Kind == OracleToken.VariableMana))
    {
      return "VariableEffect";
    }

    // Based on classification confidence
    return classification.Confidence switch
    {
      < 0.5 => "UnknownStructure",
      < 0.7 => "AmbiguousStructure",
      _ => $"Unparsed{classification.Kind}",
    };
  }
}
