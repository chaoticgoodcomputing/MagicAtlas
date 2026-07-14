namespace MagicAtlas.Flows.Shared;

/// <summary>
/// The canonical resource-FAMILY taxonomy shared by the port-graph diagnostics — the single source of
/// truth mapping a colon-<see cref="MagicAST.Interaction.PortLabel"/> to its flowing-resource family
/// (mana, token, sacrifice, dice, …), plus the set of families that form real resource cycles. Extracted
/// so <c>PortGraphAtlas</c> (the structural map) and <c>CardAtlas</c> (D1–D4, the deckbuilding datasets)
/// agree on families byte-for-byte.
///
/// <para>Promoted from tests/magic-ast-tests/Flows/Shared/ResourceFamilies.cs (the canonical taxonomy).</para>
/// </summary>
public static class ResourceFamilies
{
  /// <summary>The canonical flowing resources — the "periodic table" the arms move. Everything else
  /// (coarse <c>emit:&lt;effect-type&gt;</c> fallbacks) is inert and excluded from the cycle families.</summary>
  public static readonly IReadOnlySet<string> Canonical = new HashSet<string>(StringComparer.Ordinal)
  {
    "mana", "token", "sacrifice", "death", "etb", "recur", "dice", "damage",
    "life", "blink", "copy", "cast", "combat", "untap", "tap", "counter", "phase",
  };

  /// <summary>The resource FAMILY a colon-label belongs to (groups an emit + its matching trigger/cost
  /// onto the same flowing resource: <c>emit:rolldice</c> &amp; <c>trigger:rolldice</c> → "dice").</summary>
  public static string Of(string label)
  {
    var parts = label.Split(':');
    var role = parts[0];
    return role switch
    {
      "emit" => Resource(parts.Length > 1 ? parts[1] : "emit"),
      "trigger" => Resource(parts.Length > 1 ? parts[1] : "trigger"),
      "pay" => "mana",
      "tap" => "tap",
      "sac" => "sacrifice",
      "etb" => "etb",
      "ltb" => "death",
      "at" => "phase",
      "cast" => "cast",
      "attacksorblocks" => "combat",
      "replace" => "replacement",
      "intercept" => "intercept",
      _ => role,
    };
  }

  private static string Resource(string kind) =>
    kind switch
    {
      "rolldice" => "dice",
      "additionalcombat" => "combat",
      "returntobattlefield" => "recur",
      "returntohand" => "recur",
      "damage" => "damage",
      "life" => "life",
      "mana" => "mana",
      "token" => "token",
      "counter" => "counter",
      "blink" => "blink",
      "copy" => "copy",
      "cast" => "cast",
      "untap" => "untap",
      _ => kind,
    };
}
