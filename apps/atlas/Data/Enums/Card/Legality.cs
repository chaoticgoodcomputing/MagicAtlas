using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Legality status in various formats.
/// </summary>
public enum LegalityStatus
{
  [SerializedEnum("legal")]
  Legal,

  [SerializedEnum("not_legal")]
  NotLegal,

  [SerializedEnum("restricted")]
  Restricted,

  [SerializedEnum("banned")]
  Banned,
}
