namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// Parses the Mycosynth Lattice "Players may spend mana as though it were mana
/// of any color" oracle template: a continuous static permission that lets all
/// players pay costs with any mana as though it were colored.
///
/// <para>
/// CR 609.4b (verbatim): "If an effect allows a player to spend mana 'as though
/// it were mana of any [type or color],' this affects only how the player may pay
/// a cost. It doesn't change that cost, and it doesn't change what mana was
/// actually spent to pay that cost."
/// </para>
///
/// <para>
/// The "Players" subject identifies all players globally. Future variants with
/// "You may spend mana" would emit the same node type with
/// <c>Beneficiary = "You"</c>.
/// </para>
///
/// <para>
/// Priority 967 — below <see cref="AllObjectsAreColorlessRule"/> (968).
/// </para>
/// </summary>
[StaticRule(Priority = 967)]
public sealed class PlayersSpendManaAsAnyColorRule : IStaticRule
{
  // Anchored exact match for the global mana-flexibility sentence.
  // Handles: "Players may spend mana as though it were mana of any color."
  private static readonly Regex _pattern = new(
    @"^\s*(?<beneficiary>Players|You)\s+may\s+spend\s+mana\s+as\s+though\s+it\s+were\s+mana\s+of\s+any\s+color\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var beneficiary = m.Groups["beneficiary"].Value.Trim();

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new SpendManaAsAnyColorEffect
          {
            Beneficiary = beneficiary,
          },
        ],
      },
    ];
  }
}
