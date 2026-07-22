namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target creature becomes a Treasure artifact with &quot;{T}, Sacrifice this artifact:
/// Add one mana of any color&quot; and loses all other card types and abilities." —
/// Vraska, Betrayal's Sting's −2 loyalty ability.
///
/// <para>
/// A single continuous-effect sentence that applies four simultaneous layer-4 changes
/// (CR 613.1d) and a layer-6 ability grant (CR 613.1f) to the target creature:
/// <list type="bullet">
///   <item><see cref="SetCardTypesEffect"/> — sets the target's card types to
///   <c>["artifact"]</c>, implementing "becomes … artifact … and loses all other
///   card types".</item>
///   <item><see cref="ChangeSubtypeEffect"/> — sets the target's artifact subtypes
///   to <c>["Treasure"]</c>, implementing "becomes a Treasure …".</item>
///   <item><see cref="GainAbilityEffect"/> — grants the Treasure mana ability
///   <c>{T}, Sacrifice this artifact: Add one mana of any color</c> as a
///   nested <see cref="ActivatedAbility"/>.</item>
///   <item><see cref="LoseAbilityEffect"/> — the "loses all other … abilities"
///   clause, structured as <see cref="AbilityScope.AllOther"/> (the granted Treasure
///   mana ability survives the strip; CR 613.1f).</item>
/// </list>
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the phrase "becomes a Treasure artifact" does not recur
/// as a substring of any other sibling rule, but anchoring prevents future broad
/// patterns from consuming it silently.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the four effects sit as
/// flat siblings on <c>Effects</c>. <see cref="TryMatch"/> always returns null.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 953)]
public sealed class BecomesTreasureArtifactEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // Anchored pattern: "Target creature becomes a Treasure artifact with "..." and loses all other card types and abilities"
  // Oracle text uses curly-quote “ / ” around the nested ability text.
  // The character class [“”"] accepts both Unicode curly quotes and plain ASCII double quotes.
  private static readonly Regex Pattern = new(
    "^Target\\s+creature\\s+becomes\\s+a\\s+Treasure\\s+artifact\\s+with\\s+[“”\"](?<ability>[^“”\"]+)[“”\"]\\s+and\\s+loses\\s+all\\s+other\\s+card\\s+types\\s+and\\s+abilities$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape always produces multiple sibling effects.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = ["creature"] },
    };

    // "It" references the target creature throughout.
    var it = new ObjectReference { Kind = ObjectReferenceKind.It };

    // 1. Set card types to ["artifact"] — implements "becomes … artifact … and loses all other card types".
    var setCardTypes = new SetCardTypesEffect
    {
      Subject = target,
      CardTypes = ["artifact"],
    };

    // 2. Change subtypes to ["Treasure"] — implements "becomes a Treasure …".
    var changeSubtype = new ChangeSubtypeEffect
    {
      Target = it,
      Subtypes = ["Treasure"],
    };

    // 3. Gain the Treasure mana ability: {T}, Sacrifice this artifact: Add one mana of any color.
    var treasureActivatedAbility = new ActivatedAbility
    {
      Costs =
      [
        new TapCost(),
        new SacrificeCost
        {
          Filter = new ObjectFilter { CardTypes = ["artifact"], IsSelf = true },
          Quantity = LiteralQuantity.Of(1),
        },
      ],
      Effects =
      [
        new AddManaEffect
        {
          Mana = string.Empty,
          AnyColor = true,
        },
      ],
      IsManaAbility = true,
    };

    var gainAbility = new GainAbilityEffect
    {
      Target = it,
      GainedAbility = treasureActivatedAbility,
    };

    // 4. Lose all other abilities — an unbounded scope determiner (CR 613.1f), not a
    //    named ability. Structured as AbilityScope.AllOther (the granted Treasure mana
    //    ability from effect 3 survives the strip).
    var loseAbilities = new LoseAbilityEffect
    {
      Target = it,
      Scope = AbilityScope.AllOther,
    };

    effects = new List<Effect>
    {
      setCardTypes,
      changeSubtype,
      gainAbility,
      loseAbilities,
    };
    return true;
  }
}
