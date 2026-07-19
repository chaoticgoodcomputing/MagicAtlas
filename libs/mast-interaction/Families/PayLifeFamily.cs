namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>pay-life</b> cost family (2026-07-18 precision-fix, Interaction Currency B).
/// A "Pay N life" cost (CR 118.1/119.4) consumes the LIFE resource as an activation/casting COST — a
/// distinct concept from <see cref="LifeFamily"/>'s emit side (a life-gain/loss EVENT, CR 119.3) and
/// <see cref="LifeTriggerFamily"/>'s consume side (a "whenever a player gains/loses life" TRIGGER
/// subscribing to that event). Deliberately given its OWN stem <c>paylife</c> — not <c>life</c> — even
/// though both are the life resource: sharing the <c>life</c> stem with <see cref="LifeTriggerFamily"/>
/// would make <see cref="PortFlowMatcher.SelectArm"/>'s <c>(life,life)→LifeToTrigger</c> stem match capture
/// this port too (ambiguous — a TRIGGER subscription and a COST payment are not the same relation), so the
/// distinct stem keeps <c>LifeCostToPay</c> unambiguously selectable. ADR-2 label <c>pay:paylife</c> ↔
/// ADR-3 structure <c>consume:paylife</c> (no attributes — the amount rides as <see cref="PortNode.Quantity"/>,
/// the payer as <see cref="PortNode.Subject"/>, same convention as <c>pay:mana</c>).
/// </summary>
public sealed class PayLifeFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume || port.Label != "pay:paylife")
      return null;
    return PortStructure.Of(PortSide.Consume, "paylife");
  }
}
