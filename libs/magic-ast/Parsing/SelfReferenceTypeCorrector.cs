namespace MagicAST.Parsing;

using System;
using System.Collections.Generic;
using System.Linq;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Corrects the card type on a SELF-BY-NAME trigger reference once the type line is known.
///
/// <para>
/// The oracle parser resolves a self-by-name reference ("When The One Ring enters", "When Elenda
/// dies") via <see cref="Parsers.Triggered.TriggeredRuleHelpers.IsSelfByNameTrigger"/>, which
/// hardcodes <c>CardTypes:["creature"]</c> because the parser has no access to the card's type line
/// at that layer. That is correct for the common case (a creature naming itself) but mislabels a
/// NON-creature self-reference — e.g. The One Ring (a pure Artifact) gets a wrong
/// <c>{creature, IsSelf}</c> ETB filter.
/// </para>
///
/// <para>
/// CR 201.5: text that refers to the object it's on by name means just that particular object. The
/// reference is to the SOURCE object whatever its type — so the type annotation should be the card's
/// actual type, not a hardcoded "creature". This corrector runs in <see cref="CardParser"/>, where
/// the parsed type line is available, and retypes any self-by-name-derived <c>{["creature"], IsSelf}</c>
/// trigger filter to the card's actual types — but ONLY when the card is not itself a creature (a
/// creature/artifact-creature keeps <c>["creature"]</c>, so every existing creature self-by-name gold
/// is untouched; this is gold-regression-safe).
/// </para>
/// </summary>
public static class SelfReferenceTypeCorrector
{
  public static CardOracle Correct(CardOracle oracle, TypeLineAST typeLine)
  {
    var types = typeLine.Types;
    // If the card has no parsed types, or IS a creature, the hardcoded "creature" self filter needs
    // no correction (the creature case is already right; the empty case has nothing to retype to).
    if (
      types.Count == 0
      || types.Any(t => t.Equals("creature", StringComparison.OrdinalIgnoreCase))
    )
    {
      return oracle;
    }

    var selfTypes = types.Select(t => t.ToLowerInvariant()).ToList();
    var corrected = oracle.Abilities.Select(a => CorrectAbility(a, selfTypes)).ToList();
    return oracle with { Abilities = corrected };
  }

  private static Ability CorrectAbility(Ability ability, IReadOnlyList<string> selfTypes)
  {
    if (ability is not TriggeredAbility triggered)
    {
      return ability;
    }

    var trigger = CorrectTrigger(triggered.Trigger, selfTypes);
    var additional =
      triggered.AdditionalTrigger is null
        ? null
        : CorrectTrigger(triggered.AdditionalTrigger, selfTypes);

    if (
      ReferenceEquals(trigger, triggered.Trigger)
      && ReferenceEquals(additional, triggered.AdditionalTrigger)
    )
    {
      return ability;
    }

    return triggered with { Trigger = trigger, AdditionalTrigger = additional };
  }

  private static TriggerCondition CorrectTrigger(
    TriggerCondition trigger,
    IReadOnlyList<string> selfTypes
  )
  {
    // Only a self-by-name-derived filter — exactly {CardTypes:["creature"], IsSelf:true} — is
    // retyped. A genuine "a creature" reference (IsSelf null/false) or a multi-type filter is left
    // untouched.
    if (trigger.Filter is { IsSelf: true, CardTypes: ["creature"] } filter)
    {
      return trigger with { Filter = filter with { CardTypes = selfTypes } };
    }

    return trigger;
  }
}
