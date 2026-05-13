using System.Text.RegularExpressions;
using Flowthru.Step;

namespace MagicAtlas.Flows.Ingest.Nodes;

/// <summary>
/// Source step that resolves and fetches the current MTG comprehensive rules text. Wizards' rules
/// index page at <c>https://magic.wizards.com/en/rules</c> links to a dated text file
/// (e.g. <c>MagicCompRules%2020260417.txt</c>) that rotates a few times a year on rules-update
/// release; the file's URL is the first <c>href="...\.txt"</c> on the page.
/// </summary>
/// <remarks>
/// Pattern adapted from <c>docs/reference/misc/external/cgc-mtg-rules/.github/workflows/sync.yaml</c>,
/// which uses the same single-regex scrape against the rules index page. Both fetches use a plain
/// <see cref="HttpClient"/> rather than Flowthru's HTTP-cached storage medium because
/// <see cref="TextBuilder"/> doesn't (yet) compose with the resolver — the rules text is ~1 MB so
/// a fresh download per run is acceptable.
/// </remarks>
[FlowthruStep]
public static partial class FetchRulesTextNode
{
  private const string RulesIndexUrl = "https://magic.wizards.com/en/rules";

  // Matches the first `href="...something.txt"` attribute on the rules index page.
  [GeneratedRegex(@"href=""([^""]*\.txt)""", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
  private static partial Regex RulesTxtLinkPattern();

  public static Func<Task<string>> Create(HttpClient httpClient)
  {
    return async () =>
    {
      var indexHtml = await httpClient.GetStringAsync(RulesIndexUrl);
      var match = RulesTxtLinkPattern().Match(indexHtml);
      if (!match.Success)
      {
        throw new InvalidOperationException(
          $"No 'href=\"...\\.txt\"' link found on {RulesIndexUrl}. Wizards may have changed the "
            + "rules-page layout — update the scraper pattern in FetchRulesTextNode."
        );
      }
      var rulesUrl = match.Groups[1].Value;
      var raw = await httpClient.GetStringAsync(rulesUrl);
      return NormalizeRulesText(raw);
    };
  }

  /// <summary>
  /// Wizards publishes the rules .txt with a UTF-8 BOM and CRLF line endings; the downstream
  /// <c>SplitSectionsNode</c> uses literal <c>"\nGlossary\n"</c> / <c>"\nCredits\n"</c>
  /// delimiters, so we normalize both here so the rest of the pipeline can stay
  /// line-ending-agnostic.
  /// </summary>
  private static string NormalizeRulesText(string raw)
  {
    var s = raw;
    if (s.Length > 0 && s[0] == '﻿') s = s.Substring(1);
    s = s.Replace("\r\n", "\n").Replace("\r", "\n");
    return s;
  }
}
