namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each [player|opponent] sacrifices [a|an|one|N] [type] [of their choice] unless
/// they pay {COST}" — the distributed taxing-edict triggered form (Rishadan
/// Cutpurse: "When this creature enters, each opponent sacrifices a permanent of
/// their choice unless they pay {1}.").
///
/// <para>
/// CR 701.21 (Sacrifice) — the affected player always chooses which permanent
/// they sacrifice, so "of their choice" is mechanically inert and is
/// matched-and-discarded (mirroring
/// <see cref="EachPlayerSacrificesTriggeredRule"/>). CR 118.1 — "A cost is an
/// action or payment necessary to take another action or to stop another action
/// from taking place"; the "unless they pay {COST}" clause is the payment each
/// affected player may make to stop their own sacrifice from happening.
/// </para>
///
/// <para>
/// Produces a <see cref="MagicAST.AST.Effects.Core.PreventableEffect"/> wrapping a
/// <see cref="SacrificeEffect"/>. The sacrificing scope rides on the
/// <see cref="SacrificeEffect.Target"/>'s <see cref="ObjectReferenceKind"/>
/// (<see cref="ObjectReferenceKind.EachPlayer"/> or
/// <see cref="ObjectReferenceKind.EachOpponent"/> — the same
/// Target-holds-the-player convention used by
/// <see cref="EachPlayerSacrificesTriggeredRule"/>); the sacrificed-object type
/// rides on that reference's <see cref="ObjectFilter"/>. The
/// <see cref="UnlessClause"/> Player carries the same scope ("they" refers back
/// to each affected player). "a"/"an"/"one" yields no
/// <see cref="SacrificeEffect.Count"/> (singular is the default).
/// </para>
///
/// <para>
/// Distinct from <see cref="ThatPlayerSacrificesUnlessPayTriggeredRule"/> ("that
/// player sacrifices … unless they pay …", the single triggering player) by the
/// leading "each player/opponent" distributed scope, and from
/// <see cref="EachPlayerSacrificesTriggeredRule"/> (same scope, no payment option)
/// by the required "unless they pay {COST}" suffix.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class EachOpponentSacrificesUnlessPayTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^each\s+(?<scope>player|opponent)\s+sacrifices\s+"
    + @"(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+"
    + @"(?<type>[a-zA-Z]+?)s?(?:\s+of\s+their\s+choice)?\s+"
    + @"unless\s+they\s+pay\s+(?<cost>(?:\{[^}]+\})+)$",
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

    var playerKind = m.Groups["scope"].Value.ToLowerInvariant() == "opponent"
      ? ObjectReferenceKind.EachOpponent
      : ObjectReferenceKind.EachPlayer;

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

    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(m.Groups["cost"].Value);
    if (manaCost is null)
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

    var sacrifice = new SacrificeEffect
    {
      Target = new ObjectReference
      {
        Kind = playerKind,
        Filter = new ObjectFilter { CardTypes = [cardType] },
      },
      Count = count,
    };

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(
      sacrifice,
      new UnlessClause
      {
        Player = new ObjectReference { Kind = playerKind },
        Cost = manaCost,
      }
    );
    return true;
  }
}
