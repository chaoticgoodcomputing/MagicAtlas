namespace MagicAST.Tests.Tests;

using MagicAST.AST.Costs;
using MagicAST.Parsing.Parsers.Activated.Rules;

/// <summary>
/// <see cref="SacrificeCostRule"/> — the fodder filter for a "Sacrifice [X]" activated-ability cost.
/// "Another X" excludes the source (<c>ExcludeSelf</c>, CR 701.21 — a cost requiring another permanent
/// can't be paid by the source itself), a distinction that matters for self-sac loops; and
/// "permanent"/"enchantment" are card types, not subtypes. Regression for the Pious Evangel
/// "Sacrifice another permanent" projection bug, where "another" leaked into the subject slot
/// (<c>sac:another:controlled</c>), dropping both "permanent" and the exclusion.
/// </summary>
[TestFixture]
public class SacrificePatternTests
{
  [TestCase("Sacrifice another permanent", "permanent", true)]
  [TestCase("Sacrifice another creature", "creature", true)]
  [TestCase("Sacrifice another artifact", "artifact", true)]
  [TestCase("Sacrifice another enchantment", "enchantment", true)]
  [TestCase("Sacrifice another land", "land", true)]
  [TestCase("Sacrifice a creature", "creature", false)]
  [TestCase("Sacrifice a permanent", "permanent", false)]
  [TestCase("Sacrifice an artifact", "artifact", false)]
  [TestCase("Sacrifice an enchantment", "enchantment", false)]
  public void Sacrifice_fodder_carries_card_type_and_the_another_exclusion(
    string cost,
    string cardType,
    bool excludeSelf
  )
  {
    var parsed = new SacrificeCostRule().TryMatch(cost);
    Assert.That(parsed, Is.InstanceOf<SacrificeCost>());
    var filter = ((SacrificeCost)parsed!).Filter;
    Assert.That(filter, Is.Not.Null);
    Assert.That(filter!.CardTypes, Is.EqualTo(new[] { cardType }));
    Assert.That(filter.ExcludeSelf ?? false, Is.EqualTo(excludeSelf));
  }

  // Self-binding for COSTS (the dual of the §6 trigger self-bind): "Sacrifice this [type]" / a bare
  // self-name is the source itself → IsSelf=true, migrating the free-text Characteristic.Other("this
  // permanent") marker to structure. This is what lets the interaction engine know a created token can
  // never refuel a self-sacrifice (ADR-0002 §8 "A" — a self-sac source is consumed once). "a/an/another
  // X" stay non-self.
  [TestCase("Sacrifice this creature", "creature")]
  [TestCase("Sacrifice this artifact", "artifact")]
  [TestCase("Sacrifice this enchantment", "enchantment")]
  [TestCase("Sacrifice this land", "land")]
  public void Sacrifice_this_self_reference_binds_is_self(string cost, string cardType)
  {
    var filter = ((SacrificeCost)new SacrificeCostRule().TryMatch(cost)!).Filter;
    Assert.That(filter, Is.Not.Null);
    Assert.That(filter!.IsSelf, Is.True, "a 'Sacrifice this' cost is the source itself");
    Assert.That(filter.CardTypes, Is.EqualTo(new[] { cardType }));
    Assert.That(
      filter.Characteristics ?? [],
      Is.Empty,
      "the free-text 'this permanent' marker is migrated to the structured IsSelf"
    );
    Assert.That(filter.ExcludeSelf ?? false, Is.False);
  }

  [Test]
  public void Sacrifice_by_name_binds_is_self()
  {
    var filter = ((SacrificeCost)new SacrificeCostRule().TryMatch("Sacrifice Denethor")!).Filter;
    Assert.That(filter!.IsSelf, Is.True, "a bare self-name cost is the source itself");
    Assert.That(filter.Characteristics ?? [], Is.Empty);
  }

  [TestCase("Sacrifice a creature")]
  [TestCase("Sacrifice another creature")]
  [TestCase("Sacrifice a Saproling")]
  public void Sacrifice_non_self_fodder_does_not_bind_is_self(string cost)
  {
    var filter = ((SacrificeCost)new SacrificeCostRule().TryMatch(cost)!).Filter;
    Assert.That(filter!.IsSelf ?? false, Is.False, "typed/another fodder is not the source");
  }
}
