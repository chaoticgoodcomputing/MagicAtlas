namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "As this [permanent] enters, choose a color." — the as-enters color-choice
/// declaration (Rule 614.1c, as-enters static replacement). The oracle line
/// records that the controller makes a color selection at the moment this
/// permanent enters the battlefield. Subsequent abilities that reference
/// "the chosen color" are downstream consumers of this choice; MAST models
/// only the choice declaration itself, not the producer/consumer link.
///
/// <para>Design rationale: This is a separate effect type rather than a
/// variant of another entry effect because the surface verb in oracle text
/// is "choose a color" — an explicit player-decision instruction, not a
/// board-state check or cost payment. Keeping the choice declaration
/// as its own node is descriptively faithful to what the oracle line says
/// (per the MAST descriptive-not-engine doctrine) and leaves the consumer
/// connection (how "the chosen color" references resolve) to the rules engine.</para>
///
/// <para>Distinct from <c>PayLifeOnEntryEffect</c> (Shockland pattern): that
/// effect records a cost-payment decision; this effect records a color-selection
/// decision. Both fire "as this permanent enters" but carry different descriptive
/// shapes in oracle text.</para>
/// </summary>
[OracleEffect("chooseColorOnEntry")]
public sealed record ChooseColorOnEntryEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// Optional restriction on the color choice (e.g. "choose a color other than
  /// blue"). Null for unrestricted "choose a color" printings. Stored verbatim
  /// from oracle text as a free-form string; MAST does not parse the exclusion
  /// further.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Restriction { get; init; }

  /// <summary>Whether this effect carries a "you may" prefix in oracle text. (IOptionalEffect)</summary>
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
}
