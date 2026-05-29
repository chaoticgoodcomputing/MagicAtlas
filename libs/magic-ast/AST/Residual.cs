namespace MagicAST.AST;

/// <summary>
/// Marks an AST node that represents oracle text the parser recognised but has
/// not yet fully structured — the typed residual arms of the free-text doctrine
/// (ADR 0001): a structured node whose role is to carry "not yet structured"
/// honestly (e.g. <c>OtherCharacteristic</c>, <c>OtherHistoryPredicate</c>,
/// <c>UnparsedEffect</c>).
///
/// <para>
/// Implemented purely so the residual-debt metric can find these nodes without a
/// hardcoded type list: a new residual arm opts into the count by implementing
/// this marker. Does NOT cover <c>UnparsedAbility</c> — a total ability-level
/// parse failure is already tracked by <c>ParseMetrics.FailedAbilities</c>;
/// residual debt is what hides *inside* otherwise-parsed ASTs.
/// </para>
/// </summary>
public interface IResidual { }

/// <summary>
/// Marks an AST node that represents a genuine parse FAILURE — the parser could
/// not structure this clause or effect at all, and recovered by emitting a node
/// that carries the raw text (e.g. <c>UnparsedAbility</c>, <c>UnparsedEffect</c>).
///
/// <para>
/// Distinct from <see cref="IResidual"/> (a recognised construct whose sub-part
/// structuring is deferred): an <c>IUnparsed</c> node is a hole, not a coarse
/// description. Under ADR 0001 its presence ANYWHERE in the tree means the parse
/// is not complete — it must fail triage and is banned from gold fixtures (a
/// gold fixture carrying one would assert the parser's current failure as truth,
/// the test-overfit anti-pattern). Real error-recovery parsers keep the partial
/// tree for diagnosis but propagate a "has errors" signal to the root; this
/// marker is that signal.
/// </para>
/// </summary>
public interface IUnparsed { }

/// <summary>
/// Marks a string-valued property that carries free text inside an otherwise
/// structured node — interior free text under the ADR 0001 doctrine (e.g.
/// <c>HistoryPredicate.Timeframe</c>, <c>SpellAbility.Instructions</c>,
/// <c>AbilityAdder.AbilityText</c>). The residual-debt metric counts non-empty
/// occurrences so this bounded-but-unstructured debt stays visible and trends
/// down rather than quietly accumulating.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FreeTextFieldAttribute : Attribute { }
