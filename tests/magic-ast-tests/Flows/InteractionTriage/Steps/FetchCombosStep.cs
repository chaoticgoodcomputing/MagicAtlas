using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._01_Raw.Schemas;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// Projects the Commander Spellbook <c>variants.json</c> dump (the <c>CsbVariantsRaw</c> HTTP catalog
/// item, ~510 MB) down to the lean <see cref="Combo"/> work-list — cards (name + Scryfall
/// <c>oracleId</c>), popularity, identity, produced results. The fetch + on-disk conditional-GET cache
/// is owned by Flowthru's <c>HttpStorageMedium</c> (wired via <c>UseHttp</c> in <c>Program.cs</c>), so
/// this step is now a pure transform: no <see cref="HttpClient"/>, no file IO, no manual-curl precondition.
/// </summary>
/// <remarks>
/// The narrow <see cref="CsbVariantsDump"/> schema already drops CSB's bloat (the ~10 image-URI fields
/// per card, prices, prerequisites, card-state) on the Flowthru read side via its
/// <c>[SerializedLabel]</c> camelCase aliases — so the materialized dump is a small fraction of the
/// 510 MB wire payload.
/// </remarks>
[FlowthruStep]
public static class FetchCombosStep
{
  public static Func<CsbVariantsDump, IEnumerable<Combo>> Create() =>
    dump =>
      (dump.Variants ?? [])
        .Select(v => new Combo
        {
          Id = v.Id,
          Popularity = v.Popularity ?? 0,
          Identity = v.Identity,
          Cards = v
            .Uses.Select(u => new ComboCard { Name = u.Card.Name, OracleId = u.Card.OracleId })
            .ToList(),
          Results = v.Produces.Select(p => p.Feature.Name).ToList(),
        })
        .ToList();
}
