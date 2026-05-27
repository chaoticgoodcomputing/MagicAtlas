namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "This [permanent] enters tapped." A static-ability-attached effect that
/// records the oracle-level property. Rules-engine treatment (Rule 614,
/// replacement effects) — that the permanent enters tapped instead of
/// untapped — is derived from this descriptive declaration; MAST does not
/// model the replacement-event machinery itself.
///
/// <para>When the effect is scoped to permanents other than the source itself
/// (e.g., "Creatures your opponents control enter tapped."), the
/// <see cref="Scope"/> field holds an <see cref="ObjectFilter"/> describing
/// which permanents the replacement applies to. Null means the effect applies
/// to the source permanent itself (the normal self-enters-tapped shape).</para>
/// </summary>
[OracleEffect("entersTapped")]
public sealed record EntersTappedEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// Filter describing which permanents enter tapped under this static ability.
  /// Null for the standard self-enters-tapped shape ("This land enters tapped.").
  /// Populated when the ability applies to a broader set of permanents, e.g.
  /// "Creatures your opponents control enter tapped." maps to
  /// <c>{ CardTypes: ["creature"], Controller: "Opponent" }</c>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Scope { get; init; }
  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }

  /// <summary>
  /// Board-state condition that gates whether this permanent enters tapped.
  /// Two oracle shapes share this field:
  /// <list type="bullet">
  ///   <item>Fastland / checkland — "This land enters tapped <em>unless</em>
  ///         [condition]." <see cref="EntryConditionIsPositive"/> = false
  ///         (default): the condition text describes when it enters
  ///         <em>untapped</em>; the land enters tapped in all other cases.</item>
  ///   <item>Slow land — "<em>If</em> [condition], this land enters tapped."
  ///         <see cref="EntryConditionIsPositive"/> = true: the condition text
  ///         describes when it enters <em>tapped</em>; the land enters untapped
  ///         in all other cases.</item>
  /// </list>
  /// Distinct from <see cref="UnlessClause"/> which represents the
  /// "unless [player] pays [cost]" cost-prevention pattern (Rule 614.1c).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? EntryCondition { get; init; }

  /// <summary>
  /// Polarity of <see cref="EntryCondition"/>.
  /// <c>false</c> (default) — the condition is the "unless" negation branch:
  ///   the land enters tapped when the condition does NOT hold
  ///   (fastland / checkland shape: "enters tapped unless [condition]").
  /// <c>true</c> — the condition is the positive "if" branch:
  ///   the land enters tapped when the condition DOES hold
  ///   (slow land shape: "if [condition], this land enters tapped").
  /// Omitted from serialization when false (the implied default).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool EntryConditionIsPositive { get; init; }
}
