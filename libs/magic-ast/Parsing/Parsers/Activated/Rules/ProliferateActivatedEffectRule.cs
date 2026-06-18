namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;

/// <summary>
/// "Proliferate." as a standalone activated-ability effect sentence
/// (CR 701.27, CR 701.34).
///
/// <para>
/// This mirrors <see cref="Spell.Rules.ProliferateSpellRule"/> for the spell-parser
/// registry but lives in the activated-effect registry so that
/// <see cref="ActivatedAbilityParser.TryParseMultiEffectSentences"/> can resolve the
/// "Proliferate." fragment that appears as the second sentence of Vraska,
/// Betrayal's Sting's 0-ability ("You draw a card and lose 1 life. Proliferate.").
/// </para>
///
/// <para>
/// Priority is set high (999) so this fires before any broader pattern can claim
/// a sentence that starts with "Proliferate".
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class ProliferateActivatedEffectRule : IActivatedEffectRule
{
  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    return trimmed.Equals("Proliferate", StringComparison.OrdinalIgnoreCase)
      ? new ProliferateEffect()
      : null;
  }
}
