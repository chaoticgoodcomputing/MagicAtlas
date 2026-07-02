using System.Net.Http.Json;
using System.Text.Json;
using Flowthru.Step;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Flows.Ingest.Nodes;

/// <summary>
/// Source step that fetches Scryfall's card-symbology JSON from
/// <c>https://api.scryfall.com/symbology</c> and parks it in the <c>RawCardSymbols</c> catalog
/// item. Uses an injected <see cref="HttpClient"/> directly rather than a plain HTTP catalog item:
/// <c>api.scryfall.com</c> rejects requests that carry no <c>Accept</c> header with HTTP 400, and
/// Flowthru's <c>HttpStorageMedium</c> builds its client from <c>HttpOptions</c> (User-Agent only —
/// no per-item header hook), so a <c>.Json()</c>-over-https item would 400 here. The injected client
/// sets <c>Accept</c> explicitly. (The <c>.Json()</c> singleton itself <em>does</em> route https://
/// through <c>UseHttp</c> as of 0.21+ — see the Commander Spellbook <c>CsbVariantsRaw</c> item in the
/// magic-ast-tests harness, whose host needs no <c>Accept</c>.)
/// </summary>
/// <remarks>
/// Scryfall returns snake_case JSON, but System.Text.Json doesn't understand Flowthru's
/// <c>[SerializedLabel]</c> attribute. The <c>SnakeCaseLower</c> naming policy bridges the gap
/// for properties whose C# PascalCase/underscored name maps to the JSON snake_case key — true
/// for every field on <see cref="RawScryfallCardSymbol"/> and <see cref="RawScryfallCardSymbolList"/>.
/// </remarks>
[FlowthruStep]
public static class FetchCardSymbolsNode
{
  private const string ScryfallSymbologyUrl = "https://api.scryfall.com/symbology";

  private static readonly JsonSerializerOptions s_jsonOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  public static Func<Task<RawScryfallCardSymbolList>> Create(HttpClient httpClient)
  {
    return async () =>
    {
      var payload =
        await httpClient.GetFromJsonAsync<RawScryfallCardSymbolList>(
          ScryfallSymbologyUrl,
          s_jsonOptions
        )
        ?? throw new InvalidOperationException(
          $"Scryfall symbology endpoint at '{ScryfallSymbologyUrl}' returned null."
        );
      return payload;
    };
  }
}
