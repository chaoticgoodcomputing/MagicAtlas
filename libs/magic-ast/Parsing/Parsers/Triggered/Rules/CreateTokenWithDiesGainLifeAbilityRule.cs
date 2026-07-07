namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "create a [P/T] [colors] [Subtype] creature token with \"When this token dies, you
/// gain N life.\"" — the Strixhaven Pest token (Pest Rescuer, Sedgemoor Witch, Callous
/// Bloodmage, …), created here inside an upkeep trigger.
///
/// <para>
/// The generic <see cref="CreateTokenRule"/> parses the token's power/toughness, colors,
/// subtypes and types but drops the quoted <i>triggered</i> ability (its
/// <c>TriggeredRuleHelpers.ParseTokenAbilities</c> only recognises bare keywords). This
/// dedicated rule reuses the same base-token helpers so the token's characteristics are
/// identical, then attaches the quoted ability as a structured
/// <see cref="TriggeredAbility"/>: "When this token dies" is a self-referential
/// <see cref="TriggerEvent.Dies"/> trigger (<see cref="ObjectFilter.IsSelf"/> = true), and
/// "you gain N life" is a <see cref="GainLifeEffect"/> for you (CR 119.3 — "If an effect
/// causes a player to gain life or lose life, that player's life total is adjusted
/// accordingly."). The additive life amount N is carried as a typed
/// <see cref="LiteralQuantity"/>.
/// </para>
///
/// <para>
/// Runs at priority 60, above the generic <see cref="CreateTokenRule"/> (default 50), so
/// this specific shape is matched first and the generic rule never overwrites the token's
/// structured ability with an empty ability list (mirrors
/// <see cref="CreateColorlessTokenWithSacrificeManaAbilityRule"/>). Anchored on the exact
/// quoted ability and the singular "create a [P/T]" form, so plural / "twice X" token
/// counts (which the base helpers would miscount) fall through to
/// <see cref="CreateTokenRule"/> unchanged.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class CreateTokenWithDiesGainLifeAbilityRule : ITriggeredRule
{
  // Singular "create a [P]/[T] ..." only — excludes "create two ...", "create twice X ..."
  // whose non-unit count the base helpers do not parse.
  private static readonly Regex _singularCreate = new(
    @"^create\s+a\s+\d+/\d+\s",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // The quoted granted ability: "When this token dies, you gain N life."
  // \x22 = ASCII double-quote; “/” = Unicode curly quotes. The additive amount is captured.
  private static readonly Regex _diesGainLife = new(
    @"creature\s+token\s+with\s+[\x22“]When\s+this\s+token\s+dies,\s+you\s+gain\s+(?<life>\d+)\s+life\.[\x22”]",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    if (!_singularCreate.IsMatch(trimmed))
    {
      return false;
    }

    var abilityMatch = _diesGainLife.Match(trimmed);
    if (!abilityMatch.Success)
    {
      return false;
    }

    // Reuse the shared base-token helpers so the token's characteristics match exactly
    // what CreateTokenRule would emit for this clause.
    var powerToughness = TriggeredRuleHelpers.ParsePowerToughness(trimmed);
    if (powerToughness == null)
    {
      return false;
    }

    var subtypes = TriggeredRuleHelpers.ParseCreatureSubtypes(trimmed);
    if (subtypes.Count == 0)
    {
      return false;
    }

    var colors = TriggeredRuleHelpers.ParseColors(trimmed);
    var types = TriggeredRuleHelpers.ParseTokenTypes(trimmed);

    var life = int.Parse(
      abilityMatch.Groups["life"].Value,
      System.Globalization.CultureInfo.InvariantCulture
    );

    // "When this token dies, you gain N life." — a self-referential Dies trigger
    // whose effect gains you N life (CR 119.3 — "If an effect causes a player to gain
    // life or lose life, that player's life total is adjusted accordingly.").
    var tokenAbility = new TriggeredAbility
    {
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Dies,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          IsSelf = true,
        },
      },
      Effects =
      [
        new GainLifeEffect
        {
          Amount = LiteralQuantity.Of(life),
          Player = ObjectReference.You(),
        },
      ],
    };

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = new TokenDefinition
      {
        Power = powerToughness.Value.Power,
        Toughness = powerToughness.Value.Toughness,
        Colors = colors,
        Types = types,
        Subtypes = subtypes,
        Abilities = [tokenAbility],
        IsCopy = false,
      },
    };
    return true;
  }
}
