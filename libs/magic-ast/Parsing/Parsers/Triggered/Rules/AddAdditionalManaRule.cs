namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "add an additional [mana]" — triggered mana-doubling effect. The effect clause of a
/// mana-doubling trigger, covering two subject forms and three mana forms:
///
/// <list type="bullet">
///   <item>
///     Bare subject "add an additional {X}" (Forsaken Monument: "Whenever you tap a permanent
///     for {C}, add an additional {C}") — the player is the implicit ability controller, so
///     <see cref="AddManaEffect.Player"/> is null.
///   </item>
///   <item>
///     Named subject "its controller adds an additional …" — the mana-doubler Aura family
///     (Fertile Ground, Wild Growth, Glittering Frost, Market Festival). "Its controller" is
///     the controller of the enchanted land that was tapped, recorded as
///     <see cref="AddManaEffect.Player"/> = <see cref="ObjectReferenceKind.Controller"/>.
///   </item>
/// </list>
///
/// The mana form is one of:
/// <list type="bullet">
///   <item>a specific symbol — "{G}" (Wild Growth) → <see cref="AddManaEffect.Mana"/>;</item>
///   <item>"one mana of any color" (Fertile Ground, Glittering Frost) →
///   <see cref="AddManaEffect.AnyColor"/> with empty <see cref="AddManaEffect.Mana"/>;</item>
///   <item>"&lt;n&gt; mana in any combination of colors" (Market Festival) →
///   <see cref="AddManaEffect.Amount"/> + <see cref="AddManaEffect.AnyCombinationOf"/> over the
///   five colors, with empty <see cref="AddManaEffect.Mana"/>.</item>
/// </list>
///
/// The word "additional" distinguishes this from the baseline <see cref="AddManaRule"/> which
/// handles "add {X}" directly. CR 106.4: "When an effect instructs a player to add mana, that
/// mana goes into a player's mana pool." The "additional" qualifier is descriptive context for
/// the trigger (the named player gets one more mana on top of what the tapping already produced);
/// MAST models what the oracle text says.
/// </summary>
[TriggeredRule]
public sealed class AddAdditionalManaRule : ITriggeredRule
{
  // Optional "its controller " subject, then "adds/add an additional", then the mana form.
  // The subject toggles between "its controller adds" (Player = Controller) and the bare
  // "add" (Player = null). Both verbs ("add"/"adds") are accepted.
  private static readonly Regex _pattern = new(
    @"^(?<subject>its\s+controller\s+)?adds?\s+an?\s+additional\s+(?<mana>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Specific mana symbol — "{G}".
  private static readonly Regex _symbol = new(
    @"^\{[A-Z0-9/]+\}$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "one mana of any color".
  private static readonly Regex _anyColor = new(
    @"^one\s+mana\s+of\s+any\s+color$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "<n> mana in any combination of colors" — n is a number word.
  private static readonly Regex _anyCombination = new(
    @"^(?<count>one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+mana\s+in\s+any\s+combination\s+of\s+colors$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly string[] _fiveColors = ["W", "U", "B", "R", "G"];

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var t = text.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(t);
    if (!m.Success)
    {
      return false;
    }

    // "its controller adds" → the player is the controller of the enchanted land
    // (the permanent tapped for mana); the bare "add" leaves Player implicit (null).
    ObjectReference? player = m.Groups["subject"].Success
      ? new ObjectReference { Kind = ObjectReferenceKind.Controller }
      : null;

    var manaText = m.Groups["mana"].Value.Trim();

    // Specific mana symbol — "{G}".
    if (_symbol.IsMatch(manaText))
    {
      effect = new AddManaEffect { Mana = manaText.ToUpperInvariant(), Player = player };
      return true;
    }

    // "one mana of any color".
    if (_anyColor.IsMatch(manaText))
    {
      effect = new AddManaEffect { Mana = string.Empty, AnyColor = true, Player = player };
      return true;
    }

    // "<n> mana in any combination of colors".
    var combo = _anyCombination.Match(manaText);
    if (combo.Success)
    {
      var count = ParseCount(combo.Groups["count"].Value);
      if (count <= 0)
      {
        return false;
      }
      effect = new AddManaEffect
      {
        Mana = string.Empty,
        Amount = LiteralQuantity.Of(count),
        AnyCombinationOf = _fiveColors,
        Player = player,
      };
      return true;
    }

    return false;
  }

  private static int ParseCount(string raw) =>
    raw.ToLowerInvariant() switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.TryParse(raw, out var n) ? n : 0,
    };
}
