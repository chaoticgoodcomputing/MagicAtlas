namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>dice-rolled</b> EMIT family (a "roll N dice" effect, CR 706). Dice-rolled is
/// a bare EVENT verb (ADR-0003 §5, fork F3 — no supergroup): stem <c>dice</c>. The rolling player rides as
/// the port Subject; its control token is the only label facet. ADR-2 label <c>emit:rolldice[:&lt;scope&gt;]</c>
/// ↔ ADR-3 <c>dice</c> (scope reconstructed from the Subject, mirroring <see cref="PortLabel.RollDiceEmit"/>).
/// </summary>
public sealed class DiceEmitFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "emit" || seg[1] != "rolldice")
      return null;
    return PortStructure.Of(PortSide.Emit, "dice");
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Emit || structure.Stem != "dice")
      return null;
    return Join("emit", "rolldice", subject is null ? null : PortLabel.Scope(subject));
  }

  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
