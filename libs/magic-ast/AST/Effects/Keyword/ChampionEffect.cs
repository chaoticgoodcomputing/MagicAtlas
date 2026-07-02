namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Champion (Rule 702.71). A keyword ability that appears as "Champion a [type]"
/// on a creature card. When this creature enters the battlefield, its controller
/// must sacrifice it unless they exile another creature of the named type they
/// control. When this creature leaves the battlefield, the exiled card returns.
/// The <see cref="CreatureType"/> field records the type restriction from the
/// oracle line (e.g., "creature", "Elemental", "Goblin"). "creature" indicates
/// the general form with no subtype restriction. MAST records the keyword's
/// presence and type parameter; the sacrifice-unless and return mechanics are
/// engine territory, not described by the oracle line itself.
/// </summary>
[OracleEffect("champion")]
public sealed record ChampionEffect : Effect
{
  /// <summary>
  /// The creature type (or "creature") that must be exiled to satisfy champion.
  /// Derived from the oracle text: "Champion a creature" → "creature",
  /// "Champion an Elemental" → "Elemental", "Champion a Goblin" → "Goblin".
  /// </summary>
  public required string CreatureType { get; init; }
}
