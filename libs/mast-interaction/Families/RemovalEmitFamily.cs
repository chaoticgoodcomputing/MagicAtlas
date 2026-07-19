namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 §5 — the <b>sacrifice death</b> EMIT family. Paying a sacrifice cost intrinsically removes the
/// fodder (CR 701.21a: battlefield → its owner's graveyard), so the sac clause also EMITS a death event —
/// the dual of the fodder consume (<see cref="SacFamily"/>), the O2/O10 "one clause, two ports". The stem is
/// <c>removal:creature</c> (Removal anchored on the battlefield FROM-endpoint, ADR-0003 §4) with the
/// destination + manner as attributes: the narrowest rung <c>[to=graveyard, manner=sacrificed]</c>. A dies
/// trigger (<see cref="DeathFamily"/>, <c>removal:creature[to=graveyard]</c>), a bare leaves-the-battlefield
/// consume, or a "when sacrificed" consume (<c>[manner=sacrificed]</c>) all capture it by attribute
/// subsumption (<c>PortFlowMatcher.SacrificeDeathToTrigger</c>), which is what retired the curated
/// consume→consume <c>sac→dies</c> label bridge. The fodder rides as the port Subject, so the
/// subject/scope/exclusion facets of the label come from it. ADR-2 has no counterpart — this port did not
/// exist before the remodel (the death was a curated bridge, never a projected emit).
/// </summary>
public sealed class RemovalEmitFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "emit" || seg[1] != "removal")
      return null;
    return PortStructure.Of(PortSide.Emit, "removal:creature", ("to", "graveyard"), ("manner", "sacrificed"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (
      structure.Side != PortSide.Emit
      || structure.Stem != "removal:creature"
      || structure.Attr("to") != "graveyard"
      || structure.Attr("manner") != "sacrificed"
    )
      return null;
    return Join(
      "emit",
      "removal",
      subject is null ? null : PortLabel.Subject(subject, ontology),
      "to-graveyard",
      "sacrificed",
      subject is null ? null : PortLabel.Scope(subject),
      subject is null ? null : PortLabel.Exclusion(subject)
    );
  }

  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
