using System.Net.Http.Json;
using System.Text.Json;
using Flowthru.Step;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Flows.Ingest.Nodes;

/// <summary>
/// Source step that fetches Scryfall's card-symbology JSON from
/// <c>https://api.scryfall.com/symbology</c> and parks it in the <c>RawCardSymbols</c> catalog
/// item. Uses an injected <see cref="HttpClient"/> directly because
/// <see cref="JsonSingletonBuilder{T}"/> doesn't compose with Flowthru's HTTP storage-medium
/// resolver in 0.17.x — once that gap is closed we can collapse this step into a plain HTTP
/// catalog item.
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
