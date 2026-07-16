namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>compatibility shim</b> that serializes a <see cref="PortStructure"/> back to
/// the ADR-0002 colon-label, byte-for-byte, per family. This is what lets the ADR-3 structure and the
/// ADR-2 string-matching engine coexist during Stages 2–3: every port carries the structure (the new
/// truth) AND a label produced by the legacy generator, and the Stage-2 round-trip gate asserts
/// <c>ToLegacyLabel(structure, subject) == label</c> for every structured port — proving the structure is
/// a lossless superset. Deleted at Stage 4 cutover, when the label becomes a direct ADR-3 serialization.
///
/// <para>One branch per family; the branch reconstructs the exact <see cref="PortLabel"/> <c>Join(...)</c>
/// the ADR-2 generator produced, keyed on the structure's (Side, Stem, attribute) shape. Unmapped
/// structures throw — Stage 2 converts families incrementally, so a throw means "this family isn't
/// structured yet," never a silent wrong label.</para>
/// </summary>
public static class LegacyLabel
{
  public static string ToLegacyLabel(PortStructure s, ObjectFilter? subject, TypeOntology ontology)
  {
    // ---- Deployment: blink (ADR-2 emit:blink) ---------------------------------------------------------
    // BlinkEmit = Join("emit","blink", Subject(blinked), blinked.IsSelf ? "self" : null). The ADR-3
    // structure is deployment:<type>[manner=blink]; the blinked object rides as the port Subject, so the
    // subject/self facets come from it (not the stem).
    if (s.Side == PortSide.Emit && s.Attr("manner") == "blink")
    {
      return Join(
        "emit",
        "blink",
        subject is null ? null : PortLabel.Subject(subject, ontology),
        subject?.IsSelf == true ? "self" : null
      );
    }

    throw new InvalidOperationException(
      $"LegacyLabel: no ADR-2 serialization mapped for structured port {s.Canonical()} "
        + "(Stage 2 converts families incrementally — this family is not structured yet)."
    );
  }

  /// <summary>Join facets in canonical order, dropping empties (mirrors <see cref="PortLabel"/>'s private
  /// <c>Join</c>).</summary>
  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
