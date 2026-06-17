namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "You may copy the exiled card. If you do, you may cast the copy without paying
/// its mana cost." — Isochron Scepter's activated-ability effect (MRD:200).
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to put
/// a copy of it onto the stack … a copy of a spell isn't cast." Here the copy is made
/// from a card in exile (the imprinted card), not from the stack; it is a copy of the
/// card, and then the controller MAY cast that copy — two separate optional actions.
/// The first "you may" (copy) gates the second "you may" (cast without paying) via an
/// <see cref="OptionalEffect.IfYouDo"/> consequence (ADR 0005 / CR 117.7).
/// </para>
///
/// <para>
/// "The exiled card" refers to the card exiled by this permanent's Imprint trigger
/// (CR 406.6 linked ability). It is modelled as an <see cref="ObjectReferenceKind.Any"/>
/// reference with Zone.Exile + ExiledWith: Self — a linked-ability reference (ADR 0004
/// "reference not resolution") that names the linking object, not a threaded binding.
/// The copy target is then referenced as <see cref="ObjectReferenceKind.It"/>.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> because the two-sentence body
/// forms ONE <see cref="OptionalEffect"/> (not two sibling effects): the "If you do"
/// clause is the consequence of choosing to copy, not an independent effect. Returning
/// null from <see cref="TryMatch"/> ensures the single-effect path never claims the text.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 960)]
public sealed class CopyExiledCardAndCastWithoutPayingEffectRule
  : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // Two-sentence form: "You may copy the exiled card. If you do, you may cast the copy
  // without paying its mana cost."
  private static readonly Regex Pattern = new(
    @"^You\s+may\s+copy\s+the\s+exiled\s+card\.\s+If\s+you\s+do,\s+you\s+may\s+cast\s+the\s+copy\s+without\s+paying\s+its\s+mana\s+cost\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape is served exclusively via TryMatchMulti.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = effectText.Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    // "the exiled card" = the card in exile exiled with this permanent (CR 406.6)
    var exiledCardTarget = new ObjectReference
    {
      Kind = ObjectReferenceKind.Any,
      Filter = new ObjectFilter
      {
        Zone = Zone.Exile,
        ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
      },
    };

    // "You may copy the exiled card. If you do, you may cast the copy without
    // paying its mana cost." — one OptionalEffect wrapping CopyEffect, with an
    // IfYouDo consequence that is itself an OptionalEffect wrapping CastWithoutPayingEffect.
    effects = new List<Effect>
    {
      new OptionalEffect
      {
        Inner = new CopyEffect
        {
          Target = exiledCardTarget,
        },
        IfYouDo = new OptionalEffect
        {
          Inner = new CastWithoutPayingEffect
          {
            Target = ObjectReference.It(),
          },
        },
      },
    };
    return true;
  }
}
