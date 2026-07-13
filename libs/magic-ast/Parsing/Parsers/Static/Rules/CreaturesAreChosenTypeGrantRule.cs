namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Arcane Adaptation oracle template: a static continuous effect that
/// additively grants the CHOSEN creature type (CR 607.1 linked-ability consumer of
/// the paired "As this enchantment enters, choose a creature type." producer —
/// <see cref="ChooseCreatureTypeOnEntryRule"/>) to creatures you control, creature
/// spells you control, and creature cards you own that aren't on the battlefield.
///
/// <para>
/// Oracle text (verbatim):
/// "Creatures you control are the chosen type in addition to their other types.
/// The same is true for creature spells you control and creature cards you own
/// that aren't on the battlefield."
/// </para>
///
/// <para>
/// Three scopes are modelled as three sibling <see cref="AddTypeEffect"/> nodes in
/// a single <see cref="StaticAbility"/>, each additively granting the chosen
/// creature-type subtype (<see cref="AddTypeEffect.ChosenSubtypeReference"/> =
/// <see cref="ChosenCharacteristicKind.CreatureType"/>) — parallels the sibling
/// Maskwood Nexus template (<see cref="CreaturesAreEveryCreatureTypeRule"/>), which
/// shares this exact three-scope tail sentence but grants Changeling (every type)
/// via <c>GainAbilityEffect</c> instead of one chosen type via <c>AddTypeEffect</c>:
/// <list type="number">
///   <item>Creatures you control — CardTypes: ["creature"], Controller: You (permanents)</item>
///   <item>Creature spells you control — CardTypes: ["spell", "creature"], Controller: You (on stack)</item>
///   <item>Creature cards you own not on battlefield — CardTypes: ["creature"], Owner: You (non-permanent zones)</item>
/// </list>
/// </para>
///
/// <para>
/// CR 205.1b (verbatim): "Some effects change an object's card type, subtype, and/or
/// supertype but specify that the object retains a prior card type, subtype, and/or
/// supertype. In such cases, all the object's prior card types, subtypes, and
/// supertypes are retained, and the effect causes the object to gain or lose other
/// card types, subtypes, and/or supertypes." CR 607.1: "An object may have two
/// abilities printed on it such that one of them causes actions to be taken or
/// objects or players to be affected and the other one directly refers to those
/// actions, objects, or players."
/// </para>
///
/// <para>
/// ANCHORED (^…$): the two-sentence oracle paragraph is matched in full so this
/// rule cannot fire on a substring of a more-specific sibling. Distinct wording
/// ("the chosen type" vs "every creature type") keeps this disjoint from
/// <see cref="CreaturesAreEveryCreatureTypeRule"/> regardless of dispatch order;
/// priority 971 matches that sibling's tier.
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CreaturesAreChosenTypeGrantRule : IStaticRule
{
  // Full anchored match for the two-sentence Arcane Adaptation oracle clause.
  // The two sentences arrive as a single paragraph (ClauseSplitter splits on
  // newlines, not on periods within a paragraph).
  private static readonly Regex _pattern = new(
    @"^\s*Creatures\s+you\s+control\s+are\s+the\s+chosen\s+type\s+in\s+addition\s+to\s+their\s+other\s+types\.\s+"
      + @"The\s+same\s+is\s+true\s+for\s+creature\s+spells\s+you\s+control\s+and\s+creature\s+cards\s+you\s+own\s+"
      + @"that\s+aren't\s+on\s+the\s+battlefield\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          // Scope 1: Creatures you control — permanents on the battlefield.
          // CardTypes: ["creature"], Controller: You.
          new AddTypeEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
              },
            },
            ChosenSubtypeReference = ChosenCharacteristicKind.CreatureType,
          },

          // Scope 2: Creature spells you control — on the stack.
          // CardTypes: ["spell", "creature"] encodes "creature spell" per the
          // BuildTypeSpellFilter convention (StaticRuleHelpers): a spell that
          // also has the creature type. Controller: You.
          new AddTypeEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["spell", "creature"],
                Controller = ControllerFilter.You,
              },
            },
            ChosenSubtypeReference = ChosenCharacteristicKind.CreatureType,
          },

          // Scope 3: Creature cards you own not on the battlefield — in hand,
          // library, graveyard, exile, command zone, etc.
          // CardTypes: ["creature"], Owner: You (ownership, CR 108.3 — distinct
          // from control). Zone omitted: the "card" / "not on the battlefield"
          // distinction is implicit in "you own", mirroring the Maskwood Nexus
          // precedent (CreaturesAreEveryCreatureTypeRule).
          new AddTypeEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Owner = ControllerFilter.You,
              },
            },
            ChosenSubtypeReference = ChosenCharacteristicKind.CreatureType,
          },
        ],
      },
    ];
  }
}
