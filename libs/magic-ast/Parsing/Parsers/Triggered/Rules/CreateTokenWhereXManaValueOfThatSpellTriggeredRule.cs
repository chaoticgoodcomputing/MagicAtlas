namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create X [P]/[T] [color] [Subtype][ Subtype] creature tokens, where X is the
/// mana value of that spell." — the cast-triggered token-count sibling of
/// <see cref="CreateTokenWhereXCountTriggeredRule"/> (which resolves X to a count
/// of controlled permanents), reached from a "Whenever you cast a[n] [X] spell, ..."
/// trigger. Covers Ovika, Enigma Goliath: "Whenever you cast a noncreature spell,
/// create X 1/1 red Phyrexian Goblin creature tokens, where X is the mana value of
/// that spell."
///
/// <para>
/// CR 111.2 (token creation); CR 202.3 (mana value). X is not a free variable
/// (CR 107.3) — the "where X is …" clause defines it inline as the mana value of
/// the spell that caused the trigger to fire, i.e. the object anaphorically named
/// "that spell". MAST records this as a <see cref="DerivedQuantity"/> with
/// <c>DerivedFrom = ManaValue</c> and <c>Source = "that spell"</c>, mirroring the
/// Food Chain "exiled creature's mana value" shape (<c>DerivedQuantity</c> with a
/// named <c>Source</c> string; ADR 0004 reference-not-resolution — the engine
/// resolves the actual number at trigger time).
/// </para>
///
/// <para>
/// Priority above the generic <see cref="CreateTokenRule"/> (default 50) and at
/// the same tier as its "count of controlled permanents" sibling (90) so this
/// more-specific "where X is the mana value of that spell" shape is claimed
/// before any less-specific fallback. Fully anchored (^…$) and its tail phrase
/// ("mana value of that spell") is disjoint from the sibling's tail ("number of
/// [Subtype]s you control"), so the two rules cannot collide.
/// </para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class CreateTokenWhereXManaValueOfThatSpellTriggeredRule : ITriggeredRule
{
  // "Create X 1/1 red Phyrexian Goblin creature tokens, where X is the mana value of that spell."
  private static readonly Regex _pattern = new(
    @"^Create\s+X\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green|colorless)\s+"
      + @"(?<subtypes>[A-Za-z]+(?:\s+[A-Za-z]+)?)\s+creature\s+tokens?\s*,\s+where\s+X\s+is\s+the\s+"
      + @"mana\s+value\s+of\s+that\s+spell$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorMap = new(StringComparer.OrdinalIgnoreCase)
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var colorWord = m.Groups["color"].Value;
    List<string> colors;
    if (string.Equals(colorWord, "colorless", StringComparison.OrdinalIgnoreCase))
    {
      // CR 105.1: colorless is not a color; the token's Colors list carries the
      // "C" marker, mirroring the sibling rule's convention.
      colors = ["C"];
    }
    else if (_colorMap.TryGetValue(colorWord, out var colorCode))
    {
      colors = [colorCode];
    }
    else
    {
      return false;
    }

    var subtypes = m.Groups["subtypes"].Value
      .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant())
      .ToList();

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.ManaValue,
        Source = "that spell",
      },
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = colors,
        Types = ["creature"],
        Subtypes = subtypes,
        IsCopy = false,
      },
    };
    return true;
  }
}
