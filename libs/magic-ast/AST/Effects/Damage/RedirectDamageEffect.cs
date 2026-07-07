namespace MagicAST.AST.Effects.Damage;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Damage-redirection replacement shield: "The next N damage that would be dealt to
/// [<see cref="From"/>] this turn is dealt to [<see cref="To"/>] instead." — the
/// {0} en-Kor family (Warrior en-Kor, Nomads en-Kor, Task Mage en-Kor, …).
///
/// <para>A one-shot, turn-scoped replacement effect (CR 614.1) created by the
/// resolution of an activated ability (CR 602.1). It watches for the next damage
/// that would be dealt to <see cref="From"/> this turn and reroutes up to
/// <see cref="Amount"/> of it to <see cref="To"/>. Structurally this is the
/// redirection sibling of the prevention shield <see cref="PreventDamageEffect"/>
/// ("Prevent the next N damage … this turn"): both are amount-bounded, turn-scoped
/// damage shields set up by a resolving ability, so both are modelled as a flat
/// <see cref="ContinuousEffect"/> that bears <see cref="Amount"/> and the inherited
/// <see cref="ContinuousEffect.Duration"/> ("this turn") — NOT the permanent-scoped
/// static <see cref="Replacement.ReplacementEffect"/> (Sphere of Law), which states
/// no duration because it persists while its source is on the battlefield.</para>
///
/// <para>CR 602.1 (verbatim): "Activated abilities have a cost and an effect. They are
/// written as \"[Cost]: [Effect.] [Activation instructions (if any).]\" …"</para>
///
/// <para>CR 614.1 (verbatim): "Some continuous effects are replacement effects … Such
/// effects watch for a particular event that would happen and completely or partially
/// replace it …"</para>
///
/// <para>The shield / which-instance bookkeeping and the redirected-damage event are
/// engine territory (descriptive-not-engine doctrine), mirroring how
/// <see cref="PreventDamageEffect"/> and
/// <see cref="MagicAST.AST.Effects.ZoneChange.RegenerateEffect"/> record a
/// "next … this turn" shield without modelling the replacement machinery.</para>
/// </summary>
[OracleEffect("redirectDamage")]
public sealed record RedirectDamageEffect : ContinuousEffect
{
  /// <summary>
  /// The bounded number of damage points redirected — "the next <b>N</b> damage".
  /// A literal (1 for the en-Kor creatures). Null for an amount-unbounded redirect
  /// (none in the current corpus), mirroring <see cref="PreventDamageEffect.Amount"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }

  /// <summary>
  /// The permanent whose incoming damage is redirected — "damage that would be dealt
  /// to [this]" (<see cref="ObjectReferenceKind.Self"/> for the en-Kor creatures).
  /// </summary>
  public required ObjectReference From { get; init; }

  /// <summary>
  /// The new recipient the damage is dealt to instead — "is dealt to [target creature
  /// you control] instead".
  /// </summary>
  public required ObjectReference To { get; init; }
}
