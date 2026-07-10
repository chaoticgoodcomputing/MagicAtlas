namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[you may] have its controller create [article] [P/T] [color] [Subtype] creature token
/// that's tapped and attacking" — Najeela, the Blade-Blossom's Warrior-attacks trigger.
/// The token's creator (and therefore controller — CR 111.2) is "its controller": the
/// controller of the attacking creature named by the trigger's <see cref="AST.Triggers.TriggerCondition"/>
/// filter, modelled as <see cref="ObjectReferenceKind.Controller"/> ("its controller"). The
/// token enters both tapped and attacking (CR 508.4 — an effect can put a creature onto the
/// battlefield attacking), recorded on <see cref="TokenDefinition.EntersTapped"/> /
/// <see cref="TokenDefinition.EntersAttacking"/>.
///
/// <para>
/// Distinct from the generic <see cref="CreateTokenRule"/>, which always sets the creator to
/// <see cref="ObjectReference.You"/> and drops the "tapped and attacking" entry modifiers.
/// Priority 60 (above the default-50 <see cref="CreateTokenRule"/>) so the "have its controller
/// create … tapped and attacking" shape is claimed here first. Anchored on the required
/// "have its controller create" head and "tapped and attacking" tail.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class HaveControllerCreateTappedAttackingTokenRule : ITriggeredRule
{
  // Optional "you may " (CR 116.1b) then the mandatory "have its controller create" head.
  private static readonly Regex _headPattern = new(
    @"^(?<optional>you\s+may\s+)?have\s+its\s+controller\s+create\s+(?<token>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Tail modifier: "that's tapped and attacking" (order-tolerant on the two states).
  private static readonly Regex _tappedAndAttacking = new(
    @"that'?s\s+tapped\s+and\s+attacking",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var head = _headPattern.Match(text.Trim());
    if (!head.Success)
    {
      return false;
    }

    if (!_tappedAndAttacking.IsMatch(text))
    {
      return false;
    }

    var tokenText = head.Groups["token"].Value;

    var powerToughness = TriggeredRuleHelpers.ParsePowerToughness(tokenText);
    if (powerToughness is null)
    {
      return false;
    }

    var subtypes = TriggeredRuleHelpers.ParseCreatureSubtypes(tokenText);
    if (subtypes.Count == 0)
    {
      return false;
    }

    var colors = TriggeredRuleHelpers.ParseColors(tokenText);
    var types = TriggeredRuleHelpers.ParseTokenTypes(tokenText);
    var (_, count) = TriggeredRuleHelpers.ParseArticle(tokenText);
    var isOptional = head.Groups["optional"].Success;

    var create = new CreateTokenEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      Count = LiteralQuantity.Of(count),
      Token = new TokenDefinition
      {
        Power = powerToughness.Value.Power,
        Toughness = powerToughness.Value.Toughness,
        Colors = colors,
        Types = types,
        Subtypes = subtypes,
        EntersTapped = true,
        EntersAttacking = true,
      },
    };

    effect = EffectWrap.Optional(create, isOptional);
    return true;
  }
}
