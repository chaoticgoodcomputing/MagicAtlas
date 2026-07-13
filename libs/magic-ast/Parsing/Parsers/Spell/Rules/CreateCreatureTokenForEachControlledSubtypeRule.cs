namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create a &lt;P&gt;/&lt;T&gt; &lt;color&gt; &lt;subtype ...&gt; creature token for each
/// &lt;subtype&gt; you control." (Elven Ambush: "Create a 1/1 green Elf Warrior creature
/// token for each Elf you control.")
///
/// <para>
/// The count of tokens created is the number of permanents you control matching the
/// trailing "for each &lt;subtype&gt; you control" clause. That count is modelled as a
/// <see cref="CountQuantity"/> over an <see cref="ObjectFilter"/> — the same shape
/// Elvish Archdruid's "add {G} for each Elf you control" uses for its count. MAST
/// records the object-selection reference; the engine evaluates the count against
/// game state at resolution (ADR 0004 — reference-not-resolution).
/// </para>
///
/// <para>
/// The plain <see cref="CreateTokenRule"/> (Priority 60) anchors on <c>$</c> right
/// after the token description, so its regex cannot match text bearing the trailing
/// "for each … you control" clause; this rule owns that distinct, anchored shape and
/// does not shadow — nor is it shadowed by — the plain rule.
/// </para>
/// </summary>
[SpellRule(Priority = 70)]
public sealed class CreateCreatureTokenForEachControlledSubtypeRule : ISpellRule
{
  // The whole clause is anchored ^…$: a P/T colored creature token whose count is the
  // number of a named subtype you control. The subtypes group captures every word
  // between the color and "creature" (e.g. "Elf Warrior"), split on whitespace.
  private static readonly Regex Pattern = new(
    @"^Create\s+a\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtypes>(?:\w+\s+)+)creature\s+token\s+for\s+each\s+(?<countsub>\w+)\s+you\s+control$",
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

  // The count clause "for each <word> you control" counts permanents you control. When
  // <word> is a card type (creature, land, artifact, …) it belongs on CardTypes; otherwise
  // it is a subtype (Elf, Forest, Saproling, …) and belongs on Subtypes — mirroring how the
  // existing golds model "for each creature you control" (CardTypes:["creature"]) versus
  // "for each Elf you control" (Subtypes:["Elf"]).
  private static readonly HashSet<string> CardTypeWords = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature",
    "artifact",
    "enchantment",
    "land",
    "planeswalker",
    "battle",
  };

  private static string Capitalize(string word) =>
    char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var subtypes = m.Groups["subtypes"].Value
      .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .Select(Capitalize)
      .ToArray();

    var countWord = m.Groups["countsub"].Value;
    var isCardType = CardTypeWords.Contains(countWord);

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = isCardType ? [countWord.ToLowerInvariant()] : null,
          Subtypes = isCardType ? null : [Capitalize(countWord)],
          Controller = ControllerFilter.You,
          Zone = Zone.Battlefield,
        },
      },
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [ColorMap[m.Groups["color"].Value]],
        Types = ["creature"],
        Subtypes = subtypes,
        IsCopy = false,
      },
    };
    return true;
  }
}
