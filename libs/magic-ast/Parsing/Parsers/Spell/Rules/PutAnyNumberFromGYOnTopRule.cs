namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put any number of target creature cards from your graveyard on top of your library."
/// Covers the unbounded multi-target graveyard-to-library-top zone change.
/// Examples: Forever Young (ELD), Gravepurge (DTK), Footbottom Feast (CMD).
/// Count is modelled as <see cref="UpToQuantity"/> with <see cref="int.MaxValue"/> maximum
/// — the same convention used for "choose any number" modal abilities (Rule 700.2).
/// Source zone on <see cref="ObjectFilter.Zone"/>; count on <see cref="ObjectReference.Quantity"/>.
/// </summary>
[SpellRule]
public sealed class PutAnyNumberFromGYOnTopRule : ISpellRule
{
  // "Put any number of target creature cards from your graveyard on top of your library."
  private static readonly Regex Pattern = new(
    @"^Put\s+any\s+number\s+of\s+target\s+(?<type>creature|artifact|enchantment|land|permanent|card)\s+cards?\s+from\s+your\s+graveyard\s+on\s+top\s+of\s+your\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var typeWord = m.Groups["type"].Value.ToLowerInvariant();

    effect = new PutOnTopOfLibraryEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [typeWord],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
        // "any number" = unbounded upTo; int.MaxValue follows the modal-ability convention
        // (ModalAbilityParser / TriggeredAbilityParser both use ChooseUpTo(int.MaxValue)
        // for "choose any number" headers — Rule 700.2).
        Quantity = new UpToQuantity { Maximum = int.MaxValue, Minimum = 0 },
      },
    };
    return true;
  }
}
