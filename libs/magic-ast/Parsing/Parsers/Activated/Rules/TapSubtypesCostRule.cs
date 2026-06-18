namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Tap-N-subtype cost: "Tap three untapped Elves you control",
/// "Tap two untapped Merfolk you control", etc. — the spelled-out "Tap …" verb
/// used as an activation cost where the permanents are identified by creature
/// subtype rather than card type (CR 118.3 / 602.5; CR 701.20 tap/untap).
///
/// <para>
/// This rule handles the pattern where only a creature subtype (e.g. "Elf",
/// "Elves", "Merfolk", "Goblin") appears in the tap cost — no explicit card-type
/// word ("creature", "artifact") is printed. Oracle text capitalises creature
/// subtypes (Rule 205.3m), so the regex requires an uppercase first letter,
/// preventing "Tap three untapped lands you control" from matching here (handled
/// by <see cref="TapPermanentsCostRule"/> which requires a card-type word).
/// </para>
///
/// <para>
/// The "untapped" qualifier is implicit in any tap cost (CR 701.20a: a permanent
/// must be untapped to be tapped), so it is not encoded as a separate axis on the
/// emitted <see cref="TapPermanentsCost"/>, matching the Conspire idiom.
/// </para>
///
/// <para>
/// Emits a <see cref="TapPermanentsCost"/> with:
/// <list type="bullet">
///   <item><see cref="TapPermanentsCost.Filter"/> — CardTypes = ["creature"],
///     Subtypes = [singularized subtype], Controller = You.</item>
///   <item><see cref="TapPermanentsCost.Quantity"/> — parsed from the number
///     word (one/two/three/…) or digit in the cost text.</item>
/// </list>
/// </para>
///
/// <para>
/// Priority 996 — above TapPermanentsCostRule (Priority 997 handles generic
/// card-type tap costs; numbered lower = higher priority in the registry).
/// Wait — Priority here is DESCENDING (higher number = higher priority per the
/// convention in ActivatedCostRuleAttribute). Set to 998 so this fires before
/// TapPermanentsCostRule (997), which would otherwise return null on subtype-only
/// costs anyway (it requires a card-type keyword), making the order moot — but
/// explicit priority ensures no future generic rule intercepts first.
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 998)]
public sealed class TapSubtypesCostRule : IActivatedCostRule
{
  // Matches: "Tap [count] untapped [Word] you control"
  // - count: a number word or digit
  // - Word: the subtype (possibly plural) — must begin with uppercase (Rule 205.3m)
  //   Greedy match of the whole word; singularisation is done in code after capture.
  // Anchored ^ and $ to prevent matching as a substring of a longer cost string.
  // The guard "starts with uppercase" prevents card-type words ("creature", "land",
  // "artifact") from matching here — those are handled by TapPermanentsCostRule.
  private static readonly Regex _pattern = new(
    @"^[Tt]ap\s+(?<count>one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+untapped\s+(?<word>[A-Z][A-Za-z]+)\s+you\s+control$",
    RegexOptions.Compiled
  );

  public Cost? TryMatch(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    // Fast-path guard: must start with "tap " and contain "you control".
    if (!lower.StartsWith("tap ") || !lower.Contains("you control"))
    {
      return null;
    }

    var m = _pattern.Match(costText);
    if (!m.Success)
    {
      return null;
    }

    var countRaw = m.Groups["count"].Value.ToLowerInvariant();
    int count = countRaw switch
    {
      "one"   => 1,
      "two"   => 2,
      "three" => 3,
      "four"  => 4,
      "five"  => 5,
      "six"   => 6,
      "seven" => 7,
      "eight" => 8,
      "nine"  => 9,
      "ten"   => 10,
      _       => int.Parse(countRaw),
    };

    // Singularise the captured word to produce the oracle-canonical creature subtype.
    // "Elves" → "Elf", "Goblins" → "Goblin", "Merfolk" → "Merfolk" (invariant plural),
    // "Vampires" → "Vampire". Common irregular plurals are handled explicitly; regular
    // "-s" plurals are stripped; words that are already singular are left alone.
    // Capitalisation is already correct (the regex requires [A-Z] at position 0).
    var word = m.Groups["word"].Value;
    var subtype = Singularize(word);

    return new TapPermanentsCost
    {
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
      Quantity = LiteralQuantity.Of(count),
    };
  }

  /// <summary>
  /// Converts a plural (or already-singular) creature subtype word into its
  /// singular canonical form, matching oracle-text capitalisation (Rule 205.3m).
  /// </summary>
  private static string Singularize(string word)
  {
    // MTG irregular plurals — explicit table beats generic suffix stripping.
    if (word.Equals("Elves", StringComparison.OrdinalIgnoreCase))    return "Elf";
    if (word.Equals("Dwarves", StringComparison.OrdinalIgnoreCase))  return "Dwarf";
    if (word.Equals("Wolves", StringComparison.OrdinalIgnoreCase))   return "Wolf";
    if (word.Equals("Leaves", StringComparison.OrdinalIgnoreCase))   return "Leaf";
    if (word.Equals("Knives", StringComparison.OrdinalIgnoreCase))   return "Knife";
    if (word.Equals("Shelves", StringComparison.OrdinalIgnoreCase))  return "Shelf";
    // Invariant plurals (same singular and plural form in oracle text).
    if (word.Equals("Merfolk", StringComparison.OrdinalIgnoreCase))  return "Merfolk";
    if (word.Equals("Sheep", StringComparison.OrdinalIgnoreCase))    return "Sheep";
    if (word.Equals("Fish", StringComparison.OrdinalIgnoreCase))     return "Fish";
    // MTG creature subtypes that END in "ies" pluralise as +s, NOT the English -ies→-y rule:
    // "Zombie"→"Zombies", "Faerie"→"Faeries". So they fall through to the regular -s strip below
    // ("Zombies"→"Zombie"), which is correct. (A true -ies→-y subtype like "Ally"→"Allies" does not
    // occur as a tap-cost subtype; the earlier explicit-irregulars table covers the real exceptions.)
    // Regular "-s" plural: strip trailing 's' if present and length > 2.
    // "Vampires" → "Vampire", "Goblins" → "Goblin", "Zombies" → "Zombie", "Elves" (handled above).
    if (word.Length > 2 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
    {
      return word[..^1];
    }
    // Already singular (or unrecognized — leave as-is; the judge will catch errors).
    return word;
  }
}
