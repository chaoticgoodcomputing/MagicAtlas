namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.Parsing;

/// <summary>
/// Parses the K'rrik, Son of Yawgmoth static ability:
/// "For each {X} in a cost, you may pay N life rather than pay that mana."
///
/// <para>
/// This is a continuous static permission (CR 604) that allows the controller to
/// substitute N life for each instance of the named mana color in any cost. The
/// mechanic is a generalization of the Phyrexian mana rule (CR 107.4f) applied
/// universally to all costs paid by the controller rather than only to Phyrexian
/// mana symbols on the card itself.
/// </para>
///
/// <para>
/// Pattern: "For each {COLOR} in a cost, you may pay N life rather than pay that mana."
/// Anchored (^…$) to prevent substring matches in sibling trigger/effect contexts.
/// No other static rule in the registry matches this surface phrase — the anchor is
/// belt-and-suspenders against future rules that happen to contain the words "life"
/// and "cost" (the #1 FAIL class per the vertical-slice contract).
/// </para>
/// </summary>
[StaticRule(Priority = 840)]
public sealed class PayLifeForColoredManaRule : IStaticRule
{
  /// <summary>
  /// Matches: "For each {COLOR} in a cost, you may pay N life rather than pay that mana."
  /// Groups:
  ///   color  — the mana color letter(s) inside the braces (e.g. "B", "R")
  ///   amount — the numeric life amount (e.g. "2")
  /// Anchored ^ and $ to prevent substring shadowing.
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^\s*For\s+each\s+\{(?<color>[WUBRG])\}\s+in\s+a\s+cost,\s+you\s+may\s+pay\s+(?<amount>\d+)\s+life\s+rather\s+than\s+pay\s+that\s+mana\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // The mana color letter, normalized to uppercase to match oracle conventions
    // and the ManaColor serialization used elsewhere in the AST.
    var colorLetter = match.Groups["color"].Value.ToUpperInvariant();

    // The life amount per mana symbol.
    if (!int.TryParse(match.Groups["amount"].Value, out var lifeAmount))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new PayLifeForColoredManaEffect
          {
            Colors = [colorLetter],
            LifePerMana = lifeAmount,
          },
        ],
      },
    ];
  }
}
