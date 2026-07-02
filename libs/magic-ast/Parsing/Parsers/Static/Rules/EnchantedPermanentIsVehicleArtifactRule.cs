namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Enchanted permanent is a Vehicle artifact with crew N and it loses all other card types."
/// — the Swift Reconfiguration static ability (Aura from NEO) that transforms any enchanted
/// permanent into a Vehicle artifact and strips its non-artifact card types.
///
/// <para>
/// The oracle clause decomposes into three simultaneous continuous effects (all targeting
/// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>):
/// <list type="bullet">
///   <item><b>SetCardTypes</b> (layer 4, CR 613.1d) — sets the permanent's card types to
///   <c>["artifact"]</c>, which together with "it loses all other card types" establishes
///   the complete type picture. CR 205.2: artifact is a card type.</item>
///   <item><b>ChangeSubtype</b> (layer 4, CR 613.1d) — sets the permanent's subtype to
///   <c>["Vehicle"]</c>. CR 301.7: Vehicle is a subtype of artifact.</item>
///   <item><b>GainAbility</b> (layer 6, CR 613.1f) — grants <c>Crew N</c> (CR 702.122),
///   structured as a <see cref="StaticAbility"/> carrying a <see cref="CrewEffect"/>
///   with the printed power threshold, mirroring the native Crew encoding on Vehicle cards
///   (e.g. Bomat Bazaar Barge, Ripclaw Wrangler).</item>
/// </list>
/// </para>
///
/// <para>
/// The parenthetical reminder "(It's not a creature unless it's crewed.)" is stripped by
/// <see cref="StaticRuleHelpers.StripReminderText"/> before matching. The reminder text is
/// not modelled: the rule engine applies it via CR 702.122a (Crew's becomes-artifact-creature
/// clause). The <see cref="Parenthetical"/> is preserved in the emitted
/// <see cref="StaticAbility.Reminder"/> field for round-tripping.
/// </para>
///
/// <para>
/// CR 702.122 (Crew): "Crew N" means "Tap any number of other untapped creatures you control
/// with total power N or greater: This permanent becomes an artifact creature until end of turn."
/// CR 301.7 (Vehicles): "Some artifacts have the subtype 'Vehicle.'"
/// CR 205.1a: an effect that sets card types replaces existing types not listed.
/// </para>
/// </summary>
[StaticRule(Priority = 970)]
public sealed class EnchantedPermanentIsVehicleArtifactRule : IStaticRule
{
  // "Enchanted permanent is a Vehicle artifact with crew N and it loses all other card types."
  // Optional trailing reminder "(It's not a creature unless it's crewed.)" is captured in
  // group <reminder> for round-trip preservation. <n> is the printed Crew power integer.
  private static readonly Regex _pattern = new(
    @"^\s*Enchanted\s+permanent\s+is\s+a\s+Vehicle\s+artifact\s+with\s+crew\s+(?<n>\d+)\s+and\s+it\s+loses\s+all\s+other\s+card\s+types\.?\s*(?<reminder>\([^)]+\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var crewPower = int.Parse(match.Groups["n"].Value);
    var reminderRaw = match.Groups["reminder"].Value.Trim();
    Parenthetical? reminder = string.IsNullOrEmpty(reminderRaw)
      ? null
      : new Parenthetical { Text = reminderRaw };

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          // Layer 4: set the enchanted permanent's card types to artifact only.
          // "it loses all other card types" — only 'artifact' remains.
          new SetCardTypesEffect
          {
            Subject = target,
            CardTypes = ["artifact"],
          },
          // Layer 4: set the enchanted permanent's subtype to Vehicle.
          // "is a Vehicle artifact" — Vehicle is the sole subtype declared.
          new ChangeSubtypeEffect
          {
            Target = target,
            Subtypes = ["Vehicle"],
          },
          // Layer 6: grant Crew N to the enchanted permanent.
          // "with crew N" — the printed power threshold for the Crew ability.
          new GainAbilityEffect
          {
            Target = target,
            GainedAbility = new StaticAbility
            {
              KeywordSource = KeywordAbility.Crew,
              Effects =
              [
                new CrewEffect
                {
                  Power = new LiteralQuantity { Value = crewPower },
                },
              ],
            },
          },
        ],
        Reminder = reminder,
      },
    ];
  }
}
