namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Mass-exile spell rules. Two oracle shapes are handled:
///
/// <list type="bullet">
///   <item>"Exile all creatures." — removes every creature on the battlefield
///     (the default game zone). <see cref="ObjectFilter.Zone"/> is left null
///     (battlefield-scope is the implicit default for permanent references).
///     Rule 701.13a (exile action) + Rule 400.1 (zones).</item>
///   <item>"Exile all [type] cards from all graveyards." — graveyard-scope
///     mass exile. Sets <see cref="ObjectFilter.Zone"/> to
///     <see cref="Zone.Graveyard"/>. Covers Scavenging Ooze cousins and
///     clean-the-graveyard spells. Rule 701.13a + Rule 400.2g (graveyard
///     zone).</item>
/// </list>
///
/// <para>
/// Both shapes emit an <see cref="ExileEffect"/> whose <see cref="ExileEffect.Target"/>
/// carries <see cref="ObjectReferenceKind.Each"/> and the appropriate
/// <see cref="ObjectFilter"/> slot populated.
/// </para>
///
/// <para>
/// Priority 60 — same band as <see cref="DestroyAllRule"/>. The leading
/// "Exile all" anchor is mutually exclusive with all "Exile target" rules
/// (priority 50–70), so no shadowing risk.
/// </para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class ExileAllRule : ISpellRule
{
  /// <summary>
  /// Card-type oracle tokens accepted in an "Exile all &lt;type&gt;" filter phrase.
  /// Keys cover both singular and plural forms; values are canonical lowercase singular.
  /// </summary>
  private static readonly Dictionary<string, string> CardTypeMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
      { "land", "land" },
      { "lands", "land" },
      { "creature", "creature" },
      { "creatures", "creature" },
      { "artifact", "artifact" },
      { "artifacts", "artifact" },
      { "enchantment", "enchantment" },
      { "enchantments", "enchantment" },
      { "planeswalker", "planeswalker" },
      { "planeswalkers", "planeswalker" },
      { "permanent", "permanent" },
      { "permanents", "permanent" },
      { "instant", "instant" },
      { "instants", "instant" },
      { "sorcery", "sorcery" },
      { "sorceries", "sorcery" },
    };

  // "Exile all <filter>" — battlefield scope (zone omitted).
  // e.g. "Exile all creatures", "Exile all artifacts"
  private static readonly Regex BattlefieldPattern = new(
    @"^Exile\s+all\s+(?<filter>[A-Za-z ]+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // "Exile all <type> cards from all graveyards"
  // e.g. "Exile all creature cards from all graveyards"
  private static readonly Regex GraveyardPattern = new(
    @"^Exile\s+all\s+(?<type>[A-Za-z]+)\s+cards?\s+from\s+all\s+graveyards$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // Graveyard-scope match takes priority — checked first so the more
    // constrained pattern wins before the broader BattlefieldPattern claims it.
    var graveyardMatch = GraveyardPattern.Match(text);
    if (graveyardMatch.Success)
    {
      var typeToken = graveyardMatch.Groups["type"].Value;
      if (!CardTypeMap.TryGetValue(typeToken, out var singularType))
      {
        return false;
      }

      effect = new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter
          {
            CardTypes = [singularType],
            Zone = Zone.Graveyard,
          },
        },
      };
      return true;
    }

    // Battlefield-scope match: "Exile all [filter]"
    var battlefieldMatch = BattlefieldPattern.Match(text);
    if (!battlefieldMatch.Success)
    {
      return false;
    }

    var filterPhrase = battlefieldMatch.Groups["filter"].Value.Trim();

    // Delegate to the shared filter helper for multi-word phrases
    // (e.g. "white creatures", "nonbasic lands").
    if (filterPhrase.Contains(' '))
    {
      var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
      if (filter is null)
      {
        return false;
      }

      effect = new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = filter,
        },
      };
      return true;
    }

    // Single token: must be a known card type.
    if (!CardTypeMap.TryGetValue(filterPhrase, out var singularCardType))
    {
      return false;
    }

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = [singularCardType] },
      },
    };
    return true;
  }
}
