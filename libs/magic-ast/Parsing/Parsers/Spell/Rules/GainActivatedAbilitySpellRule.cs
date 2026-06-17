namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Recognises the "Until end of turn, target creature gains &lt;quoted activated ability&gt;."
/// shape — i.e. an instant/sorcery that grants a target creature a full activated ability
/// (cost + effect, quoted in oracle text) for the rest of the turn.
///
/// Example: Retraction Helix (BNG) — "Until end of turn, target creature gains
/// &quot;{T}: Return target nonland permanent to its owner's hand.&quot;"
///
/// The granted ability is modelled as a nested <see cref="ActivatedAbility"/> inside a
/// <see cref="GainAbilityEffect"/>; the duration is <c>untilEndOfTurn</c>. CR 613.1c
/// (Layer 6 — ability-granting effects); CR 611 (continuous effects with duration);
/// CR 113.6 — abilities granted by effects are still full-fledged abilities of the
/// gaining permanent.
///
/// Priority 85: must fire before the bare keyword-grant rules (TargetCreatureGainsKeywordRule,
/// priority 50) since this rule is more specific (the quoted text inside the gains clause
/// is a full activated ability, not a keyword).
/// </summary>
[SpellRule(Priority = 85)]
public sealed class GainActivatedAbilitySpellRule : ISpellRule
{
  // Matches: "Until end of turn, target creature gains "<cost>: <effect>."
  // Captures the entire quoted activated ability string (everything between the
  // opening and closing double-quote, excluding the surrounding quote marks).
  // The quoted text is surrounded by literal Unicode left/right double quotes (“/”)
  // or standard ASCII double quotes ("); both are accepted because Scryfall uses curly quotes
  // but test fixtures and some printings may use straight quotes.
  private static readonly Regex _pattern = new(
    @"^Until\s+end\s+of\s+turn,\s+target\s+creature\s+gains\s+[“""](?<quoted>.+)[”""]\.*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the cost part before the colon in the quoted activated ability.
  // Supports one or more comma-separated mana/tap/untap symbols.
  // E.g. "{T}", "{2}", "{1}{U}", "{T}, {1}{U}"
  private static readonly Regex _tapOnlyCostPattern = new(
    @"^\{T\}$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly ManaCostParser _manaCostParser = new();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      // Also try the untrimmed text in case the trailing period is still present.
      m = _pattern.Match(text.Trim());
      if (!m.Success)
      {
        return false;
      }
    }

    var quotedAbility = m.Groups["quoted"].Value.Trim().TrimEnd('.');
    // Split on the first colon to get cost and effect parts of the granted ability.
    var colonIndex = quotedAbility.IndexOf(':');
    if (colonIndex < 0)
    {
      return false;
    }

    var costPart = quotedAbility[..colonIndex].Trim();
    var effectPart = quotedAbility[(colonIndex + 1)..].Trim().TrimEnd('.');

    // Parse the cost component(s) of the granted activated ability.
    var costs = ParseGrantedAbilityCosts(costPart);
    if (costs is null)
    {
      return false;
    }

    // Parse the effect component of the granted activated ability.
    var grantedEffects = ParseGrantedAbilityEffects(effectPart);
    if (grantedEffects is null || grantedEffects.Count == 0)
    {
      return false;
    }

    var grantedActivatedAbility = new ActivatedAbility
    {
      Costs = costs,
      Effects = grantedEffects,
      IsManaAbility = false,
    };

    effect = new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      GainedAbility = grantedActivatedAbility,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  /// <summary>
  /// Parses the cost portion of the quoted activated ability string.
  /// Currently handles: "{T}" (tap cost), simple mana costs like "{1}", "{U}", "{2}{U}".
  /// Returns null if the cost text cannot be parsed.
  /// </summary>
  private static IReadOnlyList<Cost>? ParseGrantedAbilityCosts(string costText)
  {
    var costs = new List<Cost>();

    // Handle comma-separated cost components (e.g., "{2}, {T}").
    var components = costText.Split(',').Select(c => c.Trim()).ToList();
    foreach (var component in components)
    {
      var cost = ParseSingleCostComponent(component);
      if (cost is null)
      {
        return null;
      }
      costs.Add(cost);
    }

    return costs.Count > 0 ? costs : null;
  }

  private static Cost? ParseSingleCostComponent(string component)
  {
    if (_tapOnlyCostPattern.IsMatch(component))
    {
      return new TapCost();
    }

    // Try parsing as a mana cost: one or more {symbol} tokens.
    if (!component.StartsWith('{') || !component.EndsWith('}'))
    {
      return null;
    }

    // Parse mana symbols from the cost text (e.g., "{U}", "{2}{U}", "{1}{G}{G}").
    try
    {
      var parsed = _manaCostParser.Parse(component);
      if (parsed.Symbols.Count > 0)
      {
        return new ManaCost { Symbols = parsed.Symbols };
      }
    }
    catch
    {
      // Parsing failed — return null to signal inability to handle this cost.
    }

    return null;
  }

  /// <summary>
  /// Parses the effect text of the quoted activated ability string.
  /// Handles the "Return target [nonX] [type] to its owner's hand" shape for now,
  /// which covers Retraction Helix's "Return target nonland permanent to its owner's hand."
  /// </summary>
  private static IReadOnlyList<Effect>? ParseGrantedAbilityEffects(string effectText)
  {
    // Use the existing ReturnTargetToHandRule to parse the effect text.
    // This leverages the structured nonland/type filter logic already built there.
    var returnToHandRule = new ReturnTargetToHandRule();
    if (returnToHandRule.TryMatch(effectText.TrimEnd('.'), out var returnEffect) && returnEffect is not null)
    {
      return new List<Effect> { returnEffect };
    }

    return null;
  }
}
