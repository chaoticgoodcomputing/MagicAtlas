namespace MagicAST.Interaction;

/// <summary>
/// CR 111.10 predefined tokens, resolved to their intrinsic activated ability (ADR-0002 §9). A created
/// Treasure is not just an <c>emit:token</c> leaf — it is an object carrying "{T}, Sacrifice this
/// token: add one mana of any color", so it brings a <em>self-sacrifice</em> consume (plus tap /
/// generic-mana / discard costs) that drives the resource it emits. Each <see cref="Spec"/> is a
/// transformative derivation of the token's CR 111.10 characteristics — not raw rules text. Consumed
/// by <see cref="PortWalk"/>'s §9 resolution.
/// </summary>
internal static class PredefinedTokens
{
  /// <param name="Taps">The ability taps the token (<c>{T}</c> in its cost).</param>
  /// <param name="GenericMana">A <c>{N}</c> generic-mana activation cost (0 = none).</param>
  /// <param name="Discards">The ability discards a card as a cost (Blood).</param>
  /// <param name="Emit">The §3 colon-label of the resource the token emits.</param>
  internal sealed record Spec(bool Taps, int GenericMana, bool Discards, string Emit);

  internal static readonly IReadOnlyDictionary<string, Spec> Registry = new Dictionary<string, Spec>(
    StringComparer.OrdinalIgnoreCase
  )
  {
    // {T}, Sacrifice this token: Add one mana of any color.
    ["Treasure"] = new(Taps: true, GenericMana: 0, Discards: false, Emit: "emit:mana:any"),
  };
}
