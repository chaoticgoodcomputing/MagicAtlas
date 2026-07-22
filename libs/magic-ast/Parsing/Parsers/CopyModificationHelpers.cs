namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// Shared construction of the <see cref="CopyModification"/> for an "except it has [ability]" /
/// "that token gains [ability]" clause on a token-copy effect (CR 707.2 copiable-value overrides).
///
/// <para>The bare-keyword case (CR 702 — e.g. "haste", "flying and haste") is the common one and is
/// rules-meaningful, so it is structured as a <see cref="KeywordAbilityAdder"/> carrying the typed
/// <see cref="KeywordAbility"/> identities (casing-proof, matchable — ADR 0001). Anything that is not a
/// pure list of recognised keywords (a quoted triggered/activated ability handled by a dedicated adder,
/// or an unrecognised body) falls back to the <see cref="AbilityAdder.AbilityText"/> free-text escape
/// hatch so no information is lost.</para>
/// </summary>
public static class CopyModificationHelpers
{
  // Split "flying and haste" / "flying, haste" / "flying, trample, and haste" into keyword tokens.
  private static readonly Regex _splitter = new(
    @"\s*,\s*and\s+|\s*,\s*|\s+and\s+",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Build the copy modification for an "…has/gains &lt;abilityText&gt;" clause: a structured
  /// <see cref="KeywordAbilityAdder"/> when every token is a recognised <see cref="KeywordAbility"/>,
  /// otherwise the free-text <see cref="AbilityAdder"/>.
  /// </summary>
  public static CopyModification AbilityGrant(string abilityText)
  {
    var trimmed = abilityText.Trim();
    var keywords = TryParseKeywordList(trimmed);
    return keywords is not null
      ? new KeywordAbilityAdder { Keywords = keywords }
      : new AbilityAdder { AbilityText = trimmed };
  }

  /// <summary>
  /// Parse a keyword-list phrase ("haste", "flying and haste", "vigilance, trample, and haste") into
  /// typed <see cref="KeywordAbility"/> members. Returns <c>null</c> if the phrase is empty or any token
  /// is not a recognised keyword (so the caller can fall back to free text).
  /// </summary>
  public static IReadOnlyList<KeywordAbility>? TryParseKeywordList(string phrase)
  {
    if (string.IsNullOrWhiteSpace(phrase))
      return null;

    var result = new List<KeywordAbility>();
    foreach (var raw in _splitter.Split(phrase))
    {
      var token = raw.Trim();
      if (token.Length == 0)
        continue;
      // Multi-word keywords ("first strike", "double strike") serialize with a space but the enum
      // member is the space-stripped identity; single-word keywords are unaffected.
      if (!Enum.TryParse<KeywordAbility>(token.Replace(" ", string.Empty), ignoreCase: true, out var kw))
        return null;
      result.Add(kw);
    }

    return result.Count > 0 ? result : null;
  }
}
