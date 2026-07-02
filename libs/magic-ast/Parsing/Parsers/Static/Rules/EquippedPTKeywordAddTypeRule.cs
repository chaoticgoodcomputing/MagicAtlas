namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the compound static Equipment line:
/// "Equipped creature gets +N/+N, has [keyword], and is a [Subtype] in addition to its
/// other types."
///
/// <para>
/// Raven Wings is the canonical example:
/// "Equipped creature gets +1/+0, has flying, and is a Bird in addition to its other
/// types."
/// This is a three-part continuous ability:
/// <list type="bullet">
///   <item>P/T modification (layer 7c — CR 613.4c)</item>
///   <item>Keyword ability grant (layer 6 — CR 613.1f)</item>
///   <item>Additive subtype gain (layer 4 — CR 613.1d)</item>
/// </list>
/// All three effects target the equipped creature (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>)
/// and persist for as long as the Equipment is attached (no Duration — always-on static).
/// </para>
///
/// <para>
/// Unlike <see cref="EquippedPTKeywordColorSubtypeRule"/> (Nim Deathmantle: "...and is a
/// black Zombie."), which REPLACES the color and subtype via <see cref="ChangeColorEffect"/>
/// and <see cref="ChangeSubtypeEffect"/>, this clause has no color and is ADDITIVE ("in
/// addition to its other types"). Per CR 205.1b, "in addition to its other types" means all
/// prior card types, supertypes, and subtypes are retained — so the subtype gain is modeled
/// with <see cref="AddTypeEffect"/>, not a replacing type-change effect.
/// </para>
///
/// <para>
/// CR 205.1b (verbatim): "Some effects change an object's card type, supertype, or subtype
/// but specify that the object retains a prior card type, supertype, or subtype. In such
/// cases, all the object's prior card types, supertypes, and subtypes are retained. This
/// rule applies to effects that use phrases such as "in addition to its other types" or
/// that state that something is "still a [type, supertype, or subtype]."
/// </para>
/// <para>
/// CR 613.1d (verbatim): "Layer 4: Type-changing effects are applied. These include effects
/// that change an object's card type, subtype, and/or supertype."
/// </para>
/// <para>
/// CR 613.1f (verbatim): "Layer 6: Ability-adding effects, keyword counters, ability-removing
/// effects, and effects that say an object can't have an ability are applied."
/// </para>
/// <para>
/// CR 613.4c (verbatim): "Layer 7c: Effects and counters that modify power and/or toughness
/// (but don't set power and/or toughness to a specific number or value) are applied."
/// </para>
///
/// <para>
/// Rule 702.6 (Equipment): an Equipment's static abilities apply to the equipped creature.
/// </para>
///
/// <para>
/// Reminder text is stripped by <see cref="StaticRuleHelpers.StripReminderText"/> before
/// matching, following the convention of sibling Equipment static rules such as
/// <see cref="EquippedPTKeywordColorSubtypeRule"/>.
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class EquippedPTKeywordAddTypeRule : IStaticRule
{
  // "Equipped creature gets +N/+N, has <keyword>, and is a <Subtype> in addition to its
  // other types."
  // The keyword capture is a bare lowercase word or two-word phrase (e.g. "first strike").
  // The subtype is a single proper-noun word (capitalised creature subtype like "Bird").
  private static readonly Regex _pattern = new(
    @"^\s*Equipped\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+),\s+has\s+(?<kw>[a-z][a-z ]*?),\s+and\s+is\s+a\s+(?<subtype>[A-Z][a-zA-Z]+)\s+in\s+addition\s+to\s+its\s+other\s+types\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var kw = match.Groups["kw"].Value.Trim().ToLowerInvariant();
    var subtype = match.Groups["subtype"].Value;

    // Ensure the subtype starts with uppercase per oracle-text convention for creature subtypes.
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..];

    // Map the keyword to its canonical StaticAbility expansion.
    var grantedKeywordAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedKeywordAbility is null)
    {
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ModifyPTEffect
          {
            Target = target,
            PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
            ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
          },
          new GainAbilityEffect
          {
            Target = target,
            GainedAbility = grantedKeywordAbility,
          },
          new AddTypeEffect
          {
            Target = target,
            AddedSubtypes = [subtype],
          },
        ],
      },
    ];
  }
}
