namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>pay-mana</b> cost family. A mana requirement in a cost consumes the scalar
/// <c>mana</c> resource (no object subject); the color qualifier rides as an attribute. ADR-2 labels
/// <c>pay:mana</c> (generic), <c>pay:mana:colorless</c>, and <c>pay:mana:&lt;color&gt;</c> ↔ ADR-3 structure
/// <c>consume:mana[color=&lt;segment&gt;]</c> (the color attr is absent for the generic requirement).
/// </summary>
public sealed class PayManaFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    if (port.Label != "pay:mana" && !port.Label.StartsWith("pay:mana:"))
      return null;
    var seg = port.Label.Split(':');
    return seg.Length >= 3
      ? PortStructure.Of(PortSide.Consume, "mana", ("color", seg[2]))
      : PortStructure.Of(PortSide.Consume, "mana");
  }
}
