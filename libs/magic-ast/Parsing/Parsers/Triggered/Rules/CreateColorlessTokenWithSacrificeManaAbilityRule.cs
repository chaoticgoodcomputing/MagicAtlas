namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create a [P/T] colorless [Subtype(s)] creature token with 'Sacrifice this token: Add {C}.'"
/// — colorless creature token whose only ability is a sacrifice-for-mana activated ability.
///
/// <para>
/// This covers Eldrazi Spawn and Eldrazi Scion tokens (CR 701.6: "Create" puts a token on
/// the battlefield; CR 111.1: a token is a marker representing a permanent that isn't a
/// card). The token has a single activated mana ability (CR 605.1a: an activated ability
/// that doesn't require a target and could add mana is a mana ability): "Sacrifice this
/// token: Add {C}." The sacrifice cost targets the token itself (CR 701.21a), encoded via
/// <see cref="ObjectFilter.IsSelf"/> = <c>true</c> on the <see cref="SacrificeCost.Filter"/>.
/// </para>
///
/// <para>
/// Runs at priority 75, above the generic <see cref="CreateTokenRule"/> (default 50), so
/// this specific shape is matched first and the generic rule never overwrites the token's
/// structured ability with an empty ability list.
/// </para>
///
/// <para>
/// Example: Glaring Fleshraker — "create a 0/1 colorless Eldrazi Spawn creature token
/// with \"Sacrifice this token: Add {C}.\""
/// </para>
/// </summary>
[TriggeredRule(Priority = 75)]
public sealed class CreateColorlessTokenWithSacrificeManaAbilityRule : ITriggeredRule
{
  // Matches: create a [P]/[T] colorless [Subtype1] [Subtype2?] creature token
  //          with "[Sacrifice this token: Add {C}.]"
  // \x22 = ASCII double-quote; “/” = Unicode curly quotes (left/right).
  // The quoted ability phrase must match exactly to avoid false positives.
  // Subtype words are capitalised (Rule 205.3m).
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+(?<power>\d+)/(?<toughness>\d+)\s+colorless\s+"
    + @"(?<sub1>[A-Z][A-Za-z]+)(?:\s+(?<sub2>[A-Z][A-Za-z]+))?\s+creature\s+token\s+with\s+"
    + @"[\x22“]Sacrifice\s+this\s+token:\s+Add\s+\{C\}\.[\x22”]",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var power = m.Groups["power"].Value;
    var toughness = m.Groups["toughness"].Value;
    var sub1 = m.Groups["sub1"].Value;
    var sub2 = m.Groups["sub2"].Success ? m.Groups["sub2"].Value : null;

    var subtypes = sub2 is not null
      ? new List<string> { sub1, sub2 }
      : new List<string> { sub1 };

    // The token's mana ability: "Sacrifice this token: Add {C}."
    // CR 605.1a: an activated ability is a mana ability if it doesn't require a target
    // and it could add mana when it resolves. The sacrifice cost pays by removing the
    // token from the battlefield (CR 701.21a); the effect adds one colorless mana ({C}).
    // The sacrifice filter targets the token itself (IsSelf=true, CardTypes=["creature"]).
    var tokenAbility = new ActivatedAbility
    {
      Costs =
      [
        new SacrificeCost
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            IsSelf = true,
          },
          Quantity = LiteralQuantity.Of(1),
        },
      ],
      Effects =
      [
        new AddManaEffect
        {
          Mana = "{C}",
          AnyColor = false,
          AnyType = false,
          OfChosenColor = false,
        },
      ],
      IsManaAbility = true,
    };

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = new TokenDefinition
      {
        Power = power,
        Toughness = toughness,
        Colors = ["C"],
        Types = ["creature"],
        Subtypes = subtypes,
        Abilities = [tokenAbility],
        IsCopy = false,
      },
    };
    return true;
  }
}
