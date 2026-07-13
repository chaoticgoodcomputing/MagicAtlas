namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Enchant Swamp" / "Enchant Island" / etc. — the Enchant keyword ability (CR
/// 702.5) qualified by a single basic land subtype (CR 305.6) rather than the
/// generic "land"/"creature" noun that <see cref="EnchantRule"/> handles. The
/// legal-target descriptor is a basic land TYPE word, not a card-type noun, so it
/// lands on <see cref="ObjectFilter.Subtypes"/> (CardTypes still records "land").
///
/// <para>
/// Canonical corpus: the Kamigawa "Genju" cycle (Genju of the Fens enchants Swamp,
/// siblings enchant the other four basic land types) — "Enchant Swamp\n{2}: ...".
/// </para>
///
/// <para>
/// Priority 993 — above <see cref="EnchantRule"/> (992), which returns null for a
/// bare basic-land-type descriptor (only the generic noun set is in its
/// simpleTypes whitelist), so this more specific shape is tried first.
/// </para>
///
/// Rule 702.5a: "Enchant [object or player] is a static ability ... that restricts
/// what an Aura ... can legally enchant." Rule 305.6: "The basic land types are
/// Plains, Island, Swamp, Mountain, and Forest."
/// </summary>
[StaticRule(Priority = 993)]
public sealed class EnchantBasicLandTypeRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Enchant\s+(?<subtype>Plains|Island|Swamp|Mountain|Forest)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var subtype = match.Groups["subtype"].Value;

    return
    [
      new StaticAbility
      {
        KeywordSource = KeywordAbility.Enchant,
        Effects = [new EnchantRestrictionEffect
        {
          LegalTargets = new ObjectFilter
          {
            CardTypes = ["land"],
            Subtypes = [subtype],
          },
        }],
      },
    ];
  }
}
