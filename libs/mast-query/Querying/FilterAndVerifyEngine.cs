namespace MagicAST.Query;

using System.Text.Json.Nodes;
using MagicAST.Query.Patterns;
using MagicAST.Schema;

/// <summary>
/// Reference query engine. Implements the "verify" half of filter-and-verify by walking the
/// canonical JSON of each card AST; the shape-hash prefilter (mast-query ADR-0001) is staged.
///
/// Discriminator keys, the registered <c>(key, value)</c> type pairs, and the <c>IUnparsed</c> set
/// all come from MAST's schema export (<see cref="AstSchema"/>, magic-ast ADR-0008) — never
/// hardcoded — so the engine cannot drift from the serializer.
///
/// Tri-state: a card is <see cref="Determinacy.Unknown"/> when an unparsed region lies within the
/// pattern's scope and nothing matched. Full tri-state *composition* through nested patterns is
/// staged — this scaffold resolves Unknown at the top-level descendant walk, which already covers
/// the dominant "does this card contain shape X anywhere?" query.
/// </summary>
public sealed class FilterAndVerifyEngine : IQueryEngine
{
  private readonly IReadOnlyList<string> _discriminatorKeys;
  private readonly HashSet<(string Key, string Value)> _discriminators;
  private readonly HashSet<(string Key, string Value)> _unparsed;

  public FilterAndVerifyEngine(AstSchema schema)
  {
    _discriminatorKeys = schema.DiscriminatorKeys;
    _discriminators = schema
      .Bases.SelectMany(b => b.Types.Select(t => (b.DiscriminatorKey, t.Discriminator)))
      .ToHashSet();
    _unparsed = schema.UnparsedDiscriminators.Select(u => (u.Key, u.Value)).ToHashSet();
  }

  public QueryResult Run(string queryName, Pattern pattern, IReadOnlyList<CardDocument> corpus)
  {
    var matched = new List<CardMatch>();
    var unknown = new List<CardMatch>();
    var nonMatch = 0;

    // Deterministic ordering (ADR-0001): by card id.
    foreach (var card in corpus.OrderBy(c => c.Card, StringComparer.Ordinal))
    {
      var captures = new Dictionary<string, string>(StringComparer.Ordinal);
      var (det, path) = EvaluateCard(pattern, card.Ast, captures);
      switch (det)
      {
        case Determinacy.Match:
          matched.Add(
            new CardMatch
            {
              Card = card.Card,
              Determinacy = Determinacy.Match,
              Path = path,
              Captures = captures.Count > 0 ? captures : null,
            }
          );
          break;
        case Determinacy.Unknown:
          unknown.Add(
            new CardMatch
            {
              Card = card.Card,
              Determinacy = Determinacy.Unknown,
              Reason = "unparsed region within pattern scope",
            }
          );
          break;
        default:
          nonMatch++;
          break;
      }
    }

    return new QueryResult
    {
      Query = queryName,
      Matched = matched,
      Unknown = unknown,
      NonMatch = nonMatch,
    };
  }

  private (Determinacy, string?) EvaluateCard(
    Pattern pattern,
    JsonNode root,
    Dictionary<string, string> captures
  )
  {
    if (pattern is AnyDepthPattern depth)
      return EvaluateAnyDepth(depth.Inner, root, "$", captures);

    var local = new Dictionary<string, string>(StringComparer.Ordinal);
    if (MatchHere(pattern, root, local))
    {
      Merge(captures, local);
      return (Determinacy.Match, "$");
    }
    return (IsUnparsed(root) ? Determinacy.Unknown : Determinacy.NoMatch, null);
  }

  private (Determinacy, string?) EvaluateAnyDepth(
    Pattern inner,
    JsonNode root,
    string path,
    Dictionary<string, string> captures
  )
  {
    var sawUnparsed = false;
    foreach (var (node, nodePath) in DescendantsAndSelf(root, path))
    {
      if (IsUnparsed(node))
        sawUnparsed = true;

      var local = new Dictionary<string, string>(StringComparer.Ordinal);
      if (MatchHere(inner, node, local))
      {
        Merge(captures, local);
        return (Determinacy.Match, nodePath);
      }
    }
    return (sawUnparsed ? Determinacy.Unknown : Determinacy.NoMatch, null);
  }

  private bool MatchHere(Pattern pattern, JsonNode? node, Dictionary<string, string> captures)
  {
    switch (pattern)
    {
      case AnyPattern:
        return node is not null;

      case ScalarEqPattern eq:
        return node is JsonValue && node.ToString() == eq.Value;

      case ScalarInPattern set:
        return node is JsonValue && set.Values.Contains(node.ToString());

      case AnyDepthPattern depth:
        // Nested descendant match is two-valued here (tri-state composition is staged).
        var (d, _) = EvaluateAnyDepth(depth.Inner, node!, "$", captures);
        return d == Determinacy.Match;

      case NodePattern np:
        if (node is not JsonObject obj)
          return false;
        if (np.TypeName is not null && !HasDiscriminator(obj, np.TypeName))
          return false;

        // Accumulate into a pending set so a later failing field leaves no partial captures.
        var pending = new Dictionary<string, string>(StringComparer.Ordinal);
        if (np.Fields is not null)
        {
          foreach (var field in np.Fields)
          {
            if (!obj.TryGetPropertyValue(field.Field, out var value) || value is null)
              return false;
            if (!MatchHere(field.Value, value, pending))
              return false;
          }
        }
        if (np.Capture is not null)
          pending[np.Capture] = CanonicalJson.Serialize(node);
        Merge(captures, pending);
        return true;

      default:
        return false;
    }
  }

  /// <summary>True if <paramref name="obj"/> carries a registered discriminator whose value is
  /// <paramref name="typeName"/>. Validating against the registered <c>(key, value)</c> pairs —
  /// not bare value equality — prevents matching an un-discriminated object that happens to have a
  /// colliding property (e.g. <c>ObjectReference.Kind = "AnyTarget"</c> vs. the <c>Kind</c> ability
  /// discriminator).</summary>
  private bool HasDiscriminator(JsonObject obj, string typeName)
  {
    foreach (var key in _discriminatorKeys)
      if (
        obj.TryGetPropertyValue(key, out var v)
        && v is JsonValue
        && v.ToString() == typeName
        && _discriminators.Contains((key, typeName))
      )
        return true;
    return false;
  }

  private bool IsUnparsed(JsonNode? node)
  {
    if (node is not JsonObject obj)
      return false;
    foreach (var key in _discriminatorKeys)
      if (obj.TryGetPropertyValue(key, out var v) && v is JsonValue && _unparsed.Contains((key, v.ToString())))
        return true;
    return false;
  }

  private static IEnumerable<(JsonNode Node, string Path)> DescendantsAndSelf(
    JsonNode root,
    string path
  )
  {
    yield return (root, path);
    switch (root)
    {
      case JsonObject obj:
        foreach (var kv in obj)
          if (kv.Value is not null)
            foreach (var d in DescendantsAndSelf(kv.Value, $"{path}.{kv.Key}"))
              yield return d;
        break;
      case JsonArray arr:
        for (var i = 0; i < arr.Count; i++)
          if (arr[i] is not null)
            foreach (var d in DescendantsAndSelf(arr[i]!, $"{path}[{i}]"))
              yield return d;
        break;
    }
  }

  private static void Merge(Dictionary<string, string> into, Dictionary<string, string> from)
  {
    foreach (var kv in from)
      into[kv.Key] = kv.Value;
  }
}
