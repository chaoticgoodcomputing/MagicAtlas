namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses "&lt;Subtype&gt; creatures you control have [keyword1][ and
/// keyword2]." (Cloudshredder Sliver: "Sliver creatures you control have
/// flying and haste.") — a static continuous effect (CR 604.2: "Static
/// abilities create continuous effects … These effects are active as long as
/// the permanent with the ability remains on the battlefield.") granting one
/// or two keyword abilities to every creature of the named subtype the
/// controller controls (CR 702.9 Flying / CR 702.10 Haste are both static
/// abilities).
///
/// <para>
/// Sibling to <see cref="ControlledFilterHaveKeywordListRule"/> (which owns
/// "Other permanents you control have …" and "&lt;Subtype&gt; tokens you
/// control have …") and <see cref="CreatureTokensHaveKeywordListRule"/> (which
/// owns the bare, non-subtyped "Creature tokens you control have …" two-
/// keyword shape). Neither sibling's pattern matches the "creatures" noun
/// paired with a capitalised subtype qualifier and no "tokens"/"token"
/// restriction, so this rule is a new, disjoint sibling rather than an edit to
/// either shared rule body — its regex only recognises the literal noun
/// "creature(s)" immediately following the subtype word, which
/// <see cref="ControlledFilterHaveKeywordListRule"/>'s <c>noun</c> group
/// (<c>permanents?|tokens?</c>) never matches.
/// </para>
///
/// <para>
/// The granted subjects are ordinary (non-token) permanents of the named
/// creature subtype, so the built filter is
/// <c>CardTypes=["creature"], Subtypes=[&lt;Subtype&gt;], Controller=You</c> —
/// deliberately omitting <c>IsToken</c> (unlike the sibling "tokens" shapes)
/// since the oracle text says "creatures", not "tokens".
/// </para>
///
/// <para>
/// Keyword list: one or two keywords, optionally joined by "and". Per the
/// MAST multi-effect-per-clause doctrine, a two-keyword grant is bundled into
/// a single <see cref="StaticAbility"/> whose <see cref="Ability.Effects"/>
/// list carries two independent <see cref="GainAbilityEffect"/> nodes —
/// mirroring <see cref="ControlledFilterHaveKeywordListRule"/>'s and
/// <see cref="CreatureTokensHaveKeywordListRule"/>'s two-keyword grant shape.
/// </para>
///
/// <para>
/// Anchored (^…$) pattern; the mandatory literal "creatures" (or "creature")
/// noun and capitalised subtype qualifier keep this rule from matching any
/// sibling clause shape (bare "Creature tokens …", "Other permanents …",
/// "&lt;Subtype&gt; tokens …"). Because the capturing group for the subject
/// qualifier is a bare "any capitalised word" ([A-Z][a-z]+), a corpus sweep of
/// the existing fixtures surfaced three capitalised words that precede
/// "creatures … you control have …" WITHOUT being a creature subtype, each
/// already owned by a more specific sibling rule producing a materially
/// different filter shape — these are explicitly declined below so this rule
/// never shadows them:
/// <list type="bullet">
/// <item>"Other creatures you control have …" (Aggressive Mammoth) — the
/// exclusion-of-self qualifier, owned by <see cref="BareKeywordGrantRule"/>
/// Arm 3 (<c>ExcludeSelf = true</c>, no Subtypes).</item>
/// <item>"Nontoken creatures you control have …" (Rhythm of the Wild) — the
/// token-exclusion qualifier, owned by
/// <see cref="NontokenCreaturesHaveKeywordRule"/> (<c>IsToken = false</c>, no
/// Subtypes).</item>
/// <item>"Attacking creatures you control have …" — the combat-state
/// qualifier, owned by <see cref="AttackingObjectsHaveKeywordRule"/> (a
/// <c>CombatStateCharacteristic</c>, no Subtypes).</item>
/// </list>
/// </para>
/// </summary>
[StaticRule(Priority = 975)]
public sealed class SubtypeCreaturesHaveKeywordListRule : IStaticRule
{
  // "<Subtype> creatures you control have <kw1>[ and <kw2>]."
  private static readonly Regex _pattern = new(
    @"^\s*(?<sub>[A-Z][a-z]+)\s+creatures?\s+you\s+control\s+have\s+" +
    @"(?<kw1>[a-z][a-z]*(?:\s+[a-z]+)*?)(?:\s+and\s+(?<kw2>[a-z][a-z ]+?))?\.?\s*$",
    RegexOptions.Compiled
  );

