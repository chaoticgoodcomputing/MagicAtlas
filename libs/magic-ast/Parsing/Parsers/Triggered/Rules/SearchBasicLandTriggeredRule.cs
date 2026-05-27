namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may search your library for a basic land card, [reveal it,] put [it|that card]
/// onto the battlefield tapped / into your hand, then shuffle."
///
/// Covers the ETB-search-land pattern (Rule 701.23: Search).
/// Two destination variants:
/// - put it into your hand (Destination=Hand, Revealed=true)
/// - put that card onto the battlefield tapped (Destination=BattlefieldTapped, Revealed=false)
/// </summary>
[TriggeredRule]
public sealed class SearchBasicLandTriggeredRule : ITriggeredRule
{
  // Matches both destination variants for basic-land ETB searches.
  // Group <dest> captures the destination phrase after the optional reveal clause.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+search\s+your\s+library\s+for\s+a\s+basic\s+land\s+card,\s*"
    + @"(?:reveal\s+it,\s+)?put\s+(?:it|that\s+card)\s+(?<dest>into\s+your\s+hand|onto\s+the\s+battlefield\s+tapped),\s*then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly ObjectFilter _basicLandFilter = new()
  {
    Supertypes = ["Basic"],
    CardTypes = ["land"],
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var dest = m.Groups["dest"].Value.ToLowerInvariant().Trim();
    bool revealed = dest.StartsWith("into your hand", StringComparison.OrdinalIgnoreCase);
    var destination = revealed ? SearchDestination.Hand : SearchDestination.BattlefieldTapped;

    effect = new SearchLibraryEffect
    {
      Filter = _basicLandFilter,
      Count = LiteralQuantity.Of(1),
      Destination = destination,
      Revealed = revealed,
      IsOptional = true,
    };
    return true;
  }
}
