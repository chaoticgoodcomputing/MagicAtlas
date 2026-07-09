namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player sacrifices [a|an|one|N] [type] [of their choice] unless they
/// pay {COST}" — the taxing-edict triggered form (Spelltithe Enforcer:
/// "Whenever an opponent casts a spell, that player sacrifices a permanent of
/// their choice unless they pay {1}.").
///
/// <para>
/// Rule 701.21 (Sacrifice) — the affected player always chooses which
/// permanent they sacrifice, so "of their choice" is mechanically inert and is
/// matched-and-discarded (mirroring <see cref="EachPlayerSacrificesTriggeredRule"/>).
/// Rule 118.1 — "A cost is an action or payment necessary to take another action
/// or to stop another action from taking place"; the "unless they pay {COST}"
/// clause is the payment the named player ("that player" / "they") may make to
/// stop the sacrifice from happening.
/// </para>
///
/// <para>
/// Produces a <see cref="MagicAST.AST.Effects.Core.PreventableEffect"/> wrapping
/// a <see cref="SacrificeEffect"/>. The sacrificing player rides on the
/// <see cref="SacrificeEffect.Target"/>'s <see cref="ObjectReferenceKind.ThatPlayer"/>
/// (the same Target-holds-the-player convention used by
/// <see cref="EachPlayerSacrificesTriggeredRule"/>); the sacrificed-object type
/// rides on that reference's <see cref="ObjectFilter"/>. The
/// <see cref="UnlessClause"/> Player is also <see cref="ObjectReferenceKind.ThatPlayer"/>
/// ("they" refers back to that player). "a"/"an"/"one" yields no
/// <see cref="SacrificeEffect.Count"/> (singular is the default).
/// </para>
///
/// <para>
/// Distinct from <see cref="SacrificeUnlessPayTriggeredRule"/> ("sacrifice it
/// unless you pay …", the controller's own bounce-land sacrifice) by the
/// leading "that player" scope and the "they pay" payer.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ThatPlayerSacrificesUnlessPayTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+sacrifices\s+"
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
        Kind = ObjectReferenceKind.ThatPlayer,
        Filter = new ObjectFilter { CardTypes = [cardType] },
      },
      Count = count,
    };

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(
      sacrifice,
      new UnlessClause
      {
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        Cost = manaCost,
      }
    );
    return true;
  }
}
