namespace MagicAST.Interaction;

/// <summary>
/// The five ADR-0002 §8 accounting axes of a reconstructed cycle, plus the one sub-discriminator
/// (<see cref="Gated"/>) needed to say WHY <see cref="Firable"/> failed. This is the whole input to
/// <see cref="ComboPlainLanguage.Describe"/> — <b>the axis vector IS the reason</b> (ADR 0004 §5.3), so
/// no prose is stored anywhere and none is passed in.
///
/// <para><b><see cref="Gated"/> is not a sixth pinned axis.</b> It is a strictly narrower fact than
/// <c>!Firable</c>: <see cref="PortCycle.Firable"/> is false either because a hop touches a hard
/// <see cref="PortNode.Gated"/> port (never discharged) or because it touches a
/// <see cref="PortNode.TapGated"/> port the loop does not renew (dischargeable). Both collapse to the
/// same axis in the expectation file; only the rendered sentence distinguishes them.</para>
/// </summary>
public sealed record ComboAxisVector
{
  public required bool Firable { get; init; }
  public required bool CoCostsSatisfied { get; init; }
  public required bool Balanced { get; init; }
  public required bool LifeBalanced { get; init; }
  public required bool Productive { get; init; }

  /// <summary>Some hop touches a HARD gate (an intervening-if, a rate-limit restriction, a self-bounce
  /// cost, an optional "you may" effect) — the undischargeable half of <c>!Firable</c>.</summary>
  public required bool Gated { get; init; }

  /// <summary>All five axes hold — the cycle is a certified infinite.</summary>
  public bool AllHold => Firable && CoCostsSatisfied && Balanced && LifeBalanced && Productive;

  /// <summary>The axis names that do NOT hold, in the fixed <see cref="ComboPlainLanguage.Axes"/> order.
  /// This set — and nothing else — is what <c>combo-axis-expectations.json</c> pins.</summary>
  public IReadOnlyList<string> FailingAxes =>
    [
      .. new (string Name, bool Holds)[]
      {
        (ComboPlainLanguage.AxisFirable, Firable),
        (ComboPlainLanguage.AxisCoCostsSatisfied, CoCostsSatisfied),
        (ComboPlainLanguage.AxisBalanced, Balanced),
        (ComboPlainLanguage.AxisLifeBalanced, LifeBalanced),
        (ComboPlainLanguage.AxisProductive, Productive),
      }
        .Where(a => !a.Holds)
        .Select(a => a.Name),
    ];

  public static ComboAxisVector FromCycle(PortCycle cycle) =>
    new()
    {
      Firable = cycle.Firable,
      CoCostsSatisfied = cycle.CoCostsSatisfied,
      Balanced = cycle.Balanced,
      LifeBalanced = cycle.LifeBalanced,
      Productive = cycle.Productive,
      Gated = cycle.Edges.Any(e => e.From.Gated || e.To.Gated),
    };

  public static ComboAxisVector FromDiagnostics(ComboDiagnostics d) =>
    new()
    {
      Firable = d.Firable,
      CoCostsSatisfied = d.CoCostsSatisfied,
      Balanced = d.Balanced,
      LifeBalanced = d.LifeBalanced,
      Productive = d.Productive,
      Gated = d.Edges.Any(e => e.Gated),
    };
}

/// <summary>
/// ADR 0004 §5.3/§5.4 — renders a <see cref="ComboAxisVector"/> as plain language a non-player can read
/// without a legend. <b>Pure, total, and stored nowhere</b>: this is the replacement for the hand-written
/// <c>reason</c> prose that used to sit in <c>combo-expected-tiers.json</c>, and for the bare
/// <c>"Green"</c>/<c>"Amber"</c> chip. Generated at display time, never persisted, never gated on.
///
/// <para><b>Precedence mirrors <see cref="PortCycle.LimitingReason"/> exactly</b>, and
/// <c>ComboPlainLanguageTest</c> is the metamorphic check that keeps the two implementations honest —
/// the same pattern the retired <c>LimitingHopAgreesWithEngineTest</c> applied to the limiting hop. A
/// cycle is described as certified infinite iff the engine reports no limiting reason.</para>
///
/// <para><b>PROVISIONAL COPY (2026-07-21).</b> Every sentence below is pending the owner's sign-off per
/// ADR 0004's open question "plain-language wording for the retired colours (§5.4) — no concrete copy
/// exists yet". They are constants precisely so a wording change is one edit with a compiler-checked
/// blast radius.</para>
/// </summary>
public static class ComboPlainLanguage
{
  // ── The pinned axis vocabulary (the ONLY legal `axis` values in combo-axis-expectations.json) ──────

