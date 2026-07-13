namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player creates (a|X|&lt;num&gt;) &lt;P&gt;/&lt;T&gt; &lt;color&gt; [subtype ...]
/// creature token(s)." (e.g. Gravedig: "Target player creates a 2/2 black Zombie creature
/// token.")
///
/// <para>
/// The creator of the token is a TARGETED player rather than the ability's controller —
/// CR 111.2: "if an effect of a resolving spell or ability creates a token, it enters the
/// battlefield under the control of the player or that spell or ability's controller
/// specified by that effect." Sibling of <see cref="CreateTokenRule"/>, which handles the
/// implicit-"you" subject form ("Create a ..."); this rule's subject is the named target
/// player instead, so <see cref="CreateTokenEffect.Player"/> carries a Target reference with
/// an <c>ObjectFilter{CardTypes:["player"]}</c> filter rather than <c>ObjectReference.You()</c>.
/// </para>
/// </summary>
[SpellRule]
public sealed class CreateTokenForTargetPlayerRule : ISpellRule
{
  private static readonly Regex CreaturePattern = new(
    @"^Target\s+player\s+creates\s+(?<count>a|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtypes>(?:\w+\s+)+)creature\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> ColorMap = new Dictionary<string, string>(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W",
    ["blue"] = "U",
    ["black"] = "B",
    ["red"] = "R",
    ["green"] = "G",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = CreaturePattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var rawCount = m.Groups["count"].Value;
    Quantity count;
    var rawLower = rawCount.ToLowerInvariant();
    if (rawLower is "x" or "y" or "z")
    {
      count = new VariableQuantity { Name = rawLower.ToUpperInvariant() };
    }
    else
    {
      count = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(rawCount));
    }

    var colorCode = ColorMap[m.Groups["color"].Value];
    var power = m.Groups["power"].Value;
    var toughness = m.Groups["toughness"].Value;
    // Split the subtypes group (e.g. "Zombie ") into individual canonical subtype
    // strings, matching CreateTokenRule's multi-subtype handling.
    var subtypes = m.Groups["subtypes"].Value
      .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .Select(s => char.ToUpperInvariant(s[0]) + s[1..])
      .ToArray();

    effect = new CreateTokenEffect
    {
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
      Count = count,
      Token = new TokenDefinition
      {
        Power = power,
        Toughness = toughness,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = subtypes,
        IsCopy = false,
      },
    };
    return true;
  }
}
