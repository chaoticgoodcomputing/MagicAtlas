namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Exchange control of two target lands." — Shifting Borders, Vedalken Plotter,
/// Political Trickery. Two target lands swap controllers as the spell resolves.
///
/// <para>
/// CR 701.12a: "A spell or ability may instruct players to exchange something (for
/// example, life totals or control of two permanents) as part of its resolution."
/// The two permanents whose control is exchanged here are two target lands
/// (CR 205.3j — land as card type), so this is the control-exchange facet of the
/// same primitive the life-totals exchange uses.
/// </para>
///
/// <para>
/// Reuses the shared <see cref="ExchangeCharacteristicEffect"/> with its pre-existing
/// <see cref="ExchangeableCharacteristic.Control"/> facet — no new discriminator. The
/// two exchanged objects are both targeted lands, so <c>First</c> and <c>Second</c>
/// are each a <see cref="ObjectReferenceKind.Target"/> reference filtered to
/// <c>CardTypes=["land"]</c>. Anchored (<c>^…$</c>) so it can never claim a substring
/// of a broader "exchange control of two target [creatures|permanents]" sentence.
/// </para>
/// </summary>
[SpellRule]
public sealed class ExchangeControlOfTwoTargetLandsRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Exchange\s+control\s+of\s+two\s+target\s+lands$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new ExchangeCharacteristicEffect
    {
      Characteristic = ExchangeableCharacteristic.Control,
      First = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["land"] },
      },
      Second = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["land"] },
      },
    };
    return true;
  }
}
