namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Counter target spell. If that spell is countered this way, put it on top of its
/// owner's library instead of into that player's graveyard." — Lapse of Certainty.
///
/// <para>
/// A single-effect counter whose follow-up sentence redirects the countered spell's
/// zone-change: instead of the default graveyard (CR 701.6a — "A countered spell is
/// put into its owner's graveyard.") the spell is put on top of its owner's library.
/// The redirect is a property on the counter effect (mirroring
/// <see cref="CounterSpellEffect.ExileInsteadOfGraveyard"/>), so this whole
/// two-sentence surface collapses to one <c>counterSpell</c> effect with
/// <see cref="CounterSpellEffect.TopOfLibraryInsteadOfGraveyard"/> set.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>) on the FULL two-sentence text — the base
/// <see cref="CounterSpellRule"/> is anchored to end right after "spell", so it never
/// claims this longer surface, and the sentence-bundle splitter cannot parse the
/// dependent follow-up ("If that spell is countered this way, …") as a standalone
/// effect, so dispatch falls through to the whole-text rule chain where this fires.
/// </para>
/// </summary>
[SpellRule(Priority = 82)]
public sealed class CounterSpellPutOnTopInsteadOfGraveyardRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Counter\s+target\s+spell\.\s+"
    + @"If\s+that\s+spell\s+is\s+countered\s+this\s+way,\s+"
    + @"put\s+it\s+on\s+top\s+of\s+its\s+owner's\s+library\s+"
    + @"instead\s+of\s+into\s+that\s+player's\s+graveyard\.?$",
    RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text))
    {
      return false;
    }

    effect = new CounterSpellEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["spell"] },
      },
      TopOfLibraryInsteadOfGraveyard = true,
    };
    return true;
  }
}
