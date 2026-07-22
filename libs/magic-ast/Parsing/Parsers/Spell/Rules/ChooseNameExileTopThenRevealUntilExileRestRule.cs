namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Demonic Consultation: "Choose a card name. Exile the top six cards of your
/// library, then reveal cards from the top of your library until you reveal a card
/// with the chosen name. Put that card into your hand and exile all other cards
/// revealed this way."
///
/// <para>
/// The full three-sentence spell is a single coherent resolution and is matched as
/// one whole-text shape (mirroring the Nissa, Resurgent Animist
/// <c>AddManaThenIfSecondTimeRevealUntilRule</c> pattern): the middle sentence
/// chains a preliminary library-sculpting action ("Exile the top N cards...") with
/// the reveal-until search via "then", and "that card"/"all other cards revealed
/// this way" in the third sentence back-reference the search — none of the three
/// sentences parses independently, so the generic sentence-bundle splitter cannot
/// fragment this shape (it falls through to this whole-text multi-rule). Emits
/// three sibling effects on <see cref="MagicAST.AST.Abilities.SpellAbility.Effects"/>:
/// </para>
/// <list type="number">
///   <item>
///     <see cref="ChooseCardNameEffect"/> — "Choose a card name." (CR 201.4-adjacent
///     naming; reused unchanged from the as-enters card-name-choice family, here as a
///     plain spell effect rather than an as-enters static).
///   </item>
///   <item>
///     <see cref="ExileEffect"/> — "Exile the top six cards of your library" (CR
///     701.13a): a positionally-designated group of N cards from the top of the
///     controller's library, mirroring <c>ExileTopCardOfLibraryEffectRule</c>'s
///     single-card shape with a <see cref="MagicAST.AST.References.ObjectReference.Quantity"/>
///     of six.
///   </item>
///   <item>
///     <see cref="RevealUntilExileRestEffect"/> — "reveal cards from the top of your
///     library until you reveal a card with the chosen name. Put that card into your
///     hand and exile all other cards revealed this way." The stop condition is "a
///     card with the chosen name" — the structured consumer of the preceding
///     <see cref="ChooseCardNameEffect"/> declaration, via
///     <see cref="ChosenCharacteristicKind.CardName"/> (CR 607 linked ability),
///     mirroring <c>CounterTargetSpellWithChosenNameActivatedEffectRule</c>'s
///     analogous consumer.
///   </item>
/// </list>
///
/// <para>
/// Fully anchored (^…$) end to end across all three sentences, so it cannot
/// substring-match any sibling reveal/exile shape. Priority 90 — above the default
/// band, matching the specificity convention used by other whole-text multi-sentence
/// composite rules.
/// </para>
/// </summary>
[SpellRule(Priority = 90)]
public sealed class ChooseNameExileTopThenRevealUntilExileRestRule : ISpellRule, IMultiSpellRule
{
  private static readonly Regex _pattern = new(
    @"^Choose\s+a\s+card\s+name\.\s*Exile\s+the\s+top\s+(?<count>\w+)\s+cards?\s+of\s+your\s+library,\s*then\s+reveal\s+cards?\s+from\s+the\s+top\s+of\s+your\s+library\s+until\s+you\s+reveal\s+a\s+card\s+with\s+the\s+chosen\s+name\.\s*Put\s+that\s+card\s+into\s+your\s+hand\s+and\s+exile\s+all\s+other\s+cards\s+revealed\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc />
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false; // always multi — three sibling effects, never a single node.
  }

  /// <inheritdoc />
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    if (!SpellRuleHelpers.TryParseSmallWord(match.Groups["count"].Value, out var count))
    {
      return false;
    }

    var chooseName = new ChooseCardNameEffect();

    var exileTop = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Designated,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Library,
          Controller = ControllerFilter.You,
          // "the top six cards of your library" — positional block of N off the top
          // (CR 401.1); Count mirrors the exiled Quantity below.
          LibraryPosition = new LibraryPosition { Position = ZonePosition.Top, Count = count },
        },
        Quantity = LiteralQuantity.Of(count),
      },
    };

    var revealUntilExileRest = new RevealUntilExileRestEffect
    {
      Filter = new ObjectFilter
      {
        CardTypes = ["card"],
        ChosenCharacteristic = ChosenCharacteristicKind.CardName,
      },
      Player = ObjectReference.You(),
    };

    effects = new List<Effect> { chooseName, exileTop, revealUntilExileRest };
    return true;
  }
}
