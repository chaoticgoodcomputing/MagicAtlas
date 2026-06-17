namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Recognises the Underworld Breach (THB) two-sentence static paragraph:
/// <list type="bullet">
///   <item>"Each nonland card in your graveyard has escape. The escape cost is
///   equal to the card's mana cost plus exile three other cards from your
///   graveyard. (You may cast cards from your graveyard for their escape cost.)"</item>
/// </list>
///
/// <para>
/// This is a continuous static ability (CR 604) that grants escape (CR 702.139)
/// to every nonland card in the controller's graveyard, with the escape cost
/// defined as the card's own mana cost plus exiling three other graveyard cards.
/// The parenthetical is standard Escape reminder text (Rule 207.2) and is
/// captured into <see cref="Ability.Reminder"/>.
/// </para>
///
/// <para>
/// The granted ability is modelled as a <see cref="GainAbilityEffect"/> whose
/// <see cref="GainAbilityEffect.Target"/> is each nonland card in your graveyard
/// and whose <see cref="GainAbilityEffect.GainedAbility"/> is a
/// <see cref="StaticAbility"/> with <see cref="EscapeEffect"/>. The escape cost's
/// mana half is a <see cref="SelfManaCost"/> (the card's own mana cost — a
/// self-referential reference, not a fixed mana cost); the exile half is
/// represented by <see cref="EscapeEffect.CardsToExile"/> = 3.
/// </para>
///
/// <para>
/// Rule citations: CR 604 (static abilities), CR 702.139 (Escape),
/// CR 202.1 (mana cost), CR 207.2 (reminder text).
/// </para>
/// </summary>
[StaticRule(Priority = 95)]
public sealed class EachNonlandGraveyardHasEscapeRule : IStaticRule
{
  // Matches the canonical Underworld Breach oracle paragraph (both sentences
  // plus optional trailing parenthetical reminder). Named group "reminder"
  // captures the parenthetical so it can be attached to the returned ability.
  private static readonly Regex _pattern = new(
    @"^\s*Each\s+nonland\s+card\s+in\s+your\s+graveyard\s+has\s+escape\.\s+"
      + @"The\s+escape\s+cost\s+is\s+equal\s+to\s+the\s+card's\s+mana\s+cost\s+"
      + @"plus\s+exile\s+three\s+other\s+cards\s+from\s+your\s+graveyard\."
      + @"\s*(?<reminder>\([^)]*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // Capture the optional reminder parenthetical if present.
    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // The granted escape ability: EscapeEffect with the card's own mana cost
    // (SelfManaCost — a reference, not a fixed printed cost) and exile-3 count.
    var grantedEscapeAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Escape,
      Effects =
      [
        new EscapeEffect
        {
          Cost = new SelfManaCost(),
          CardsToExile = LiteralQuantity.Of(3),
        },
      ],
    };

    // The outer static ability grants escape to each nonland card in your
    // graveyard. Filter: CardTypes=["card"], ExcludedCardTypes=["land"],
    // Controller=You, Zone=Graveyard — "each nonland card in your graveyard."
    return
    [
      new StaticAbility
      {
        Reminder = reminder,
        Effects =
        [
          new GainAbilityEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["card"],
                ExcludedCardTypes = ["land"],
                Controller = ControllerFilter.You,
                Zone = Zone.Graveyard,
              },
            },
            GainedAbility = grantedEscapeAbility,
          },
        ],
      },
    ];
  }
}
