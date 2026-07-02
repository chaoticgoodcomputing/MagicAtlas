namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.Parsing;

/// <summary>
/// Matches "you get an emblem with [quoted abilities]" — creates an emblem in the command
/// zone whose abilities are parsed from the quoted oracle-text fragment (CR 114.2).
///
/// <para>
/// The quoted string is extracted and re-parsed via <see cref="OracleParser"/> so the
/// emblem abilities receive the same full triggered/static/activated parsing as any other
/// oracle text. The resulting abilities become <see cref="EmblemDefinition.Abilities"/>.
/// </para>
///
/// <para>ANCHORED (^...$): matches only the full "you get an emblem with ..." clause.
/// Priority 90: runs before generic keyword or residual rules.</para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class CreateEmblemTriggeredRule : ITriggeredRule
{
  // Build the regex pattern dynamically using char values so the source file does not
  // need to contain raw quote characters (curly or straight) that confuse the Write tool.
  // U+201C = left double quotation mark, U+201D = right double quotation mark, U+0022 = ASCII quote.
  private static readonly Regex _pattern = BuildPattern();

  private static Regex BuildPattern()
  {
    // Opening quote class: U+201C or U+0022
    var open = new string(new char[] { '[', '“', '"', ']' });
    // Closing quote class: U+201D or U+0022
    var close = new string(new char[] { '[', '”', '"', ']' });
    var patternStr = @"^you\s+get\s+an\s+emblem\s+with\s+" + open + @"(?<quoted>.+)" + close + @"\.?$";
    return new Regex(patternStr, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
  }

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var quotedText = m.Groups["quoted"].Value.Trim();
    if (string.IsNullOrWhiteSpace(quotedText))
    {
      return false;
    }

    // Parse the emblem oracle text through the full oracle parser. CR 114.2: the quoted
    // text is the emblem's oracle text verbatim; parsing it independently is correct.
    var parser = new OracleParser();
    var result = parser.Parse(quotedText);

    if (result.Output.Abilities.Count == 0)
    {
      return false;
    }

    effect = new CreateEmblemEffect
    {
      Emblem = new EmblemDefinition
      {
        Abilities = result.Output.Abilities,
      },
    };
    return true;
  }
}
