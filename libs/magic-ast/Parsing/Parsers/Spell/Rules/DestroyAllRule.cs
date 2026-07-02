namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy all [filter]." — mass-destroy spell, with optional qualifier extensions.
///
/// <para>Handled patterns (via <see cref="ISpellRule.TryMatch"/>):</para>
/// <list type="bullet">
///   <item>Single-token bare type: "Destroy all creatures."</item>
///   <item>Single-token subtype: "Destroy all Plains."</item>
///   <item>Conjunction of two card types: "Destroy all artifacts and enchantments."</item>
///   <item>non- prefix + type: "Destroy all nonbasic lands.", "Destroy all nonland permanents."</item>
///   <item>Color + type: "Destroy all white creatures."</item>
/// </list>
///
/// <para>Handled patterns (via <see cref="IMultiSpellRule.TryMatchMulti"/>):</para>
/// <list type="bullet">
///   <item>"Destroy all [filter]. They can't be regenerated." — emits a single
///     <see cref="DestroyEffect"/> with <see cref="DestroyEffect.CantBeRegenerated"/> set.
///     The two-sentence form is not broken apart; regeneration is a modifier on the
///     destruction event, not a sibling effect. (Wrath of God pattern.)</item>
/// </list>
///
/// <para>
/// Emits a <see cref="DestroyEffect"/> whose <see cref="DestroyEffect.Target"/> has
/// <see cref="ObjectReferenceKind.Each"/> and the appropriate <see cref="ObjectFilter"/>
/// slot populated.
/// </para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class DestroyAllRule : ISpellRule, IMultiSpellRule
{
  /// <summary>
  /// Card types that appear as plural oracle tokens for "Destroy all &lt;type&gt;".
  /// These map directly to the singular lowercase value used in <see cref="ObjectFilter.CardTypes"/>.
  /// </summary>
  private static readonly HashSet<string> CardTypeTokens = new(StringComparer.OrdinalIgnoreCase)
  {
    "lands",
    "creatures",
    "artifacts",
    "enchantments",
    "planeswalkers",
    "permanents",
    "instants",
    "sorceries",
  };

  /// <summary>
  /// Lookup from plural oracle-text subtype tokens to their canonical singular form.
  /// Only subtypes that appear as "Destroy all &lt;subtype&gt;" targets need entries here.
  /// Plain-singular forms (e.g., "Plains") map to themselves; irregular plurals are explicit.
  /// </summary>
  private static readonly Dictionary<string, string> SubtypeSingular =
    new(StringComparer.OrdinalIgnoreCase)
    {
      // Basic land types — canonical singular
      { "Plains", "Plains" },
      { "Islands", "Island" },
      { "Swamps", "Swamp" },
      { "Mountains", "Mountain" },
      { "Forests", "Forest" },
      // Enchantment subtype
      { "Auras", "Aura" },
    };

  // Matches "Destroy all <filter>" where <filter> is one or more words
  // (captures multi-word phrases like "nonbasic lands", "white creatures").
  private static readonly Regex Pattern = new(
    @"^Destroy\s+all\s+(?<filter>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Matches "Destroy all <filter>. They can't be regenerated"
  // (trailing period stripped by the dispatcher before TryMatchMulti is called).
  private static readonly Regex CantBeRegeneratedPattern = new(
    @"^Destroy\s+all\s+(?<filter>.+?)\.\s+They\s+can't\s+be\s+regenerated$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Matches a type-conjunction filter: "<type1> and <type2>"
  // Only card types — subtypes are not used in "and" conjunction destroy-all oracle text.
  private static readonly Regex ConjunctionPattern = new(
    @"^(?<t1>[A-Za-z]+)\s+and\s+(?<t2>[A-Za-z]+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc cref="ISpellRule.TryMatch"/>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();

    // Fast path: single-token cases (existing behaviour for Armageddon, Supreme Verdict, etc.)
    if (!filterPhrase.Contains(' '))
    {
      var token = filterPhrase;

      if (CardTypeTokens.Contains(token))
      {
        var singularType = token.TrimEnd('s').ToLowerInvariant();
        effect = new DestroyEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter { CardTypes = [singularType] },
          },
        };
        return true;
      }

      if (SubtypeSingular.TryGetValue(token, out var singular))
      {
        effect = new DestroyEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter { Subtypes = [singular] },
          },
        };
        return true;
      }

      // Unknown single token — fall through to the shared helper
      // (handles cases the fast-path tables don't cover).
    }

    // Conjunction path: "artifacts and enchantments", "creatures and planeswalkers", etc.
    // Must be checked before ParseTargetFilter (which only handles single-noun phrases).
    var conjM = ConjunctionPattern.Match(filterPhrase);
    if (conjM.Success)
    {
      var t1 = conjM.Groups["t1"].Value;
      var t2 = conjM.Groups["t2"].Value;
      if (CardTypeTokens.Contains(t1) && CardTypeTokens.Contains(t2))
      {
        var singular1 = t1.TrimEnd('s').ToLowerInvariant();
        var singular2 = t2.TrimEnd('s').ToLowerInvariant();
        effect = new DestroyEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter { CardTypes = [singular1, singular2] },
          },
        };
        return true;
      }
    }

    // Multi-word (or unlisted single-token) path: delegate to the shared helper.
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = filter,
      },
    };
    return true;
  }

  /// <inheritdoc cref="IMultiSpellRule.TryMatchMulti"/>
  /// <remarks>
  /// Handles "Destroy all [filter]. They can't be regenerated" — the sentence-bundle
  /// dispatcher splits on ". " before each capital letter, so a naïve bundle parse would
  /// leave "They can't be regenerated" without a matching rule. By matching the whole
  /// two-sentence form here (on the un-split text, called after bundle failure),
  /// we collapse it into a single <see cref="DestroyEffect"/> with
  /// <see cref="DestroyEffect.CantBeRegenerated"/> set rather than producing a sibling
  /// effect — which is correct because regeneration prevention is a modifier on the
  /// destruction event, not an independent effect (Rule 701.7).
  /// </remarks>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = CantBeRegeneratedPattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var filter = BuildFilter(filterPhrase);
    if (filter is null)
    {
      return false;
    }

    effects = new List<Effect>
    {
      new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = filter,
        },
        CantBeRegenerated = true,
      },
    };
    return true;
  }

  /// <summary>
  /// Resolves a filter phrase (the words after "Destroy all") to an
  /// <see cref="ObjectFilter"/>, using the same fast-path tables and
  /// <see cref="SpellRuleHelpers.ParseTargetFilter"/> fallback that
  /// <see cref="TryMatch"/> uses.
  /// </summary>
  private static ObjectFilter? BuildFilter(string filterPhrase)
  {
    if (!filterPhrase.Contains(' '))
    {
      if (CardTypeTokens.Contains(filterPhrase))
      {
        var singularType = filterPhrase.TrimEnd('s').ToLowerInvariant();
        return new ObjectFilter { CardTypes = [singularType] };
      }
      if (SubtypeSingular.TryGetValue(filterPhrase, out var singular))
      {
        return new ObjectFilter { Subtypes = [singular] };
      }
    }

    var conjM = ConjunctionPattern.Match(filterPhrase);
    if (conjM.Success)
    {
      var t1 = conjM.Groups["t1"].Value;
      var t2 = conjM.Groups["t2"].Value;
      if (CardTypeTokens.Contains(t1) && CardTypeTokens.Contains(t2))
      {
        var singular1 = t1.TrimEnd('s').ToLowerInvariant();
        var singular2 = t2.TrimEnd('s').ToLowerInvariant();
        return new ObjectFilter { CardTypes = [singular1, singular2] };
      }
    }

    return SpellRuleHelpers.ParseTargetFilter(filterPhrase);
  }
}
