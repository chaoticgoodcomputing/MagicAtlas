namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player mills that many cards" — the Mindcrank family: whenever an opponent
/// loses life, that player mills that many cards.
///
/// <para>
/// The antecedent of "that many" is the life lost by the opponent in the triggering
/// event (CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly."). The amount is encoded as a
/// <see cref="DerivedQuantity"/> keyed on <see cref="DerivedKind.LifeLost"/>.
/// "That player" is a back-reference to the player identified by the
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.LosesLife"/> trigger's filter
/// (CR 603.2: the trigger fires whenever the event matches), encoded as
/// <see cref="ObjectReferenceKind.ThatPlayer"/>.
/// </para>
///
/// <para>
/// CR 701.17 defines "mill" as a keyword action: "To mill N cards, a player puts the
/// top N cards of their library into their graveyard." The player mills cards equal
/// to the amount of life lost in the triggering event.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThatPlayerMillsThatManyRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+mills\s+that\s+many\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new MillEffect
    {
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
      Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
