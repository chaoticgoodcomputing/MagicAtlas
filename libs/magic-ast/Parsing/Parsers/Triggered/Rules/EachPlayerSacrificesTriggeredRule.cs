namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each [player|opponent] sacrifices [a|an|one|N] [type] [of their choice]" —
/// the triggered-ability form of the distributed (edict) sacrifice (CR 701.16 —
/// Sacrifice). Covers the classic ETB edict family:
/// <list type="bullet">
///   <item>"each player sacrifices a creature of their choice." (Fleshbag Marauder, Slum Reaper)</item>
///   <item>"each player sacrifices a land of their choice." (Razing Snidd)</item>
///   <item>"each opponent sacrifices a creature." (edict variant)</item>
///   <item>"each player sacrifices two creatures." (count variant)</item>
/// </list>
///
/// <para>
/// "of their choice" is mechanically inert (CR 701.16a — the affected player
/// always chooses which permanent they sacrifice), so it carries no extra AST;
/// the optional suffix is matched and discarded. This mirrors the spell-level
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.EachSacrificesRule"/> shape:
/// the sacrificed-object type rides on the <see cref="SacrificeEffect.Target"/>'s
/// <see cref="ObjectFilter"/>, and the player scope rides on the target's
/// <see cref="ObjectReferenceKind"/> (<see cref="ObjectReferenceKind.EachPlayer"/>
/// or <see cref="ObjectReferenceKind.EachOpponent"/>). "a"/"an"/"one" yields no
/// <see cref="SacrificeEffect.Count"/> (singular is the default).
/// </para>
///
/// <para>
/// Distinct from <see cref="SacrificeAnyCreatureTriggeredRule"/> ("sacrifice a
/// creature" — the controller alone sacrifices) by the leading
/// "each player/opponent" scope.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class EachPlayerSacrificesTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^(?<scope>each\s+player|each\s+opponent)\s+sacrifices\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>[a-zA-Z]+?)s?(?:\s+of\s+their\s+choice)?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var playerKind = m.Groups["scope"].Value.ToLowerInvariant().Contains("opponent")
      ? ObjectReferenceKind.EachOpponent
      : ObjectReferenceKind.EachPlayer;

    // Resolve the sacrificed-object type to a canonical lowercase card-type.
    var cardType = m.Groups["type"].Value.ToLowerInvariant() switch
    {
      "creature" => "creature",
      "artifact" => "artifact",
      "enchantment" => "enchantment",
      "permanent" => "permanent",
      "planeswalker" => "planeswalker",
      "land" => "land",
      _ => null,
    };

    if (cardType is null)
    {
      return false;
    }

    // "a"/"an"/"one" → no Count (singular is the default); otherwise a literal.
    Quantity? count = null;
    var countLower = m.Groups["count"].Value.ToLowerInvariant();
    if (countLower is not ("a" or "an" or "one"))
    {
      var n = countLower switch
      {
        "two" => 2,
        "three" => 3,
        "four" => 4,
        "five" => 5,
        "six" => 6,
        "seven" => 7,
        "eight" => 8,
        "nine" => 9,
        "ten" => 10,
        _ => int.TryParse(countLower, out var parsed) ? parsed : 0,
      };
      if (n <= 0)
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
    };
    return true;
  }
}
