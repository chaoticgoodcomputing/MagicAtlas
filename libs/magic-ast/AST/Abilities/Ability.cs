namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization;

/// <summary>
/// Base type for all ability nodes in the AST.
/// Abilities represent the functional components of card text.
/// </summary>
[PolymorphicBase("Kind")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Ability>))]
public abstract record Ability
{
  /// <summary>
  /// The category of this ability.
  /// Note: Not serialized - the polymorphic "kind" discriminator provides this information.
  /// </summary>
  [JsonIgnore]
  public abstract AbilityKind AbilityKind { get; }

  /// <summary>
  /// Optional ability word prefix (e.g., \"Landfall\", \"Enrage\", \"Revolt\").
  /// Ability words have no rules meaning but tie together similar abilities.
  /// Rule 207.2c
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? AbilityWord { get; init; }

  /// <summary>
  /// If this ability was expanded from a keyword, the keyword's typed identity.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public KeywordAbility? KeywordSource { get; init; }

  /// <summary>
  /// Optional parenthetical reminder text associated with this ability.
  /// Rule 207.2: Reminder text has no rules meaning but aids comprehension.
  /// Preserved for round-tripping, display, and educational purposes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Parenthetical? Reminder { get; init; }

  /// <summary>
  /// The span in the card's oracle text that produced this ability, populated
  /// from the originating <see cref="MagicAST.Parsing.OracleClause.SourceSpan"/>
  /// at parse time (MAST oracle-text provenance — upstream-atlas-data-plan §4).
  /// <c>null</c> when a parser cannot attribute a span; never fabricated.
  /// <para>
  /// Deliberately NOT serialized: it is held in-memory for downstream consumers
  /// (port projection / Explorer span highlighting) so that adding provenance does
  /// not perturb the gold-fixture JSON contract that the parser round-trip tests
  /// compare against. Enabling serialization is a separate, fixture-regenerating
  /// follow-up.
  /// </para>
  /// </summary>
  [JsonIgnore]
  public TextSpan? SourceSpan { get; init; }

  /// <summary>
  /// The 0-based oracle-text line (paragraph) index this ability's originating
  /// clause starts on. Defaults to 0. Not serialized — see <see cref="SourceSpan"/>.
  /// </summary>
  [JsonIgnore]
  public int OracleLineIndex { get; init; }
}
