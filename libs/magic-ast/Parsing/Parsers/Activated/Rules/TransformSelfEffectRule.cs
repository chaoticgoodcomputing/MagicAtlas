namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Transform this creature" / "Transform this permanent" — the activated
/// transform-self ability on the front face of a double-faced card
/// (e.g. "{6}: Transform this creature.").
///
/// <para>
/// CR 701.27a: "To transform a permanent, turn it over so that its other face
/// is up. Only permanents represented by double-faced tokens and double-faced
/// cards can transform. (See rule 712, 'Double-Faced Cards.')"
/// </para>
///
/// <para>
/// Maps to a <see cref="TransformEffect"/> with a <see cref="ObjectReference.Self"/>
/// subject. The activation cost (including Phyrexian hybrid mana such as
/// {B/P} — CR 107.4f) is parsed upstream by the cost-component rules, the
/// "Activate only as a sorcery" restriction is lifted by
/// <c>ActivatedAbilityParser.ExtractActivationRestrictions</c>, and the
/// parenthetical reminder explaining Phyrexian mana is stripped by
/// <c>ActivatedAbilityParser.StripTrailingReminder</c> before this rule fires.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 970)]
public sealed class TransformSelfEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Transform\s+this\s+(creature|permanent)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new TransformEffect { Target = ObjectReference.Self() };
  }
}
