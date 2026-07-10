namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// The Gift-keyword damage-body shape (Wildfire Howl): "[Self] deals N damage to each
/// creature. If the gift was promised, instead [Self] deals M damage to any target and
/// N damage to each creature."
///
/// <para>The word "instead" makes the second sentence a REPLACEMENT of the first when
/// the gift was promised (CR 702.174k defines "promised"; the promised branch is stated
/// verbatim including the "to each creature" damage, so it is a full substitution, not an
/// addition). Modelled as a single <see cref="ConditionalEffect"/> — an if-then-else,
/// mirroring the "instead" convention used elsewhere in the AST — whose
/// <see cref="ConditionalEffect.Condition"/> is a <see cref="GiftPromisedCondition"/>:
/// <list type="bullet">
///   <item><see cref="ConditionalEffect.Then"/> (gift promised) — the "instead" branch: a
///   <see cref="CompositeEffect"/> of a <see cref="DealDamageEffect"/> to
///   <see cref="ObjectReferenceKind.AnyTarget"/> and one to every creature.</item>
///   <item><see cref="ConditionalEffect.Else"/> (gift not promised) — the base sweep: a
///   <see cref="DealDamageEffect"/> to every creature.</item>
/// </list>
/// Every damage source is <see cref="ObjectReference.Self"/> (the spell names itself; CR
/// 120.1 — an object that deals damage is the source of that damage). The self-reference
/// subject captured by the regex is the card's own printed name and is required to start
/// uppercase, mirroring <see cref="DealDamageToEachRule"/>.</para>
///
/// <para>ANCHORED (^...$) on the full "… deals N damage to each creature. If the gift was
/// promised, instead … deals M damage to any target and N damage to each creature" surface:
/// the "If the gift was promised, instead" phrase is unique to gift cards, so this cannot
/// claim a substring of any broader sibling clause. CR 702.174e: "'Gift a card' means the
/// effect is 'The chosen player draws a card.'"; CR 702.174j: for instant and sorcery
/// spells the gift effect always happens before any other spell abilities of the card.</para>
/// </summary>
[SpellRule(Priority = 80)]
public sealed class DealDamageToEachCreatureGiftInsteadRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^(?<subj1>\S.*?)\s+deals?\s+(?<base>\d+)\s+damage\s+to\s+each\s+creature\.\s+If\s+the\s+gift\s+was\s+promised,\s+instead\s+(?<subj2>\S.*?)\s+deals?\s+(?<extra>\d+)\s+damage\s+to\s+any\s+target\s+and\s+(?<base2>\d+)\s+damage\s+to\s+each\s+creature$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = Pattern.Match(text);
    if (!m.Success
        || !char.IsUpper(m.Groups["subj1"].Value[0])
        || !char.IsUpper(m.Groups["subj2"].Value[0]))
    {
      return false;
    }

    var baseAmount = LiteralQuantity.Of(int.Parse(m.Groups["base"].Value));
    var extraAmount = LiteralQuantity.Of(int.Parse(m.Groups["extra"].Value));
    var insteadEachAmount = LiteralQuantity.Of(int.Parse(m.Groups["base2"].Value));

    static DealDamageEffect ToEachCreature(Quantity amount) => new()
    {
      Amount = amount,
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
    };

    effect = new ConditionalEffect
    {
      Condition = new GiftPromisedCondition(),
      Then = new CompositeEffect
      {
        Effects =
        [
          new DealDamageEffect
          {
            Amount = extraAmount,
            Source = ObjectReference.Self(),
            Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
          },
          ToEachCreature(insteadEachAmount),
        ],
      },
      Else = ToEachCreature(baseAmount),
    };
    return true;
  }
}
