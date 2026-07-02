namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "For each planeswalker you control, you may activate one of its loyalty
/// abilities once this turn as though none of its loyalty abilities have been
/// activated this turn."
///
/// <para>
/// CR 606.3: the once-per-turn restriction on loyalty-ability activation.
/// This activated-effect rule recognises the Chain Veil family: a per-planeswalker
/// permission to activate a loyalty ability once this turn, ignoring the
/// once-per-turn restriction (the "as though none … have been activated"
/// clause).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the surface phrase "loyalty abilities" could appear
/// inside other ability text; anchoring prevents this rule from matching a
/// substring of a broader effect.
/// Priority 985 — specific enough to precede generic effects.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class ActivateLoyaltyAbilityAsThoughEffectRule : IActivatedEffectRule
{
  // "For each planeswalker you control, you may activate one of its loyalty
  //  abilities once this turn as though none of its loyalty abilities have been
  //  activated this turn."
  private static readonly Regex Pattern = new(
    @"^For\s+each\s+planeswalker\s+you\s+control,\s+you\s+may\s+activate\s+one\s+of\s+its\s+loyalty\s+abilities\s+once\s+this\s+turn\s+as\s+though\s+none\s+of\s+its\s+loyalty\s+abilities\s+have\s+been\s+activated\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new ActivateLoyaltyAbilityAsThoughEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["planeswalker"],
          Controller = ControllerFilter.You,
        },
      },
      Count = 1,
    };
  }
}
