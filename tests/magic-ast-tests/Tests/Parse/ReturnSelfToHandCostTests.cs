namespace MagicAST.Tests.Tests;

using MagicAST.AST.Costs;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Activated.Rules;

/// <summary>
/// <see cref="ReturnSelfToHandCostRule"/> — "Return this [permanent] to its owner's hand" in the COST
/// position (before the colon) is a self-bounce activation cost (Grinning Ignus, Recurring Nightmare),
/// not an effect. Previously dropped silently, leaving the ability's costs incomplete.
/// </summary>
[TestFixture]
public class ReturnSelfToHandCostTests
{
  [TestCase("Return this creature to its owner's hand")]
  [TestCase("Return this enchantment to its owner's hand")]
  [TestCase("Return this Aura to its owner's hand")]
  [TestCase("Return this land to its owner's hand")]
  public void Return_self_to_hand_is_a_cost(string costText)
  {
    var cost = new ReturnSelfToHandCostRule().TryMatch(costText);
    Assert.That(cost, Is.InstanceOf<ReturnToHandCost>());
    Assert.That(
      ((ReturnToHandCost)cost!).Target.Kind,
      Is.EqualTo(ObjectReferenceKind.Self),
      "a self-bounce returns the source itself"
    );
  }

  // It must NOT swallow a return of a different object (those are target/effect returns).
  [TestCase("Return target creature to its owner's hand")]
  [TestCase("Return a creature you control to its owner's hand")]
  public void Return_of_another_object_is_not_a_self_bounce_cost(string costText)
  {
    Assert.That(new ReturnSelfToHandCostRule().TryMatch(costText), Is.Null);
  }
}
