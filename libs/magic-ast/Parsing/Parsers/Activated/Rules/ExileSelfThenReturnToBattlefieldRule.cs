namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile this creature, then return it to the battlefield under your control."
/// — the Soulbond blink pattern (Deadeye Navigator, CR 702.95). A single
/// ", then"-joined sentence that is two sibling effects: an <see cref="ExileEffect"/>
/// moving self to exile, followed by a <see cref="ReturnToBattlefieldEffect"/> returning
/// the designated just-exiled card to the battlefield under the ability's controller.
///
/// <para>
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever it is."
/// CR 400.7: the exiled object is the same object returned. The return target is
/// modelled as <see cref="ObjectReferenceKind.Designated"/> with
/// <see cref="ObjectFilter.ExiledWith"/> pointing to <see cref="ObjectReferenceKind.Self"/>,
/// mirroring the Felidar Guardian blink fixture's consumer side (ADR 0004:
/// reference-not-resolution).
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as
/// a flat sibling pair on <c>Effects</c>. <see cref="TryMatch"/> always returns null
/// so the single-effect path never claims this sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 949)]
public sealed class ExileSelfThenReturnToBattlefieldRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Exile\s+this\s+(?<type>creature|permanent|artifact|enchantment|land|planeswalker),\s*then\s+return\s+it\s+to\s+the\s+battlefield\s+under\s+your\s+control$",
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
        Target = ObjectReference.Self(),
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
        Tapped = false,
        UnderControl = ObjectReference.You(),
      },
    };
    return true;
  }
}
