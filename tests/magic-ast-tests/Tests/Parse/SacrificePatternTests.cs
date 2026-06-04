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
}
