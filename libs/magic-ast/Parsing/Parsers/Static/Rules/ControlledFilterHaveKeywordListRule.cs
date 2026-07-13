namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses "Other permanents you control have [keyword]." and "&lt;Subtype&gt;
/// tokens you control have [keyword1] and [keyword2]." — static continuous
/// effects (CR 604.2: "Static abilities create continuous effects … These
/// effects are active as long as the permanent with the ability remains on
/// the battlefield.") granting one or two keyword abilities to a filtered
/// subset of the permanents the controller controls.
///
/// <para>
/// CR 702.11a (verbatim): "Hexproof is a static ability." Menace is likewise a
/// static evasion ability, granted continuously by the source's static
/// ability for as long as it remains on the battlefield.
/// </para>
///
/// <para>
/// Two sibling subject shapes recognised by one pattern:
/// <list type="bullet">
/// <item>"Other permanents you control have …" (Sigarda, Font of Blessings) —
/// the "Other" qualifier maps to <c>ExcludeSelf = true</c> (CR 109.5 —
/// "another"), the noun "permanents" maps to <c>CardTypes = ["permanent"]</c>.
/// </item>
/// <item>"&lt;Subtype&gt; tokens you control have …" (Gleaming Overseer) — the
/// capitalised subtype word populates <c>Subtypes</c>, the noun "tokens" maps
/// to <c>IsToken = true</c> (CR 111.1: "A token is a marker used to represent
/// any permanent that isn't represented by a card.").
/// </item>
/// </list>
/// Only these two combinations are built — any other combination captured by
/// the pattern (e.g. a subtype paired with "permanents", or "Other" paired
/// with "tokens") falls through to null so it doesn't shadow a sibling rule
/// that might legitimately own a different shape.
/// </para>
///
/// <para>
/// Keyword list: one or two keywords, optionally joined by "and" ("hexproof
/// and menace" — Gleaming Overseer). Per the MAST multi-effect-per-clause
/// doctrine, a two-keyword grant is bundled into a single
/// <see cref="StaticAbility"/> whose <see cref="Ability.Effects"/> list
/// carries two independent <see cref="GainAbilityEffect"/> nodes — mirroring
/// <see cref="BareKeywordPairGrantRule"/>'s enchant/equip dual-keyword shape,
/// generalised here to a filtered-subject target instead of the
/// enchanted/equipped anchor.
/// </para>
///
/// <para>
/// Priority 973 — above <see cref="AttackingObjectsHaveKeywordRule"/> (972),
/// <see cref="SubtypeTokensHaveKeywordRule"/> (970), and
/// <see cref="BareKeywordGrantRule"/> (967). Anchored (^…$) pattern prevents
/// substring matches against sibling clauses; the bounded filter-shape guard
/// above prevents this rule from mis-owning shapes already handled by those
/// sibling rules (which return null on the shapes this rule declines, so
/// dispatch order does not change behaviour either way).
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class ControlledFilterHaveKeywordListRule : IStaticRule
{
  // "Other permanents you control have <kw1>[ and <kw2>]." OR
  // "<Subtype> tokens you control have <kw1>[ and <kw2>]."
  // <other>/<sub> are the two mutually-exclusive subject qualifiers; <noun> is
  // the plural card-type/token word. kw1 greedily matches lowercase words and
  // backtracks (via the lazy trailing group) to let the optional " and <kw2>"
  // tail bind — mirroring BareKeywordPairGrantRule's kw1 pattern.
  private static readonly Regex _pattern = new(
    @"^\s*(?<other>Other\s+)?(?:(?<sub>[A-Z][a-z]+)\s+)?(?<noun>permanents?|tokens?)\s+you\s+control\s+have\s+" +
    @"(?<kw1>[a-z][a-z]*(?:\s+[a-z]+)*?)(?:\s+and\s+(?<kw2>[a-z][a-z ]+?))?\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var isOther = match.Groups["other"].Success;
    var subtype = match.Groups["sub"].Success ? match.Groups["sub"].Value : null;
    var noun = match.Groups["noun"].Value.ToLowerInvariant();
    var isTokenNoun = noun.StartsWith("token", StringComparison.Ordinal);
    var isPermanentNoun = noun.StartsWith("permanent", StringComparison.Ordinal);

    ObjectFilter filter;
    if (isOther && subtype is null && isPermanentNoun)
    {
      // "Other permanents you control have …" — Sigarda, Font of Blessings.
      filter = new ObjectFilter
      {
        CardTypes = ["permanent"],
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      };
    }
    else if (!isOther && subtype is not null && isTokenNoun &&
             !subtype.Equals("Creature", StringComparison.OrdinalIgnoreCase) &&
             !subtype.Equals("Creatures", StringComparison.OrdinalIgnoreCase) &&
             !subtype.Equals("Attacking", StringComparison.Ordinal))
    {
      // "<Subtype> tokens you control have …" — Gleaming Overseer.
      // GUARD: "Creature"/"Creatures" is the bare card-type token grant (e.g.
      // Combine Chrysalis "Creature tokens you control have flying.") — owned
      // by BareKeywordGrantRule's bare card-type branch, not a subtype filter.
      // "Attacking" is the combat-state qualifier (e.g. Starry-Eyed Skyrider
      // "Attacking tokens you control have flying.") — owned by
      // AttackingObjectsHaveKeywordRule via CombatStateCharacteristic, not a
      // subtype. Declining here lets those sibling rules own their shapes.
      filter = new ObjectFilter
      {
        Subtypes = [subtype],
        IsToken = true,
        Controller = ControllerFilter.You,
      };
    }
    else
    {
      // Not one of the two recognised subject shapes — decline so sibling
      // rules (BareKeywordGrantRule, SubtypeTokensHaveKeywordRule, etc.) own it.
      return null;
    }

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
