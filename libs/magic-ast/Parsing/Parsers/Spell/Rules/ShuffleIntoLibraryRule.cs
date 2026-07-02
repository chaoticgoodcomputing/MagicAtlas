namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Choose target [type-disjunction]. Its owner shuffles it into their library."
/// Covers the Unravel the Aether pattern — a two-sentence oracle form where the
/// first sentence targets an artifact or enchantment and the second sentence
/// describes the zone-change with shuffle.
///
/// <para>Handled patterns (via <see cref="IMultiSpellRule.TryMatchMulti"/>):</para>
/// <list type="bullet">
///   <item>"Choose target artifact or enchantment. Its owner shuffles it into their library."
///     (Unravel the Aether, BNG)</item>
///   <item>"Choose target artifact or enchantment. Its owner shuffles it into their library."
///     (any reprint of this pattern)</item>
/// </list>
///
/// <para>
/// The two-sentence form is not split by the sentence-bundle path because neither
/// sentence individually matches any rule. This rule implements
/// <see cref="IMultiSpellRule.TryMatchMulti"/> to match the full two-sentence text
/// (with the period separator present) and emits a single
/// <see cref="ShuffleIntoLibraryEffect"/>. The "Choose target" sentence is a targeting
/// sentence that is inseparable from the zone-change, so both sentences resolve into
/// one effect (Rule 701.19, 701.20).
/// </para>
/// </summary>
[SpellRule]
public sealed class ShuffleIntoLibraryRule : ISpellRule, IMultiSpellRule
{
  // Matches the full two-sentence form (period between sentences, no trailing period needed
  // because TryMatchMulti receives the TrimEnd('.') text from the dispatcher).
  // Sentence 1: "Choose target <type1> or <type2>"
  // Sentence 2: "Its owner shuffles it into their library"
  private static readonly Regex TwoSentencePattern = new(
    @"^Choose\s+target\s+(?<type1>[a-z]+)\s+or\s+(?<type2>[a-z]+)\.\s+Its\s+owner\s+shuffles\s+it\s+into\s+their\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches a single-sentence form for future extensibility:
  // "Shuffle target <type> into its owner's library."
  private static readonly Regex SinglePattern = new(
    @"^(?:Its\s+owner\s+shuffles|Shuffle)\s+target\s+(?<type>[a-z]+(?:\s+or\s+[a-z]+)?)\s+into\s+(?:their|its\s+owner'?s)\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly HashSet<string> KnownTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "artifact",
    "enchantment",
    "creature",
    "land",
    "permanent",
    "planeswalker",
  };

  /// <inheritdoc cref="ISpellRule.TryMatch"/>
  /// <remarks>
  /// Handles the single-sentence form "Shuffle target [type] into its owner's library."
  /// Returns false for the two-sentence form; that path is handled by
  /// <see cref="TryMatchMulti"/>.
  /// </remarks>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = SinglePattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var typePhrase = m.Groups["type"].Value.ToLowerInvariant();
    var cardTypes = ParseTypePhrase(typePhrase);
    if (cardTypes is null)
    {
      return false;
    }

    effect = new ShuffleIntoLibraryEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = cardTypes },
      },
    };
    return true;
  }

  /// <inheritdoc cref="IMultiSpellRule.TryMatchMulti"/>
  /// <remarks>
  /// Matches "Choose target [type1] or [type2]. Its owner shuffles it into their library"
  /// and emits a single <see cref="ShuffleIntoLibraryEffect"/>. The "Choose target" verb
  /// is the targeting mechanism; both sentences are descriptors of one zone-change action.
  /// </remarks>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var m = TwoSentencePattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var t1 = m.Groups["type1"].Value.ToLowerInvariant();
    var t2 = m.Groups["type2"].Value.ToLowerInvariant();

    if (!KnownTypes.Contains(t1) || !KnownTypes.Contains(t2))
    {
      return false;
    }

    effects = new List<Effect>
    {
      new ShuffleIntoLibraryEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = [t1, t2] },
        },
      },
    };
    return true;
  }

  /// <summary>
  /// Parses a type phrase like "artifact or enchantment" into a card-type list.
  /// Returns null if any token is not a recognised card type.
  /// </summary>
  private static IReadOnlyList<string>? ParseTypePhrase(string typePhrase)
  {
    if (!typePhrase.Contains(" or "))
    {
      var single = typePhrase.Trim();
      return KnownTypes.Contains(single) ? [single] : null;
    }

    var parts = typePhrase.Split(" or ", StringSplitOptions.RemoveEmptyEntries);
    var result = new List<string>(parts.Length);
    foreach (var part in parts)
    {
      var t = part.Trim();
      if (!KnownTypes.Contains(t))
      {
        return null;
      }
      result.Add(t);
    }
    return result;
  }
}
