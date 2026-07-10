namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// "that player adds one mana of any type that land produced" — Dictate of Karametra
/// mana-doubler shape. Distinct from the Kinnan, Bonder Prodigy shape handled by
/// <see cref="AddManaRule"/> (bare "add one mana of any type that permanent produced",
/// implicit "you" subject because Kinnan's trigger is "you tap [a nonland permanent]"):
/// Dictate's trigger fires on "a player taps a land for mana" — ANY player, not just
/// Dictate's controller — so its effect names an EXPLICIT "that player" subject to send
/// the doubled mana to whichever player actually tapped the land, not to Dictate's
/// controller. <see cref="ObjectReferenceKind.ThatPlayer"/> is the pronoun for that
/// back-referenced player (CR 603.2: "that player" refers to the player identified by
/// the trigger event).
///
/// <para>
/// The produced-object noun ("land" here, "permanent" on Kinnan) names what was tapped
/// for mana; it doesn't change the effect's shape — <see cref="AddManaEffect.AnyType"/>
/// already generalizes to "mirrors whatever mana type the triggering tap event produced"
/// regardless of the tapped object's card type, so both nouns are accepted by one pattern.
/// </para>
///
/// CR 106.4: "When an effect instructs a player to add mana, that mana goes into a
/// player's mana pool."
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class ThatPlayerAddsManaOfAnyTypeProducedRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+adds\s+one\s+mana\s+of\s+any\s+type\s+that\s+(?:land|permanent)\s+produced$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var t = text.Trim().TrimEnd('.').Trim();
    if (!_pattern.IsMatch(t))
    {
      return false;
    }

    effect = new AddManaEffect
    {
      Mana = string.Empty,
      AnyType = true,
      Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
