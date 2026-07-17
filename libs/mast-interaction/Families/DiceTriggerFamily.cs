namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>dice-rolled</b> TRIGGER family (a "whenever you roll one or more dice"
/// trigger, CR 706). Same bare EVENT stem <c>dice</c> as <see cref="DiceEmitFamily"/>, distinguished by
/// <see cref="PortSide.Consume"/>. The watched player rides as the port Subject. ADR-2 label
/// <c>trigger:rolldice[:&lt;scope&gt;]</c> ↔ ADR-3 <c>consume dice</c>.
/// </summary>
public sealed class DiceTriggerFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "trigger" || seg[1] != "rolldice")
      return null;
    return PortStructure.Of(PortSide.Consume, "dice");
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Consume || structure.Stem != "dice")
      return null;
    return Join("trigger", "rolldice", subject is null ? null : PortLabel.Scope(subject));
  }

  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
