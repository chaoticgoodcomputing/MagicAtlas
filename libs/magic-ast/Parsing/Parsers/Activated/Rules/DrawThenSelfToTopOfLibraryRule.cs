namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw a card, then put this [artifact/permanent] on top of its owner's library."
/// — Sensei's Divining Top. A single ", then"-joined sentence that is two sibling
/// effects: a <see cref="DrawCardsEffect"/> (you draw 1) followed by a
/// <see cref="PutOnTopOfLibraryEffect"/> moving this permanent (<see cref="ObjectReferenceKind.Self"/>)
/// to the top of its owner's library.
///
/// <para>
/// CR 121.1: "A player draws a card by putting the top card of their library into
/// their hand. … It may also be done as part of a cost or effect of a spell or ability."
/// The draw is the structured action; "on top of its owner's library" is the
/// structured destination carried by <see cref="PutOnTopOfLibraryEffect"/>'s
/// discriminator, not free text.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as
/// a flat sibling pair on <c>Effects</c> (the SurveilThenDraw / multi-sentence
/// convention), not nested under a CompositeEffect. <see cref="TryMatch"/> always
/// returns null so the single-effect path never claims the sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 950)]
public sealed class DrawThenSelfToTopOfLibraryRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Draw\s+a\s+card,\s*then\s+put\s+this\s+\w+\s+on\s+top\s+of\s+its\s+owner's\s+library$",
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
    if (!Pattern.IsMatch(effectText.Trim()))
    {
      return false;
    }

    effects = new List<Effect>
    {
      new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(1),
        Player = ObjectReference.You(),
      },
      new PutOnTopOfLibraryEffect
      {
        Target = ObjectReference.Self(),
      },
    };
    return true;
  }
}
