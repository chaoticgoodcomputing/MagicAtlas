namespace MagicAST.Interaction;

/// <summary>
/// The conditionality axes of a single interaction port — <b>one of the two dimensions the retired
/// four-valued <c>PortRow.Tier</c> conflated</b> (ADR 0004 #43). Where the old enum's Green/Amber split
/// answered "is this mechanism conditional?" with a single collapsed bit, these three orthogonal facts —
/// read straight off the <see cref="PortNode"/> — say <em>how</em> it is conditional, and the separate
/// provenance marker (parsed / Inferred / Declared) answers "where did it come from?" independently.
///
/// <para>The axis vector IS the description (the same principle as
/// <see cref="ComboAxisVector"/>/<see cref="ComboPlainLanguage"/> one layer up): no prose is stored on the
/// port, <see cref="PortConditionality.Describe"/> renders it at display time.</para>
/// </summary>
public sealed record PortConditionAxes
{
  /// <summary>The ability carries a <c>{T}</c> tap cost (<see cref="PortNode.TapGated"/>) — it fires once
  /// per untap.</summary>
  public required bool TapGated { get; init; }

  /// <summary>The ability's intervening-if requires a counter on the object
  /// (<see cref="PortNode.RequiresCounter"/> is non-null).</summary>
  public required bool RequiresCounter { get; init; }

  /// <summary>A HARD gate (<see cref="PortNode.Gated"/>): a rate-limit ("only once each turn"), an
  /// intervening-if / conditional restriction, a self-bounce cost, or an optional "you may" effect.</summary>
  public required bool RateLimited { get; init; }

  /// <summary>No axis is set — the mechanism fires with no timing/board-state caveat (the old Green).</summary>
  public bool Unconditional => !TapGated && !RequiresCounter && !RateLimited;
}

/// <summary>
/// ADR 0004 #43 — renders a <see cref="PortConditionAxes"/> as plain language a non-player can read without
/// a legend. <b>Pure, total, and stored nowhere</b>: this is the port-side analogue of
/// <see cref="ComboPlainLanguage"/>, replacing the bare <c>"Green"</c>/<c>"Amber"</c> half of the retired
/// port tier. Generated at display time, never persisted opaquely, never gated on.
///
/// <para><b>PROVISIONAL COPY (2026-07-21).</b> Every sentence below is pending the owner's sign-off — the
/// same open question ADR 0004 records for the combo side (§5.4). In particular
/// <see cref="RateLimited"/> is a deliberately rough gloss: <see cref="PortNode.Gated"/> has at least five
/// distinct causes (an intervening-if, <c>OnlyOnceEachTurn</c>, <c>OnlyOnce</c>/exhaust, a boolean
/// <c>Conditional</c>, <c>OnlyIfNoUntappedLands</c>, plus self-bounce costs and optional effects), so
/// "rate-limited" over-commits. They are <c>const</c> strings precisely so a wording change is one edit
/// with a compiler-checked blast radius.</para>
/// </summary>
public static class PortConditionality
{
  // ── The rendered phrases (PROVISIONAL — see the type doc) ────────────────────────────────────────────

  /// <summary>No axis set — the mechanism fires unconditionally (replaces the tier's "Green"/"Verified").</summary>
  public const string Unconditional = "fires unconditionally";

  public const string TapGated = "needs to tap";
  public const string RequiresCounter = "needs a counter on it";

  /// <summary>PROVISIONAL and the weakest of the set — "rate-limited" is one gloss over ≥5 gate causes
  /// (see the type doc). Kept as the owner proposed it, with the disagreement recorded rather than
  /// silently resolved.</summary>
  public const string RateLimited = "rate-limited";

  /// <summary>The conditionality phrases in the canonical order they are reported in.</summary>
  public static readonly IReadOnlyList<string> Phrases = [TapGated, RequiresCounter, RateLimited];

  /// <summary>
  /// The single description string for this axis vector. Total and <b>lossless</b>: unlike the old tier
  /// (which collapsed every gate to "Amber"), when several axes hold they are all rendered, joined in
  /// canonical order, so a port that both taps and is rate-limited says so. Unconditional ports render
  /// <see cref="Unconditional"/>.
  /// </summary>
  public static string Describe(PortConditionAxes a) =>
    a.Unconditional ? Unconditional : string.Join(" · ", DescribeAll(a));

  /// <summary>Each holding axis rendered, in canonical order. Empty when the port is unconditional.</summary>
  public static IReadOnlyList<string> DescribeAll(PortConditionAxes a)
  {
    var lines = new List<string>();
    if (a.TapGated)
      lines.Add(TapGated);
    if (a.RequiresCounter)
      lines.Add(RequiresCounter);
    if (a.RateLimited)
      lines.Add(RateLimited);
    return lines;
  }
}
