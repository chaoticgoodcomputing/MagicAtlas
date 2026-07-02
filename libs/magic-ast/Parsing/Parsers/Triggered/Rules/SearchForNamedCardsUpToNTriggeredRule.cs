namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may search your library for up to N cards named [Name], reveal them,
/// put them into your hand, then shuffle."
///
/// Covers the ETB named-card tutor pattern bounded by a count (Rule 701.23:
/// Search), e.g. Nesting Wurm ("up to three cards named Nesting Wurm"). This
/// is the plural, count-bounded sibling of
/// <see cref="SearchForNamedCardTriggeredRule"/> (which only matches the
/// singular "a card named X, reveal it, put it into your hand" shape) — the
/// "up to N cards"/"reveal them"/"put them" wording here cannot match that
/// rule's singular anchors, and it cannot match
/// <see cref="SearchLibraryToHandTriggeredRule"/> either, since that rule's
/// filter clause is anchored on "for a[n] " while this text begins "for up
/// to N", so the two are disjoint.
///
/// The found cards are revealed (Rule 701.20: Reveal — Revealed=true), put
/// into hand, then the library is shuffled (Rule 701.24: Shuffle). Per the
/// convention set by the direct CR 701.23 precedent template (Veteran
/// Explorer: "search their library for up to two basic land cards, put them
/// onto the battlefield, then shuffle"), the trailing "then shuffle" is
/// rules-inferred bookkeeping folded into the effect rather than modelled
/// as a separate node. The "you may" prefix is optional; its presence sets
/// IsOptional=true via <see cref="MagicAST.AST.Effects.Core.EffectWrap.Optional"/>.
///
/// Priority 80 — above both <see cref="SearchForNamedCardTriggeredRule"/>
/// (70) and <see cref="SearchLibraryToHandTriggeredRule"/> (60); the three
/// rules are mutually disjoint by anchor shape so ordering among them is
/// not load-bearing, but the higher priority keeps this most-specific rule
/// tried first.
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class SearchForNamedCardsUpToNTriggeredRule : ITriggeredRule
{
  // Anchors the card name between "cards named " and ", reveal them" so that
  // commas inside the name would survive (mirrors SearchForNamedCardTriggeredRule).
  private static readonly Regex _pattern = new(
    @"^(?:you\s+may\s+)?search\s+your\s+library\s+for\s+up\s+to\s+"
    + @"(?<count>[a-z]+|\d+)\s+cards\s+named\s+(?<name>.+?),\s*reveal\s+them,\s*"
    + @"put\s+them\s+into\s+your\s+hand(?:,\s*then\s+shuffle)?$",
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
    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(m.Groups["count"].Value) ?? 1;

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new SearchLibraryEffect {
      Filter = new ObjectFilter { Name = name },
      Count = new UpToQuantity { Maximum = count, Minimum = 0 },
      Destination = SearchDestination.Hand,
      Revealed = true}, isOptional);
    return true;
  }
}
