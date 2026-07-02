namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Untap [CardName] and put a +1/+1 counter on it." — the compound activated
/// effect on Grimgrin, Corpse-Born (and similar self-referencing activated
/// abilities). Expands to a flat two-element effect list:
/// <list type="number">
///   <item><see cref="UntapEffect"/> targeting <see cref="ObjectReferenceKind.Self"/>
///   — "Untap [Name]" where the card refers to itself by name (CR 201.5 self-reference,
///   equivalent to "Untap this permanent").</item>
///   <item><see cref="PutCountersEffect"/> targeting <see cref="ObjectReferenceKind.Self"/>
///   with +1/+1 counter — "put a +1/+1 counter on it" where "it" is the pronoun
///   back-referencing the untapped permanent (CR 701.26b; CR 122.1).</item>
/// </list>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects appear
/// as a flat sibling pair on <c>Effects</c>, following the same convention as
/// <see cref="DrawThenSelfToTopOfLibraryRule"/>. <see cref="TryMatch"/> always
/// returns null so the single-effect path never fires.
/// </para>
///
/// <para>
/// Priority 951 — above the generic <see cref="UntapEffectRule"/> (Priority 993)
/// so this compound form is tried via the multi-effect path first. Pattern is
/// anchored (^…$) to prevent false-positive substring matches on richer ability
/// text.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 951)]
public sealed class UntapSelfNameAndPutCounterEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // "Untap [CardName] and put a +1/+1 counter on it[.]"
  // The card name is self-referential (CR 201.5): one or more capitalised words
  // (with optional comma-epithet, as in legendary card names like
  // "Grimgrin, Corpse-Born"). The name precedes "and put"; this anchored pattern
  // ensures we don't mismatch a more-specific sibling.
  private static readonly Regex _pattern = new(
    @"^Untap\s+[A-Z][A-Za-z'\-]+(?:,\s+[A-Za-z'\-]+(?:\s+[A-Za-z'\-]+)*)?\s+and\s+put\s+a\s+\+1/\+1\s+counter\s+on\s+it\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — exclusively handled by <see cref="TryMatchMulti"/>.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    if (!_pattern.IsMatch(effectText.Trim()))
    {
      return false;
    }

    effects = new List<Effect>
    {
      // "Untap [Name]" — self-reference (CR 201.5); the named card refers to itself.
      // CR 701.26b: "To untap a permanent, rotate it back to the upright position
      // from a sideways position."
      new UntapEffect
      {
        Target = ObjectReference.Self(),
      },

      // "put a +1/+1 counter on it" — "it" is the pronoun for the just-untapped
      // permanent, which is the source (Self). CR 122.1: a counter is a marker
      // placed on an object; +1/+1 counters modify P/T.
      new PutCountersEffect
      {
        Target = ObjectReference.Self(),
        CounterType = "+1/+1",
        Count = LiteralQuantity.Of(1),
      },
    };
    return true;
  }
}
