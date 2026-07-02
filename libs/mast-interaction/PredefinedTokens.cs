namespace MagicAST.Interaction;

/// <summary>
/// CR 111.10 predefined tokens, resolved to their intrinsic activated ability (ADR-0002 §9). A created
/// Treasure is not just an <c>emit:token</c> leaf — it is an object carrying "{T}, Sacrifice this
/// token: add one mana of any color", so it brings the costs that consume it (a self-sacrifice, a tap,
/// generic mana, a discard) driving the resource it emits. Each <see cref="Spec"/> is a transformative
/// derivation of the token's CR 111.10 characteristics — not raw rules text. Consumed by
/// <see cref="PortWalk"/>'s §9 resolution.
///
/// <para>Only tokens with an intrinsic <em>activated</em> ability appear here. Predefined <em>creature</em>
/// tokens (Walker — CR 111.10d) and ability-less tokens are absent — there is nothing to resolve, and a
/// blanket entry would mis-assert characteristics (the false-Disjoint trap the operator judge panel
/// flagged).</para>
/// </summary>
internal static class PredefinedTokens
{
  /// <param name="Sacrifices">The ability sacrifices the token (every one but Powerstone, which taps repeatedly).</param>
  /// <param name="Taps">The ability taps the token (<c>{T}</c> in its cost).</param>
  /// <param name="GenericMana">A <c>{N}</c> generic-mana activation cost (0 = none).</param>
  /// <param name="Discards">The ability discards a card as a cost (Blood).</param>
  /// <param name="Emit">The §3 colon-label of the resource the token emits.</param>
  internal sealed record Spec(
    bool Sacrifices,
    bool Taps,
    int GenericMana,
    bool Discards,
    string Emit
  );

  internal static readonly IReadOnlyDictionary<string, Spec> Registry = new Dictionary<string, Spec>(
    StringComparer.OrdinalIgnoreCase
  )
  {
    // 111.10a — {T}, Sacrifice this token: Add one mana of any color.
    ["Treasure"] = new(Sacrifices: true, Taps: true, GenericMana: 0, Discards: false, Emit: "emit:mana:any"),
    // 111.10c — Sacrifice this token: Add one mana of any color. (no tap)
    ["Gold"] = new(Sacrifices: true, Taps: false, GenericMana: 0, Discards: false, Emit: "emit:mana:any"),
    // 111.10h — {T}: Add {C}. (taps repeatedly, never sacrificed)
    ["Powerstone"] = new(Sacrifices: false, Taps: true, GenericMana: 0, Discards: false, Emit: "emit:mana:colorless"),
    // 111.10f — {2}, Sacrifice this token: Draw a card.
    ["Clue"] = new(Sacrifices: true, Taps: false, GenericMana: 2, Discards: false, Emit: "emit:drawcards"),
    // 111.10b — {2}, {T}, Sacrifice this token: You gain 3 life.
    ["Food"] = new(Sacrifices: true, Taps: true, GenericMana: 2, Discards: false, Emit: "emit:gainlife"),
    // 111.10g — {1}, {T}, Discard a card, Sacrifice this token: Draw a card.
    ["Blood"] = new(Sacrifices: true, Taps: true, GenericMana: 1, Discards: true, Emit: "emit:drawcards"),
    // 111.10s — {1}, {T}, Sacrifice this token: Target creature you control explores.
    ["Map"] = new(Sacrifices: true, Taps: true, GenericMana: 1, Discards: false, Emit: "emit:explore"),
  };
}
