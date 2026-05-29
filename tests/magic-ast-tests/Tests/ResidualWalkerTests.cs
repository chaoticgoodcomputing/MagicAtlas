namespace MagicAST.Tests.Tests;

using System.Linq;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;
using MagicAST.Analysis;

/// <summary>
/// Tests for the residual-debt walker (ADR 0001 forcing-function): it must find
/// <see cref="IResidual"/> nodes and <see cref="FreeTextFieldAttribute"/> fields
/// anywhere in the AST, and must NOT count structured arms.
/// </summary>
[TestFixture]
public class ResidualWalkerTests
{
  [Test]
  public void Separates_Unparsed_Failures_From_Residual_FreeText_Across_Nesting()
  {
    var oracle = new CardOracle
    {
      RawText = "x",
      Abilities = new Ability[]
      {
        new SpellAbility
        {
          Effects = new Effect[]
          {
            new UnparsedEffect { SourceSpan = new TextSpan(0, 1), RawText = "blah" },
          },
          Instructions = new[] { "if you control a creature" },
        },
      },
    };

    var debt = ResidualWalker.Analyze(oracle);

    // UnparsedEffect is a parse FAILURE, not deferred-structure residual.
    Assert.That(debt.Unparsed.GetValueOrDefault("UnparsedEffect"), Is.EqualTo(1));
    Assert.That(debt.Residuals.ContainsKey("UnparsedEffect"), Is.False);
    // The free-text Instructions field is residual debt.
    Assert.That(debt.Residuals.GetValueOrDefault("SpellAbility.Instructions"), Is.EqualTo(1));
    Assert.That(debt.Residuals.Values.Sum(), Is.EqualTo(1));
  }

  [Test]
  public void Counts_OtherCharacteristic_But_Not_KeywordCharacteristic()
  {
    var filter = new ObjectFilter
    {
      CardTypes = new[] { "creature" },
      Characteristics = new Characteristic[]
      {
        Characteristic.HasKeyword(KeywordAbility.Flying),
        Characteristic.Other("tapped"),
      },
    };

    var counts = ResidualWalker.Count(filter);

    Assert.That(counts.GetValueOrDefault("OtherCharacteristic"), Is.EqualTo(1));
    Assert.That(counts.ContainsKey("KeywordCharacteristic"), Is.False);
  }
}
