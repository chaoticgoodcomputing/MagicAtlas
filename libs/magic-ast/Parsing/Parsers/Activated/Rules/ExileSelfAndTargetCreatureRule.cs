namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile this creature and target creature without [keyword] that's attacking you." —
/// a single activated-ability sentence whose one verb ("Exile") governs TWO objects: the
/// source object itself ("this creature") and a targeted attacker. Giant Trap Door
/// Spider: "{1}{R}{G}, {T}: Exile this creature and target creature without flying that's
/// attacking you." The cost half ("{1}{R}{G}, {T}") is parsed by the activated cost rules;
/// this rule recognises only the post-colon effect fragment.
///
/// <para>
/// Because the two objects are exiled by one action, the sentence expands to a flat pair of
/// sibling <see cref="ExileEffect"/> nodes on <c>Effects</c> — modelled via
/// <see cref="IMultiActivatedEffectRule.TryMatchMulti"/>, the same "flat sibling list"
/// convention as <see cref="ExileAnotherCreatureThenReturnRule"/> and
/// <see cref="ExileSelfThenReturnToBattlefieldRule"/>. <see cref="TryMatch"/> always returns
/// null so the single-effect path never claims this sentence and silently drops one exile.
/// </para>
///
/// <para>
/// The first exile targets <see cref="ObjectReferenceKind.Self"/> ("this creature", CR 109).
/// The second exiles a <see cref="ObjectReferenceKind.Target"/> creature carrying two
/// characteristic predicates. The keyword-absence "without flying" routes to the
/// first-class <see cref="ObjectFilter.LacksKeywords"/> axis for a recognised keyword
/// (the M10/Falter · ODY/Ashen Firebeast convention; unrecognised keywords keep the
/// honest "withoutX" free-text fallback). The combat-defender predicate "that's attacking
/// you" has no first-class <see cref="ObjectFilter"/> home yet, so it still rides as a
/// typed <see cref="OtherCharacteristic"/> residual (IResidual, a deliberate scope deferral
/// per ADR 0001 — NOT IUnparsed): "attackingYou" — the object is attacking the ability's
/// controller; there is no <c>CombatStateCharacteristic</c> defender axis, so the predicate
/// is deferred whole rather than split into a structured "attacking" plus a dangling "you".
/// </para>
///
/// CR 701.13a (verbatim): "To exile an object, move it to the exile zone from wherever it is."
/// CR 602.1: "Activated abilities have a cost and an effect." CR 508.1b: the active player
/// "announces which player, planeswalker, or battle each of the chosen creatures is attacking"
/// — "attacking you" is the subset of attackers whose declared defender is this controller.
/// </summary>
[ActivatedEffectRule(Priority = 947)]
public sealed class ExileSelfAndTargetCreatureRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // Anchored to the exact family template so a broader "Exile this creature and target
  // creature …" sibling with a different filter cannot be mis-claimed (it falls through
  // unchanged). The keyword is captured so "without flying" / "without reach" / … all work;
  // the apostrophe accepts both ASCII (U+0027) and curly (U+2019) so corpus typography variance
  // never breaks the match.
  private static readonly Regex Pattern = new(
    @"^Exile\s+this\s+creature\s+and\s+target\s+creature\s+without\s+(?<keyword>[A-Za-z]+)\s+that['’]s\s+attacking\s+you$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always null — this shape always yields two sibling exiles, served only via
  /// <see cref="TryMatchMulti"/>.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var match = Pattern.Match(effectText.Trim().TrimEnd('.').Trim());
    if (!match.Success)
    {
      return false;
    }

    var keyword = match.Groups["keyword"].Value.ToLowerInvariant();
    var lacksKeywords = Enum.TryParse<KeywordAbility>(keyword, ignoreCase: true, out var kw)
      ? new List<KeywordAbility> { kw }
      : null;
    var characteristics = new List<Characteristic> { Characteristic.Other("attackingYou") };
    if (lacksKeywords is null)
    {
      characteristics.Insert(
        0,
        Characteristic.Other($"without{char.ToUpperInvariant(keyword[0])}{keyword[1..]}")
      );
    }

    effects = new List<Effect>
    {
      new ExileEffect { Target = ObjectReference.Self() },
      new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            LacksKeywords = lacksKeywords,
            Characteristics = characteristics,
          },
        },
      },
    };
    return true;
  }
}
