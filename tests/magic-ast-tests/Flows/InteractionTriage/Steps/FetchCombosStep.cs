using System.Text.Json;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._01_Raw.Schemas;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// Source step: streams the Commander Spellbook <c>variants.json</c> dump (~510 MB, a gitignored
/// static input under <c>_01_Raw/Datasets/External/</c>) and projects it to the lean
/// <see cref="Combo"/> work-list — cards (name + Scryfall <c>oracleId</c>), popularity, identity,
/// produced results. Streaming-deserializes so the 510 MB is read incrementally (not buffered as a
/// string); System.Text.Json drops CSB's bloat (the ~10 image-URI fields per card, prices,
/// prerequisites, card-state).
/// </summary>
/// <remarks>
/// We curl the dump to a static input rather than use Flowthru's HTTP catalog medium: the medium's
/// <c>.Json()</c> singleton builder does not route an <c>https://</c> path through <c>UseHttp</c>
/// (it is treated as a local file path — only the array/CSV builders demonstrate HTTP routing). That
/// is a Flowthru gap to push upstream; until then this mirrors the <c>FetchScryfallBulkStep</c>
/// streaming pattern. <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> maps CSB's
/// camelCase keys onto the PascalCase schema props (System.Text.Json ignores Flowthru's
/// <c>[SerializedLabel]</c>; the labels still drive the Combos write side).
/// </remarks>
[FlowthruStep]
public static class FetchCombosStep
{
  private static readonly JsonSerializerOptions s_jsonOptions =
    new() { PropertyNameCaseInsensitive = true };

  public static Func<Task<IEnumerable<Combo>>> Create(string variantsJsonPath) =>
    async () =>
    {
      if (!File.Exists(variantsJsonPath))
      {
        throw new FileNotFoundException(
          $"Commander Spellbook variants.json not found at '{variantsJsonPath}'. Curl it first: "
            + "curl -o <path> https://json.commanderspellbook.com/variants.json",
          variantsJsonPath
        );
      }

      await using var stream = File.OpenRead(variantsJsonPath);
      var dump = await JsonSerializer.DeserializeAsync<CsbVariantsDump>(stream, s_jsonOptions);

      return (dump?.Variants ?? [])
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
    };
}
