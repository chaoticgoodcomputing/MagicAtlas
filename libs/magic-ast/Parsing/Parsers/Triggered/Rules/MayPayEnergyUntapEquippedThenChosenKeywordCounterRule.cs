namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may pay {E}[{E}…]. If you do, untap equipped creature, then put your choice of a
/// [keyword], [keyword], or [keyword] counter on it." — T-45 Power Armor's upkeep reflexive.
///
/// <para>Modelled as an <see cref="OptionalEffect"/> ("you may …") whose
/// <see cref="OptionalEffect.Inner"/> is a <see cref="ConditionalPayEffect"/> carrying the
/// <see cref="PayEnergyCost"/>, and whose <see cref="OptionalEffect.IfYouDo"/> holds the
/// consequent <see cref="CompositeEffect"/> — the same shape
/// <see cref="MayPayEnergyPutCountersAndBecomeSubtypeReflexiveRule"/> (Guide of Souls) produces.
/// The consequent is two flat siblings:
/// <list type="bullet">
///   <item><see cref="UntapEffect"/> on the equipped creature
///   (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>) — "untap equipped creature".</item>
///   <item><see cref="PutChosenKeywordCounterEffect"/> — "put your choice of a menace, trample,
///   or lifelink counter on it": the controller places one counter chosen from the enumerated
///   keyword menu (CR 122.1e). "it" (CR 113.8b) back-references the just-untapped equipped
///   creature.</item>
/// </list></para>
///
/// <para>The keyword menu is parsed via
/// <see cref="MagicAST.Parsing.Parsers.CopyModificationHelpers.TryParseKeywordList"/> after
/// normalising the "…, or …" disjunction to the comma-list form that helper splits; the rule
/// bails (no free text) if any option is not a recognised keyword.</para>
///
/// <para>ANCHORED (^…$).</para>
/// </summary>
[TriggeredRule(Priority = 64)]
public sealed class MayPayEnergyUntapEquippedThenChosenKeywordCounterRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<energy>(?:\{E\}\s*)+)\.\s*If\s+you\s+do,\s*"
      + @"untap\s+equipped\s+creature,\s*then\s+put\s+your\s+choice\s+of\s+an?\s+"
      + @"(?<opts>.+?)\s+counter\s+on\s+it\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _energySymbol = new(@"\{E\}", RegexOptions.Compiled);

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var energyCount = _energySymbol.Matches(m.Groups["energy"].Value).Count;
    if (energyCount <= 0)
    {
      return false;
    }

    // "menace, trample, or lifelink" — normalise the disjunction to the comma-list the
    // keyword-list splitter understands, then require every option to be a real keyword.
    var normalisedOptions = m.Groups["opts"].Value
      .Replace(", or ", ", ")
      .Replace(" or ", ", ");
    var options = CopyModificationHelpers.TryParseKeywordList(normalisedOptions);
    if (options is null || options.Count == 0)
    {
      return false;
    }

    var consequent = new CompositeEffect
    {
      Effects = new List<Effect>
      {
        new UntapEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
        },
        new PutChosenKeywordCounterEffect
        {
          Target = ObjectReference.It(),
          Options = options,
          Count = LiteralQuantity.Of(1),
        },
      },
    };

    effect = EffectWrap.Optional(
      new ConditionalPayEffect { Cost = new PayEnergyCost { Amount = LiteralQuantity.Of(energyCount) } },
      isOptional: true,
      ifYouDo: consequent
    );
    return true;
  }
}
