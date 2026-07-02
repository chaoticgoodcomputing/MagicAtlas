namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Activated flicker (blink) effect: "Exile another target creature you control, then
/// return it to the battlefield under its owner's control." — moves a targeted other
/// creature the controller controls to exile and then immediately returns it, preserving
/// owner control on re-entry (e.g. Emiel the Blessed's {3} ability).
///
/// <para>
/// "another target creature" is modelled with <c>ExcludeSelf = true</c> (CR 109.5 —
/// "another" means other than the source object). "then return it" is the linked
/// exiled reference (ADR 0004 reference-not-resolution): a
/// <see cref="ObjectReferenceKind.Designated"/> card in <see cref="Zone.Exile"/>
/// whose <see cref="ObjectFilter.ExiledWith"/> points to <see cref="ObjectReferenceKind.Self"/>
/// (the same ability instance that exiled it — the Petravark shape). "under its owner's
/// control" rides on <see cref="ReturnToBattlefieldEffect.UnderControl"/> as an
/// <see cref="ObjectReferenceKind.Owner"/> reference (CR 400.6).
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as a
/// flat sibling pair on <c>Effects</c>, consistent with
/// <see cref="ExileSelfThenReturnToBattlefieldRule"/>. <see cref="TryMatch"/> always
/// returns null so the single-effect path never claims this sentence.
/// </para>
///
/// CR 701.13a (verbatim): "To exile an object, move it to the exile zone from wherever
/// it is." CR 400.6: "If an effect gives a player control of an object … that player
/// controls that object until the effect ends." CR 109.5: "The words 'you' and 'your'
/// on an object refer to the object's controller, its would-be controller (if a player
/// is casting it), or its owner (if it has no controller)."
/// </summary>
[ActivatedEffectRule(Priority = 948)]
public sealed class ExileAnotherCreatureThenReturnRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // "Exile another target creature [you control], then return it to the battlefield
  //  [tapped] under its owner's control". Both qualifiers are OPTIONAL: Emiel the Blessed
  //  exiles "another target creature you control" and returns it untapped; Eldrazi Displacer
  //  exiles "another target creature" (any) and returns it TAPPED. The presence of each
  //  qualifier drives the exile's Controller filter and the return's Tapped flag respectively.
  private static readonly Regex Pattern = new(
    @"^Exile\s+another\s+target\s+creature(?<youControl>\s+you\s+control)?,\s*then\s+return\s+it\s+to\s+the\s+battlefield(?<tapped>\s+tapped)?\s+under\s+its\s+owner'?s\s+control$",
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
    var match = Pattern.Match(effectText.Trim().TrimEnd('.'));
    if (!match.Success)
    {
      return false;
    }

    // "you control" → the exiled creature is controller-scoped (CR 109.5); absent → any creature.
    var youControl = match.Groups["youControl"].Success;
    // "return it ... tapped" → the re-entered permanent comes back tapped (Eldrazi Displacer).
    var tapped = match.Groups["tapped"].Success;

    effects = new List<Effect>
    {
      new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Controller = youControl ? ControllerFilter.You : null,
            ExcludeSelf = true,
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
            ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
          },
        },
        Tapped = tapped,
        UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
      },
    };
    return true;
  }
}
