namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create a [TokenA] token or a [TokenB] token" — a one-of-two CHOICE between
/// creating one predefined artifact token or another (Tireless Provisioner:
/// "create a Food token or a Treasure token").
///
/// <para>
/// "X or Y" is a modal choice the controller makes on resolution, not two
/// creations — Rule 700.2 (modal). It is modelled as a <see cref="ModalEffect"/>
/// with <see cref="ModeSelection.ChooseOne"/> whose two modes each wrap a single
/// <see cref="CreateTokenEffect"/> in a <see cref="SpellAbility"/>, mirroring the
/// bullet-modal shape produced for "choose one —" triggers (Saurian Symbiote).
/// </para>
///
/// <para>
/// Both options name a predefined artifact token whose colorless, P/T-less body
/// is fully specified by its subtype: Treasure (Rule 111.10a) and Food
/// (Rule 111.10b). Their activated abilities are reminder text (stripped to the
/// ability's Reminder field upstream), so each token reuses the predefined
/// <see cref="TokenDefinition"/> factory rather than re-parsing the body.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class CreateTokenChoiceRule : ITriggeredRule
{
  // "create a <A> token or a <B> token" — two predefined-token names joined by "or".
  // Rule 111.10: predefined artifact tokens (Treasure, Food, Clue, Blood) are named
  // solely by their subtype with no P/T or colour.
  private static readonly Regex _choicePattern = new(
    @"^create\s+a\s+(?<first>Food|Treasure|Clue|Blood)\s+token\s+or\s+a\s+(?<second>Food|Treasure|Clue|Blood)\s+token\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var match = _choicePattern.Match(text);
    if (!match.Success)
    {
      return false;
    }

    var first = BuildPredefinedToken(match.Groups["first"].Value);
    var second = BuildPredefinedToken(match.Groups["second"].Value);
    if (first is null || second is null)
    {
      return false;
    }

    effect = new ModalEffect
    {
      ModeSelection = ModeSelection.ChooseOne(),
      Modes =
      [
        new ModalOption { Ability = WrapInSpell(first) },
        new ModalOption { Ability = WrapInSpell(second) },
      ],
    };
    return true;
  }

  // Each mode is one create-token effect, wrapped in a SpellAbility to match the
  // ModalOption.Ability contract used by the "choose one —" bullet-modal shape.
  private static SpellAbility WrapInSpell(CreateTokenEffect create) =>
    new() { Effects = [create] };

  private static CreateTokenEffect? BuildPredefinedToken(string name)
  {
    var token = name.ToLowerInvariant() switch
    {
      "food" => TokenDefinition.Food(),
      "treasure" => TokenDefinition.Treasure(),
      "clue" => TokenDefinition.Clue(),
      "blood" => TokenDefinition.Blood(),
      _ => null,
    };
    if (token is null)
    {
      return null;
    }
    return new CreateTokenEffect { Player = ObjectReference.You(), Count = LiteralQuantity.Of(1), Token = token };
  }
}
