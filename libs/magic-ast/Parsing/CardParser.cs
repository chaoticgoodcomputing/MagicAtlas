namespace MagicAST.Parsing;

using System.Diagnostics;
using System.Linq;
using MagicAST.AST;
using MagicAST.Analysis;
using MagicAST.Diagnostics;

/// <summary>
/// Main entry point for parsing Magic: The Gathering cards.
/// Orchestrates type line parsing, oracle text parsing, and attribute extraction.
/// </summary>
/// <remarks>
/// The parsing pipeline:
/// 1. TypeLineParser: Parse type line into supertypes/types/subtypes
/// 2. OracleParser: Parse oracle text into abilities
/// 3. AttributeExtractor: Extract mana cost, colors, stats, etc.
/// 4. Combine into CardOutputAST
///
/// For multi-faced cards (split, transform, etc.), each face is parsed separately.
/// </remarks>
public sealed class CardParser
{
  private readonly TypeLineParser _typeLineParser = new();
  private readonly OracleParser _oracleParser = new();
  private readonly AttributeExtractor _attributeExtractor = new();

  /// <summary>
  /// Parses a card from its input DTO into a full output AST.
  /// </summary>
  /// <param name="input">The card input DTO.</param>
  /// <returns>A CardParseResult containing the AST and diagnostics.</returns>
  public CardParseResult Parse(CardInputDTO input)
  {
    ArgumentNullException.ThrowIfNull(input);

    var stopwatch = Stopwatch.StartNew();
    var diagnostics = new List<Diagnostic>();

    // Parse type line
    var typeLine = _typeLineParser.Parse(input.TypeLine);

    // Parse oracle text
    var oracleResult = _oracleParser.Parse(input.OracleText);
    diagnostics.AddRange(oracleResult.Diagnostics);

    // Correct the self-by-name reference type now that the type line is known (the oracle parser
    // has no type-line access, so its self-by-name branch hardcodes "creature"). CR 201.5.
    var correctedOracle = SelfReferenceTypeCorrector.Correct(oracleResult.Output, typeLine);

    // Extract attributes
    var attributes = _attributeExtractor.Extract(input);

    // Handle multi-faced cards
    IReadOnlyList<CardFaceAST>? faces = null;
    if (input.CardFaces is { Count: > 0 })
    {
      var faceList = new List<CardFaceAST>();
      foreach (var faceInput in input.CardFaces)
      {
        var (face, faceDiagnostics) = ParseFace(faceInput);
        faceList.Add(face);
        diagnostics.AddRange(faceDiagnostics);
      }
      faces = faceList;
    }

    stopwatch.Stop();

    var output = new CardOutputAST
    {
      Name = input.Name,
      TypeLine = typeLine,
      Oracle = correctedOracle,
      Attributes = attributes,
      Faces = faces,
    };

    return new CardParseResult
    {
      Output = output,
      Status = oracleResult.Status,
      Diagnostics = diagnostics,
      Metrics = new CardParseMetrics
      {
        TotalAbilities = oracleResult.Metrics.TotalAbilities,
        ParsedAbilities = oracleResult.Metrics.ParsedAbilities,
        FailedAbilities = oracleResult.Metrics.FailedAbilities,
        DurationMs = stopwatch.Elapsed.TotalMilliseconds,
        FaceCount = faces?.Count ?? 0,
        // Walk the whole output (main oracle + every face) so residual debt is
        // counted card-wide in one pass.
        ResidualCounts = ResidualWalker.Count(output),
      },
    };
  }

  /// <summary>
  /// Parses a single card face.
  /// </summary>
  private (CardFaceAST Face, IReadOnlyList<Diagnostic> Diagnostics) ParseFace(CardFaceDTO faceInput)
  {
    var typeLine = _typeLineParser.Parse(faceInput.TypeLine);
    var oracleResult = _oracleParser.Parse(faceInput.OracleText);
    var attributes = _attributeExtractor.ExtractFromFace(faceInput);

    var face = new CardFaceAST
    {
      Name = faceInput.Name,
      TypeLine = typeLine,
      Oracle = SelfReferenceTypeCorrector.Correct(oracleResult.Output, typeLine),
      Attributes = attributes,
    };

    return (face, oracleResult.Diagnostics);
  }
}

/// <summary>
/// Result of parsing a complete card.
/// </summary>
public sealed record CardParseResult
{
  /// <summary>
  /// The parsed card AST.
  /// </summary>
  public required CardOutputAST Output { get; init; }

  /// <summary>
  /// The overall parse status.
  /// </summary>
  public required ParseStatus Status { get; init; }

  /// <summary>
  /// All diagnostics from parsing (warnings, errors, info).
  /// </summary>
  public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }

  /// <summary>
  /// Parsing metrics.
  /// </summary>
  public required CardParseMetrics Metrics { get; init; }
}

/// <summary>
/// Metrics for card parsing.
/// </summary>
public sealed record CardParseMetrics
{
  /// <summary>
  /// Total number of abilities identified.
  /// </summary>
  public required int TotalAbilities { get; init; }

  /// <summary>
  /// Number of abilities successfully parsed.
  /// </summary>
  public required int ParsedAbilities { get; init; }

  /// <summary>
  /// Number of abilities that did not fully parse — an <c>UnparsedAbility</c> or
  /// any ability carrying a nested <c>IUnparsed</c> node (e.g. an
  /// <c>UnparsedEffect</c>). Rule 0001 goal (a): buried failures count as failures.
  /// </summary>
  public required int FailedAbilities { get; init; }

  /// <summary>
  /// Total parsing duration in milliseconds.
  /// </summary>
  public required double DurationMs { get; init; }

  /// <summary>
  /// Number of faces parsed (0 for single-faced cards).
  /// </summary>
  public required int FaceCount { get; init; }

  /// <summary>
  /// Card-wide residual-debt tally (ADR 0001), counted across the main oracle
  /// and every face. Keyed by kind; see <see cref="ParseMetrics.ResidualCounts"/>.
  /// </summary>
  public required IReadOnlyDictionary<string, int> ResidualCounts { get; init; }

  /// <summary>Total residual occurrences across all kinds.</summary>
  public int ResidualTotal => ResidualCounts.Values.Sum();
}
