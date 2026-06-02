namespace MagicAST.Interaction;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Query;
using MagicAST.Query.Patterns;
using MagicAST.Schema;

/// <summary>
/// Derives <see cref="Port"/>s from a parsed card AST — the <em>derived</em> layer of
/// mast-interaction ADR-0001 (§3). Runs port-recognizer patterns (mast-query) over each ability
/// sub-tree, capturing subject <c>ObjectFilter</c>s via typed captures and translating token specs,
/// to build each ability's emit / consume / intercept resource sets. Port identity is the
/// canonical-subtree hash, so a port has one identity across processes.
/// </summary>
public sealed class PortProjector
{
  private readonly FilterAndVerifyEngine _engine;

  public PortProjector(AstSchema schema) => _engine = new FilterAndVerifyEngine(schema);

  // --- recognizer patterns, authored against the serialized AST shapes ---

  /// <summary>A triggered ability whose trigger is a death event — captures the dying-object filter.</summary>
  private static readonly Pattern DeathTrigger = new NodePattern
  {
    Fields =
    [
      new(
        "Trigger",
        new NodePattern
        {
          Fields =
          [
            new("Event", new ScalarEqPattern("Dies")),
            new("Filter", new NodePattern { Capture = "deathFilter" }),
          ],
        }
      ),
    ],
  };

  /// <summary>A <c>createToken</c> effect anywhere in the ability — captures the token spec and creator.</summary>
  private static readonly Pattern CreateToken = new AnyDepthPattern(
    new NodePattern
    {
      Fields =
      [
        new("EffectType", new ScalarEqPattern("createToken")),
        new("Token", new NodePattern { Capture = "token" }),
        new("Player", new NodePattern { Capture = "creator" }),
      ],
    }
  );

  /// <summary>A replacement effect intercepting token creation (a doubler).</summary>
  private static readonly Pattern TokenReplacement = new AnyDepthPattern(
    new NodePattern
    {
      Fields =
      [
        new("EffectType", new ScalarEqPattern("replacement")),
        new(
          "Event",
          new NodePattern { Fields = [new("EventType", new ScalarEqPattern("tokenCreation"))] }
        ),
      ],
    }
  );

  /// <summary>A sacrifice cost — captures the fodder filter.</summary>
  private static readonly Pattern SacrificeCost = new AnyDepthPattern(
    new NodePattern
    {
      Fields =
      [
        new("CostType", new ScalarEqPattern("sacrifice")),
        new("Filter", new NodePattern { Capture = "fodder" }),
      ],
    }
  );

  /// <summary>Project every resource-bearing ability of <paramref name="card"/> into a port.</summary>
  public IReadOnlyList<Port> Project(string card, JsonNode? oracleAbilities)
  {
    var ports = new List<Port>();
    if (oracleAbilities is not JsonArray abilities)
      return ports;

    foreach (var ability in abilities)
    {
      if (ability is null)
        continue;

      var emits = new List<Resource>();
      var consumes = new List<Resource>();
      var intercepts = new List<Resource>();
      string? label = null;

      // Death-trigger consumer (Pitiless): "whenever [filter] dies".
      var death = _engine.Match(DeathTrigger, ability);
      if (death.Determinacy == Determinacy.Match && Filter(death, "deathFilter") is { } dyingFilter)
      {
        consumes.Add(new Resource(ResourceKind.Death, dyingFilter));
        label = "death-payoff";
      }

      // Token interceptor / doubler (Chatterfang replacement on token creation).
      if (_engine.Match(TokenReplacement, ability).Determinacy == Determinacy.Match)
      {
        intercepts.Add(new Resource(ResourceKind.Token, new ObjectFilter { IsToken = true }));
        label = "token-doubler";
      }

      // Token emitter (createToken anywhere — Pitiless's Treasure, the doubler's Squirrels).
      var create = _engine.Match(CreateToken, ability);
      if (create.Determinacy == Determinacy.Match && Token(create, "token") is { } token)
        emits.Add(new Resource(ResourceKind.Token, token));

      // Sacrifice outlet (Chatterfang): consumes the fodder, which then dies.
      var sacrifice = _engine.Match(SacrificeCost, ability);
      if (sacrifice.Determinacy == Determinacy.Match && Filter(sacrifice, "fodder") is { } fodder)
      {
        // CR 701.21a: a player can only sacrifice a permanent they control — so the fodder (and the death it
        // produces) is "you control", even though the oracle text doesn't restate it. A sound
        // resource-ontology fact (C2): the parser stays text-faithful, the rules-invariant lives here.
        var owned = fodder with { Controller = fodder.Controller ?? ControllerFilter.You };
        consumes.Add(new Resource(ResourceKind.Token, owned));
        emits.Add(new Resource(ResourceKind.Death, owned));
        label = "sac-outlet";
      }

      if (emits.Count == 0 && consumes.Count == 0 && intercepts.Count == 0)
        continue; // not a resource port

      ports.Add(
        new Port
        {
          Card = card,
          Label = label ?? "emitter",
          Identity = $"{card}:{CanonicalJson.Hash(ability)}",
          Emits = emits,
          Consumes = consumes,
          Intercepts = intercepts,
        }
      );
    }
    return ports;
  }

  private static ObjectFilter? Filter(MatchOutcome match, string capture) =>
    match.Captures is not null && match.Captures.TryGetValue(capture, out var node)
      ? node.Deserialize<ObjectFilter>(MagicAST.MagicASTJsonOptions.Strict)
      : null;

  /// <summary>Translate a token-spec node (<c>Types</c>/<c>Subtypes</c>) into the ObjectFilter the join uses.</summary>
  private static ObjectFilter? Token(MatchOutcome match, string capture)
  {
    if (
      match.Captures is null
      || !match.Captures.TryGetValue(capture, out var node)
      || node is not JsonObject obj
    )
      return null;
    return new ObjectFilter
    {
      CardTypes = StringList(obj["Types"]),
      Subtypes = StringList(obj["Subtypes"]),
      IsToken = true,
      // CR 111.2: a token's creator controls it. The createToken effect's Player is the creator —
      // when it's the ability's controller (You), the emitted token is yours, so the join can prove
      // reliability. Any other / runtime creator stays null so the operator floors to Unknown — never
      // a false "you control". (Mirrors C2's CR 701.21a treatment for the sacrifice side.)
      Controller = CreatorControl(match),
    };
  }

  /// <summary>The created token's controller from the effect's creator Player (CR 111.2): You → You, else null.</summary>
  private static ControllerFilter? CreatorControl(MatchOutcome match) =>
    match.Captures is not null
    && match.Captures.TryGetValue("creator", out var p)
    && p is JsonObject creator
    && creator["Kind"]?.ToString() == "You"
      ? ControllerFilter.You
      : null;

  private static IReadOnlyList<string>? StringList(JsonNode? node) =>
    node is JsonArray arr
      ? arr.Where(x => x is not null).Select(x => x!.ToString()).ToList()
      : null;
}
