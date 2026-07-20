using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.DiscriminatorGovernance.Steps;

namespace MagicAtlas.Ast.Tests.Flows.DiscriminatorGovernance;

/// <summary>
/// The discriminator near-duplicate report → <c>Data/_08_Reporting/discriminator-governance.json</c>.
/// ADR-0004 §1, issue #38: the near-duplicate check was briefly a CORE-ring gate backed by
/// <c>discriminator-justifications.json</c>; the whitelist is deleted, the rulings moved to the
/// declaration sites, and the check is demoted to this report. The HARD per-family collision check
/// remains a gate (<c>DiscriminatorUniquenessTests</c>) and needs no whitelist.
///
/// <para>Hermetic: reflects the loaded MagicAST assembly, reads nothing.</para>
/// </summary>
public static class DiscriminatorGovernanceFlow
{
  public static BuiltFlow Create(Catalog catalog) =>
    FlowBuilder.CreateFlow(
      "DiscriminatorGovernance",
      pipeline =>
      {
        pipeline.AddStep<Data._08_Reporting.Schemas.DiscriminatorGovernance>(
          label: "ReportNearDuplicates",
          transform: GovernanceStep.Create(),
          outputs: catalog.DiscriminatorGovernance
        );
      }
    );
}
