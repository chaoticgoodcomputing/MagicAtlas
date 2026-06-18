namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "You get an emblem with [LDQUO]You may activate loyalty abilities of planeswalkers
/// you control on any player's turn any time you could cast an instant.[RDQUO]" --
/// Teferi, Temporal Archmage -10 loyalty ability.
///
/// <para>
/// Rule 114.2: "An effect that creates an emblem is written '[Player] gets an emblem
/// with [ability].' This means that [player] puts an emblem with [ability] into the
/// command zone."
/// </para>
///
/// <para>
/// The emblem grants a <see cref="TimingModificationEffect"/> that relaxes the
/// loyalty-ability activation restriction (CR 606.3: normally a player may activate
/// a loyalty ability only during a main phase on their own turn while the stack is
/// empty). With this emblem the controller may activate loyalty abilities of
/// planeswalkers they control on any player's turn any time they could cast an instant.
/// </para>
///
/// <para>
/// The timing grant applies to activated abilities of planeswalkers the controller
/// controls -- modelled as <see cref="ObjectActivatedAbilityReference"/> with
/// <see cref="ObjectFilter.CardTypes"/> = ["planeswalker"] and
/// <see cref="ControllerFilter.You"/>.
/// "On any player's turn" is recorded on <see cref="TimingModificationEffect.WhoseTurn"/>
/// = "AnyTurn" -- CR 606.3 establishes the default turn restriction; this field
/// documents its removal.
/// </para>
///
/// <para>
/// ANCHORED: the opening "You get an emblem with" is shared with other emblem-grant
/// shapes. The inner ability text is quoted verbatim from Teferi, Temporal Archmage
/// (C14/C16/CMR) and anchoring is the defensive convention.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 970)]
public sealed class CreateEmblemLoyaltyTimingGrantActivatedEffectRule : IActivatedEffectRule
{
  // The inner emblem ability text, verbatim from oracle.
  // "You may activate loyalty abilities of planeswalkers you control on any
  //  player's turn any time you could cast an instant."
  //
  // Oracle text uses curly/smart quotes (U+201C = left double quotation mark,
  // U+201D = right double quotation mark) and curly apostrophe (U+2019) for
  // "player's". Regular double-quote fallback also accepted.
  //
  // Character class for surrounding quotes: [“”"'] (left/right curly
  //   double quote, straight double quote, straight single quote)
  // Character class for apostrophe in "player's": [’'] (curly/straight)
  private static readonly Regex _pattern = new(
    "^\\s*You\\s+get\\s+an\\s+emblem\\s+with\\s+"
    + "[“”\"']"
    + "You\\s+may\\s+activate\\s+loyalty\\s+abilities\\s+of\\s+planeswalkers\\s+you\\s+control"
    + "\\s+on\\s+any\\s+player[’']s\\s+turn\\s+any\\s+time\\s+you\\s+could\\s+cast\\s+an\\s+instant\\."
    + "[“”\"']"
    + "\\s*\\.?\\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText))
    {
      return null;
    }

    // The emblem has one static ability: a timing-modification grant that allows
    // the controller to activate loyalty abilities of planeswalkers they control
    // on any player's turn at instant speed.
    var emblemAbility = new StaticAbility
    {
      Effects =
      [
        new TimingModificationEffect
        {
          Modification = TimingModificationType.Grant,
          Timing = TimingWindow.Instant,
          WhoseTurn = "AnyTurn",
          AppliesTo = new ObjectActivatedAbilityReference
          {
            PermanentFilter = new ObjectFilter
            {
              CardTypes = ["planeswalker"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
    };

    return new CreateEmblemEffect
    {
      Emblem = new EmblemDefinition
      {
        Abilities = [emblemAbility],
      },
    };
  }
}
