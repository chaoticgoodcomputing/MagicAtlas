namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if you sacrifice an Island this way" / "if you put a Cave onto the battlefield this
/// way" — a within-resolution causation gate keyed on a property of the object the
/// IMMEDIATELY PRECEDING instruction of the same resolving ability acted on. Serendib
/// Djinn: "sacrifice a land. If you sacrifice an Island this way, this creature deals 3
/// damage to you." — the preceding "sacrifice a land" chose the land; this gate holds
/// when that sacrificed land matches <see cref="Filter"/> (an Island). Spelunking: "put a
/// land card … onto the battlefield. If you put a Cave onto the battlefield this way, you
/// gain 4 life." — the gate holds when the land put onto the battlefield was a Cave.
///
/// <para>
/// The action-general sibling of <see cref="DiedThisWayCondition"/> ("if it dies this
/// way"): where DiedThisWay keys on a specific state-change event to a single referenced
/// object, this keys on a controller ACTION (<see cref="Action"/>) and describes the
/// affected object by an <see cref="ObjectFilter"/> — the "this way" phrase binds the
/// object to whatever the preceding action of the SAME resolution acted on (CR 608.2c —
/// the ability's instructions resolve in order), so no separate reference is threaded; the
/// filter records the criterion the gate checks against that object (a subtype: "an
/// Island" → <c>{Subtypes:["Island"]}</c>). A unified parametric arm rather than two
/// near-duplicate per-action nodes: the causation-gate shape is identical across the
/// family and only the verb and the affected-object filter vary.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed causation gate; the
/// engine reads whether the preceding action of this resolution acted on a matching
/// object, MAST does not pre-evaluate it. Structured to this dedicated
/// <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 608.2c (excerpt): "The spell or ability's controller … follows the instructions in
/// the order written." — "this way" refers to the object acted on by the preceding
/// instruction of the same resolving ability.
/// CR 701.16 (Sacrifice) / CR 701.? (put onto the battlefield): the two actions this gate
/// currently ranges over.
/// </summary>
[ConditionKind("actionThisWay")]
public sealed record ActionThisWayCondition : Condition
{
  /// <summary>
  /// The controller action of the preceding instruction whose affected object this gate
  /// checks — <see cref="PrecedingAction.Sacrificed"/> for "you sacrifice … this way",
  /// <see cref="PrecedingAction.PutOntoBattlefield"/> for "you put … onto the battlefield
  /// this way".
  /// </summary>
  public required PrecedingAction Action { get; init; }

  /// <summary>
  /// The criterion the object acted on must match — a subtype for "an Island"/"a Cave"
  /// (<c>{Subtypes:["Island"]}</c> / <c>{Subtypes:["Cave"]}</c>).
  /// </summary>
  public required ObjectFilter Filter { get; init; }
}

/// <summary>
/// The controller action an <see cref="ActionThisWayCondition"/> keys on — the verb of the
/// preceding instruction whose affected object the "this way" gate checks. Recorded as
/// written (reference-not-resolution, ADR 0004).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrecedingAction
{
  /// <summary>"you sacrifice … this way" (CR 701.16, Sacrifice) — Serendib Djinn.</summary>
  Sacrificed,

  /// <summary>"you put … onto the battlefield this way" — Spelunking.</summary>
  PutOntoBattlefield,
}
