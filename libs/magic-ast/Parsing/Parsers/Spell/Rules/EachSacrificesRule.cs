namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each [player|opponent] sacrifices [a|an|N] [type]." — distributed sacrifice
/// addressed to all players or all opponents (Rule 701.21a — Sacrifice).
/// <list type="bullet">
///   <item>"Each player sacrifices a creature." (Innocent Blood)</item>
///   <item>"Each player sacrifices two creatures." (Barter in Blood)</item>
///   <item>"Each opponent sacrifices a creature." (edict variant)</item>
///   <item>"Each opponent sacrifices an artifact." (Visions of Ruin variant)</item>
///   <item>"Each opponent sacrifices a permanent." (generic)</item>
/// </list>
/// Emits a <see cref="SacrificeEffect"/> whose <see cref="SacrificeEffect.Target"/>
/// carries <see cref="ObjectReferenceKind.EachPlayer"/> or
/// <see cref="ObjectReferenceKind.EachOpponent"/> and an <see cref="ObjectFilter"/>
/// restricting the sacrificed object type.
/// </summary>
[SpellRule]
public sealed class EachSacrificesRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^(?<scope>Each\s+player|Each\s+opponent)\s+sacrifices\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>[a-zA-Z]+)s?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var scope = m.Groups["scope"].Value;
    var countWord = m.Groups["count"].Value;
    var typeWord = m.Groups["type"].Value;

    var playerKind = scope.ToLowerInvariant().Contains("opponent")
      ? ObjectReferenceKind.EachOpponent
      : ObjectReferenceKind.EachPlayer;

    // Resolve the sacrificed-object type to a canonical lowercase card-type.
    var cardType = typeWord.ToLowerInvariant() switch
    {
      "creature" or "creatures" => "creature",
      "artifact" or "artifacts" => "artifact",
      "enchantment" or "enchantments" => "enchantment",
      "permanent" or "permanents" => "permanent",
      "planeswalker" or "planeswalkers" => "planeswalker",
      "land" or "lands" => "land",
      _ => null,
    };

    if (cardType is null)
    {
      return false;
    }

    // Resolve count — "a"/"an"/1 yields no Count (singular is the default).
    Quantity? count = null;
    var countLower = countWord.ToLowerInvariant();
    if (countLower is not ("a" or "an" or "one"))
    {
      if (!SpellRuleHelpers.TryParseSmallWord(countWord, out var n))
      {
        return false;
      }
      count = LiteralQuantity.Of(n);
    }

    effect = new SacrificeEffect
    {
      Target = new ObjectReference
      {
        Kind = playerKind,
        Filter = new ObjectFilter { CardTypes = [cardType] },
      },
      Count = count,
      IsOptional = false,
    };
    return true;
  }
}
