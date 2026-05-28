namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "surveil N." and "you may surveil N." — Rule 701.42 keyword action on the triggered side.
/// The "you may" prefix produces an optional <see cref="SurveilEffect"/> (IsOptional = true).
/// </summary>
[TriggeredRule]
public sealed class SurveilTriggeredRule : ITriggeredRule
{
  // Mandatory: "surveil N"
  private static readonly Regex _mandatoryPattern =
    new(@"^surveil\s+(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  // Optional: "you may surveil N"
  private static readonly Regex _optionalPattern =
    new(@"^you\s+may\s+surveil\s+(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    var mandatory = _mandatoryPattern.Match(trimmed);
    if (mandatory.Success)
    {
      var count = int.Parse(mandatory.Groups[1].Value);
      effect = new SurveilEffect { Count = LiteralQuantity.Of(count) };
      return true;
    }

    var optional = _optionalPattern.Match(trimmed);
    if (optional.Success)
    {
      var count = int.Parse(optional.Groups[1].Value);
      effect = new SurveilEffect { Count = LiteralQuantity.Of(count), IsOptional = true };
      return true;
    }

    return false;
  }
}
