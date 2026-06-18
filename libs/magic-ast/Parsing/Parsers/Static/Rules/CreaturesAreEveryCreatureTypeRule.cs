namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the Maskwood Nexus oracle template: a static continuous effect that
/// grants every creature type (the Changeling characteristic-defining ability)
/// to creatures you control, creature spells you control, and creature cards you
/// own that aren't on the battlefield.
///
/// <para>
/// Oracle text (verbatim):
/// "Creatures you control are every creature type. The same is true for creature
/// spells you control and creature cards you own that aren't on the battlefield."
/// </para>
///
/// <para>
/// Three scopes are modelled as three sibling <see cref="GainAbilityEffect"/> nodes
/// in a single <see cref="StaticAbility"/>, each granting the Changeling
/// characteristic-defining ability:
/// <list type="number">
///   <item>Creatures you control — CardTypes: ["creature"], Controller: You (permanents)</item>
///   <item>Creature spells you control — CardTypes: ["spell", "creature"], Controller: You (on stack)</item>
///   <item>Creature cards you own not on battlefield — CardTypes: ["creature"], Owner: You (non-permanent zones)</item>
/// </list>
/// </para>
///
/// <para>
/// CR 702.73a (verbatim): "Changeling is a characteristic-defining ability.
/// 'Changeling' means 'This object is every creature type.' This ability works
/// everywhere, even outside the game. See rule 604.3."
/// </para>
///
/// <para>
/// ANCHORED (^…$): the two-sentence oracle paragraph is matched in full to prevent
/// this rule from firing on any substring of a more-specific sibling. Priority 971
/// — above <see cref="NonlandCreatureTypeGrantRule"/> (970) since this shape covers
/// a broader set of zones and is more specific in oracle intent.
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CreaturesAreEveryCreatureTypeRule : IStaticRule
{
  // Full anchored match for the two-sentence Maskwood Nexus oracle clause.
  // The two sentences arrive as a single paragraph (ClauseSplitter splits on
  // newlines, not on periods within a paragraph).
  private static readonly Regex _pattern = new(
    @"^\s*Creatures\s+you\s+control\s+are\s+every\s+creature\s+type\.\s+The\s+same\s+is\s+true\s+for\s+creature\s+spells\s+you\s+control\s+and\s+creature\s+cards\s+you\s+own\s+that\s+aren't\s+on\s+the\s+battlefield\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// The Changeling keyword static ability — granted to each scope.
  /// CR 702.73a: "Changeling" means "This object is every creature type."
  /// </summary>
  private static readonly StaticAbility _changelingAbility = new()
  {
    KeywordSource = KeywordAbility.Changeling,
    Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Changeling }],
  };

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
          new GainAbilityEffect
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
            GainedAbility = _changelingAbility,
          },

          // Scope 2: Creature spells you control — on the stack.
          // CardTypes: ["spell", "creature"] encodes "creature spell" per the
          // BuildTypeSpellFilter convention (StaticRuleHelpers): a spell that
          // also has the creature type. Controller: You.
          new GainAbilityEffect
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
            GainedAbility = _changelingAbility,
          },

          // Scope 3: Creature cards you own not on the battlefield — in hand,
          // library, graveyard, exile, command zone, etc.
          // CardTypes: ["creature"], Owner: You (ownership, CR 108.3 — distinct
          // from control). Zone omitted: the "card" / "not on the battlefield"
          // distinction is implicit in "you own" (CR 108.3 / 109.4 ownership vs
          // control distinction). Mirrors the Mycosynth Lattice precedent of
          // using the card-type classifier without a Zone axis.
          new GainAbilityEffect
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
            GainedAbility = _changelingAbility,
          },
        ],
      },
    ];
  }
}
