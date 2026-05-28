namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may search your library [and/or graveyard] for a card named [Name],
/// reveal it, [and] put it into your hand. [If you search your library this way,
/// shuffle.]"
///
/// Covers the ETB named-card tutor pattern (Rule 701.23: Search), e.g. Sorin's
/// Guide. Distinct from <see cref="SearchLibraryToHandTriggeredRule"/> in two
/// ways the older rule cannot express:
///   1. The search target is pinned by exact name ("a card named X") rather than
///      a card-type/subtype category — recorded on <see cref="ObjectFilter.Name"/>.
///   2. The search may span multiple source zones ("library and/or graveyard") —
///      recorded on <see cref="SearchLibraryEffect.Sources"/>. A library-only
///      search leaves Sources null (the default Rule 701.23 case).
///
/// The "you may" prefix is optional; its presence sets IsOptional=true. The
/// reveal clause sets Revealed=true. The trailing conditional-shuffle sentence
/// ("If you search your library this way, shuffle.") is rules-inferred bookkeeping
/// and is folded into the effect rather than modelled separately — the same
/// convention SearchLibraryToHandTriggeredRule uses for its "then shuffle" tail.
///
/// Priority 70 (above SearchLibraryToHandTriggeredRule's 60) so the named/
/// multi-zone shape is recognised before the generic category-tutor rule.
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class SearchForNamedCardTriggeredRule : ITriggeredRule
{
  // Anchors the card name between "for a card named " and ", reveal it" so the
  // commas inside the name (e.g. "Sorin, Vampire Lord") stay part of the capture.
  // The <sources> group captures the searched zone phrase ("your library",
  // "your library and/or graveyard", etc.). The trailing conditional-shuffle
  // sentence is optional and consumed but not re-modelled.
  private static readonly Regex _pattern = new(
    @"^(?:you\s+may\s+)?search\s+(?<sources>your\s+library(?:\s+and/or\s+graveyard)?)\s+"
    + @"for\s+a\s+card\s+named\s+(?<name>.+?),\s*reveal\s+it,\s*(?:and\s+)?"
    + @"put\s+it\s+into\s+your\s+hand"
    + @"(?:\.\s*if\s+you\s+search\s+your\s+library\s+this\s+way,\s*shuffle)?"
    + @"(?:,\s*then\s+shuffle)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var isOptional = text.TrimStart().StartsWith("you may", StringComparison.OrdinalIgnoreCase);
    var name = m.Groups["name"].Value.Trim();

    var sources = ParseSources(m.Groups["sources"].Value);

    effect = new SearchLibraryEffect
    {
      Filter = new ObjectFilter { Name = name },
      Count = LiteralQuantity.Of(1),
      Sources = sources,
      Destination = SearchDestination.Hand,
      Revealed = true,
      IsOptional = isOptional,
    };
    return true;
  }

  /// <summary>
  /// Maps the searched-zone phrase to the source-zone list. Returns
  /// <see langword="null"/> for the bare library-only case so the effect keeps
  /// the implicit-library default (matching every pre-existing fixture); returns
  /// an explicit list only when extra zones are named.
  /// </summary>
  private static IReadOnlyList<Zone>? ParseSources(string sourcesPhrase)
  {
    var lower = sourcesPhrase.ToLowerInvariant();
    if (lower.Contains("graveyard"))
    {
      return [Zone.Library, Zone.Graveyard];
    }

    // "your library" alone — leave Sources null (library-implicit default).
    return null;
  }
}