  // Capitalised words that can precede "creatures you control have …" but are NOT
  // creature subtypes. The subject-capture group is a bare "any capitalised word"
  // ([A-Z][a-z]+), so it would otherwise fabricate a bogus subtype from a color
  // (CR 105.1), supertype (205.4a), a co-card-type qualifier ("Artifact/Land
  // creatures", 205.2b dual types), a permanent-state adjective, or a structural
  // qualifier — none of which are 205.3 creature subtypes. These four categories
  // are a CLOSED vocabulary, so an allow-anything-else guard is sound: any real
  // creature subtype (Sliver, Human, Golem, Dragon, …) is absent from this set and
  // passes through. A corpus sweep of "<Cap> creatures you control have …" over
  // all 31k cards surfaced exactly these non-subtype words (states Equipped/
  // Enchanted/Modified/Tapped were the genuinely-new mis-parses this rule would
  // otherwise introduce, e.g. Dalakos, Crafter of Wonders → bogus Subtypes:["Equipped"]).
  // Where a more specific sibling owns the clause (Other → BareKeywordGrantRule
  // Arm 3; Nontoken → NontokenCreaturesHaveKeywordRule; Attacking →
  // AttackingObjectsHaveKeywordRule) declining here also prevents shadowing.
  private static readonly HashSet<string> _reservedNonSubtypeWords = new(StringComparer.Ordinal)
  {
    // Colors (CR 105.1) + color qualifiers.
    "White", "Blue", "Black", "Red", "Green",
    "Colorless", "Multicolored", "Monocolored",
    // Supertypes (CR 205.4a) + negations.
    "Basic", "Legendary", "Snow", "World", "Nonlegendary",
    // Co-card-type qualifiers ("Artifact/Enchantment/Land creatures", CR 205.2b).
    "Artifact", "Enchantment", "Land",
    "Nonartifact", "Nonland", "Noncreature",
    // Permanent-state / status adjectives.
    "Tapped", "Untapped", "Attacking", "Blocking", "Blocked", "Unblocked",
    "Enchanted", "Equipped", "Modified", "Monstrous", "Renowned",
    // Structural qualifiers (self-exclusion / token partition).
    "Other", "Another", "Nontoken", "Token",
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var subtype = match.Groups["sub"].Value;
    if (_reservedNonSubtypeWords.Contains(subtype))
    {
      // Not a real subtype — decline so the owning sibling rule (see class
      // doc) handles this clause instead.
      return null;
    }

    var filter = new ObjectFilter
    {
      CardTypes = ["creature"],
      Subtypes = [subtype],
      Controller = ControllerFilter.You,
    };

    var kw1 = match.Groups["kw1"].Value.Trim().ToLowerInvariant();
    var granted1 = StaticRuleHelpers.MapKeywordToStaticAbility(kw1);
    if (granted1 is null)
    {
      return null;
    }

    var effects = new List<Effect>
    {
      new GainAbilityEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.Each, Filter = filter },
        GainedAbility = granted1,
      },
    };

    if (match.Groups["kw2"].Success)
    {
      var kw2 = match.Groups["kw2"].Value.Trim().ToLowerInvariant();
      var granted2 = StaticRuleHelpers.MapKeywordToStaticAbility(kw2);
      if (granted2 is null)
      {
        // First keyword resolved but the second didn't — decline entirely so
        // the fallback surfaces the gap rather than emitting a partial grant.
        return null;
      }

      effects.Add(new GainAbilityEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.Each, Filter = filter },
        GainedAbility = granted2,
      });
    }

    return [new StaticAbility { Effects = effects }];
  }
}
