namespace MagicAST.Parsing;

using System.Collections;
using System.Reflection;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// Derives a card's color identity from its parsed AST, per CR 903.4:
/// "The color identity of a card is the color or colors of any mana symbols in
/// that card's mana cost or rules text, plus any colors defined by its
/// characteristic-defining abilities (rule 604.3) or color indicator (rule 204)."
///
/// <para>
/// Color identity is a DERIVED property — nothing about it is printed on the card
/// face. We compute it rather than trust a source database's pre-computed value,
/// so the parser stays the single source of truth and can reflect a rules change
/// before a fixed dataset would.
/// </para>
///
/// <para>
/// CR 903.4c — "Reminder text is ignored when determining a card's color identity"
/// — is honored structurally, not by string-stripping: we collect colors only from
/// real <see cref="ManaSymbol"/> nodes (and the literal mana symbols in an
/// <see cref="AddManaEffect"/>'s produced mana). Mana symbols that appear only in
/// reminder text (e.g. Firebending's "(…add {R}{R}…)") are never parsed into
/// <see cref="ManaSymbol"/> nodes — they remain raw text on a Parenthetical — so
/// they are excluded for free. Conversely, a colored KEYWORD cost (Cycling {U},
/// Bestow {W}, a colored equip cost) IS a structured <see cref="ManaSymbol"/> in
/// the rules text and DOES count — see the CR 903.4 Bosh, Iron Golem example, whose
/// activated-ability cost "{3}{R}" makes it red. We therefore special-case nothing:
/// every structured mana symbol on the card contributes, reminder excluded by
/// construction.
/// </para>
/// </summary>
public static class ColorIdentityDeriver
{
  private static readonly string[] Wubrg = ["W", "U", "B", "R", "G"];

  /// <summary>
  /// Walks the entire card AST (mana cost attribute, every ability/cost/effect,
  /// and all card faces — CR 903.4d includes the back face) and returns the
  /// WUBRG-ordered set of colors implied by its mana symbols.
  /// </summary>
  public static List<string> Derive(object? cardAst)
  {
    var colors = new HashSet<string>();
    Walk(cardAst, colors, new HashSet<object>(ReferenceEqualityComparer.Instance));
    return Wubrg.Where(colors.Contains).ToList();
  }

  private static void Walk(object? node, HashSet<string> colors, HashSet<object> visited)
  {
    if (node is null)
    {
      return;
    }

    switch (node)
    {
      // A structured mana symbol: harvest its colors. Hybrid symbols carry BOTH
      // colors in Colors (CR 903.4 "color or colors"), so they contribute both.
      case ManaSymbol symbol:
        if (symbol.Colors is { Count: > 0 })
        {
          foreach (var c in symbol.Colors)
          {
            colors.Add(ColorCode(c));
          }
        }
        return;

      // "Add [mana]" stores its produced mana as a raw string (rules text, NOT
      // reminder) — parse colored symbols out of it. "Any color" produces no
      // colored mana SYMBOL, so it adds nothing (a colorless identity).
      case AddManaEffect addMana:
        AddManaStringColors(addMana.Mana, colors);
        return;

      case string:
        return; // never scan free text (reminder/flavor/etc.)
    }

    // Recurse into collections.
    if (node is IEnumerable enumerable)
    {
      foreach (var item in enumerable)
      {
        Walk(item, colors, visited);
      }
      return;
    }

    // Recurse into AST nodes (our own types only — avoids walking framework types).
    var type = node.GetType();
    if (type.Namespace is null || !type.Namespace.StartsWith("MagicAST", StringComparison.Ordinal))
    {
      return;
    }

    if (!visited.Add(node))
    {
      return;
    }

    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (prop.GetIndexParameters().Length != 0)
      {
        continue;
      }

      object? value;
      try
      {
        value = prop.GetValue(node);
      }
      catch
      {
        continue;
      }

      Walk(value, colors, visited);
    }
  }

  private static string ColorCode(ManaColor color) =>
    color switch
    {
      ManaColor.White => "W",
      ManaColor.Blue => "U",
      ManaColor.Black => "B",
      ManaColor.Red => "R",
      ManaColor.Green => "G",
      _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown mana color"),
    };

  /// <summary>
  /// Extracts colored mana from an "add [mana]" payload such as "{R}{R}" or
  /// "{G}{U}". Only the five colors count; generic/colorless/snow do not.
  /// </summary>
  private static void AddManaStringColors(string? mana, HashSet<string> colors)
  {
    if (string.IsNullOrEmpty(mana))
    {
      return;
    }

    foreach (var ch in mana)
    {
      switch (char.ToUpperInvariant(ch))
      {
        case 'W':
          colors.Add("W");
          break;
        case 'U':
          colors.Add("U");
          break;
        case 'B':
          colors.Add("B");
          break;
        case 'R':
          colors.Add("R");
          break;
        case 'G':
          colors.Add("G");
          break;
      }
    }
  }
}
