namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Activated land blink (self-flicker of a controlled land): "Exile target land you
/// control, then return it to the battlefield under your control." — moves a targeted
/// land the controller controls to exile and then immediately returns it under the
/// ability's controller (Ruin Ghost's {W}, {T} ability). This is the land analogue of
/// <see cref="ExileAnotherCreatureThenReturnRule"/>; the two effects sit as a flat
/// sibling pair on <c>Effects</c>.
///
/// <para>
/// "target land you control" is an <see cref="ObjectReferenceKind.Target"/> land with a
/// <see cref="ControllerFilter.You"/> filter (CR 109.5 — "you"/"your" refer to the
/// object's controller). "then return it" is the linked exiled reference (ADR 0004,
/// reference-not-resolution): an <see cref="ObjectReferenceKind.Designated"/> card in
/// <see cref="Zone.Exile"/> whose <see cref="ObjectFilter.ExiledWith"/> points to
/// <see cref="ObjectReferenceKind.Self"/> (the same ability instance that exiled it).
/// "under your control" rides on <see cref="ReturnToBattlefieldEffect.UnderControl"/> as
/// an <see cref="ObjectReferenceKind.You"/> reference, matching
/// <see cref="ExileSelfThenReturnToBattlefieldRule"/>.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as a
/// flat sibling pair, consistent with the creature-blink rules. <see cref="TryMatch"/>
/// always returns null so the single-effect path never claims this sentence.
/// </para>
///
/// CR 406.2 (verbatim): "To exile an object is to put it into the exile zone from
/// whatever zone it's currently in. An exiled card is a card that's been put into the
/// exile zone." CR 400.7: "An object that moves from one zone to another becomes a new
/// object with no memory of, or relation to, its previous existence." CR 109.5: "The
/// words 'you' and 'your' on an object refer to the object's controller, its would-be
/// controller (if a player is attempting to play, cast, or activate it), or its owner
/// (if it has no controller)."
/// </summary>
[ActivatedEffectRule(Priority = 947)]
public sealed class ExileTargetLandYouControlThenReturnRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // "Exile target land you control, then return it to the battlefield under your control".
  // Anchored end-to-end so this never claims a longer, more-specific sibling sentence.
  private static readonly Regex Pattern = new(
    @"^Exile\s+target\s+land\s+you\s+control,\s*then\s+return\s+it\s+to\s+the\s+battlefield\s+under\s+your\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns null — this shape always produces two sibling effects, so it is
  /// served exclusively via <see cref="TryMatchMulti"/>.
  /// </remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    if (!Pattern.IsMatch(effectText.Trim().TrimEnd('.')))
    {
      return false;
    }

    effects = new List<Effect>
    {
      new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["land"],
            Controller = ControllerFilter.You,
          },
        },
      },
      new ReturnToBattlefieldEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Designated,
          Filter = new ObjectFilter
          {
            Zone = Zone.Exile,
            ExiledWith = ObjectReference.Self(),
          },
        },
        UnderControl = ObjectReference.You(),
      },
    };
    return true;
  }
}