  public const string AxisFirable = "Firable";
  public const string AxisCoCostsSatisfied = "CoCostsSatisfied";
  public const string AxisBalanced = "Balanced";
  public const string AxisLifeBalanced = "LifeBalanced";
  public const string AxisProductive = "Productive";

  /// <summary>The axis vocabulary, in the canonical order failures are reported in.</summary>
  public static readonly IReadOnlyList<string> Axes =
  [
    AxisFirable,
    AxisCoCostsSatisfied,
    AxisBalanced,
    AxisLifeBalanced,
    AxisProductive,
  ];

  // ── The rendered sentences (PROVISIONAL — see the type doc) ────────────────────────────────────────

  public const string CertifiedInfinite = "certified infinite";

  /// <summary>A hop touches a HARD gate. <b>The wording is the weakest of the set</b> and is flagged as
  /// such: <see cref="PortNode.Gated"/> is set by an intervening-if, by any of the
  /// <c>GatingRestrictions</c> (<c>OnlyOnceEachTurn</c>, <c>OnlyOnce</c>, <c>Conditional</c>,
  /// <c>OnlyIfNoUntappedLands</c>), by a <c>returnToHand</c> self-bounce cost, and by an
  /// <c>optional</c> ("you may") effect — so "once per turn" is only ONE of at least five causes, and is
  /// not even the common one in the current pin set (all three gated combos are optional-blink cards).
  /// Kept verbatim as the owner proposed it, with the disagreement recorded here rather than silently
  /// resolved.</summary>
  public const string Gated = "can only fire once per turn";

  /// <summary>Not firable, and no hard gate — i.e. an unrenewed <see cref="PortNode.TapGated"/> hop.</summary>
  public const string TapNotRenewed = "needs a way to untap between iterations";

  public const string ManaNegative = "loop closes, but it costs more mana than it makes";
  public const string LifeNegative = "loop closes, but it drains more life than it gains";
  public const string NetZero = "repeats, but produces nothing extra each time";
  public const string UnfedCoCost = "needs another card to pay one of its costs";

  /// <summary>
  /// The single sentence for this axis vector. Total: every input yields exactly one string.
  /// </summary>
  public static string Describe(ComboAxisVector v) =>
    !v.Firable && v.Gated ? Gated
    : !v.Firable ? TapNotRenewed
    : !v.Balanced ? ManaNegative
    : !v.LifeBalanced ? LifeNegative
    : !v.Productive ? NetZero
    : !v.CoCostsSatisfied ? UnfedCoCost
    : CertifiedInfinite;

  /// <summary>
  /// Every non-holding axis rendered, in <see cref="Axes"/> order — for a reader who needs the whole
  /// picture rather than the headline. <see cref="Describe"/> returns the first of these (or
  /// <see cref="CertifiedInfinite"/>); this returns all of them. Empty when the vector is certified.
  /// </summary>
  public static IReadOnlyList<string> DescribeAll(ComboAxisVector v)
  {
    var lines = new List<string>();
    if (!v.Firable)
      lines.Add(v.Gated ? Gated : TapNotRenewed);
    if (!v.CoCostsSatisfied)
      lines.Add(UnfedCoCost);
    if (!v.Balanced)
      lines.Add(ManaNegative);
    if (!v.LifeBalanced)
      lines.Add(LifeNegative);
    if (!v.Productive)
      lines.Add(NetZero);
    return lines;
  }
}
