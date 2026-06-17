namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Return up to one target [type1], [type2], or [type3] to its owner's hand." —
/// the "up to one target" bounce with a card-type disjunction (e.g. "artifact,
/// creature, or enchantment"). Models the multi-type target as a single
/// <see cref="ReturnToHandEffect"/> whose <c>Target.Filter.CardTypes</c> is the
/// disjunction of all named types — the filter is satisfied when the object has
/// ANY of the listed types (OR semantics, CR 115.3).
///
/// <para>
/// CR 402: returning an object to its owner's hand is a zone change from any zone
/// to the hand. No dedicated keyword action; the text is stated directly.
/// </para>
///
/// <para>
/// "Up to one" is modelled as <c>Quantity = UpToQuantity { Maximum = 1 }</c> on
/// the <see cref="ObjectReference"/> — parallel to "up to two target creatures"
/// (Soul Salvage) — rather than an <c>OptionalEffect</c> wrapper, because the
/// constraint is on the NUMBER of targets chosen (0–1), not on whether a one-shot
/// action is performed at all (CR 117.3a: "up to X" targets means 0 to X are
/// chosen at time of targeting).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class ReturnUpToOneTargetTypeDisjunctionToHandEffectRule : IActivatedEffectRule
{
  // Matches: "Return up to one target <type>[, <type>]* [or <type>] to its owner's hand"
  // The types group captures a comma/or-separated list of card types.
  private static readonly Regex _pattern = new(
    @"^Return\s+up\s+to\s+one\s+target\s+(?<types>[\w]+(?:,\s*[\w]+)*(?:,?\s+or\s+[\w]+)?)\s+to\s+its?\s+owner'?s\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "artifact", "creature", "enchantment", "land", "permanent", "planeswalker",
    "instant", "sorcery", "battle",
  };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var typesRaw = m.Groups["types"].Value;

    // Split on ", " and " or " to get individual type words.
    var parts = Regex.Split(typesRaw, @",?\s+or\s+|,\s*")
      .Select(t => t.Trim().ToLowerInvariant())
      .Where(t => t.Length > 0)
      .ToList();

    // Validate all parts are known card types.
    if (parts.Count == 0 || !parts.All(p => _knownCardTypes.Contains(p)))
    {
      return null;
    }

    return new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Quantity = new UpToQuantity { Maximum = 1, Minimum = 0 },
        Filter = new ObjectFilter
        {
          CardTypes = parts,
        },
      },
    };
  }
}
