namespace MagicAST.Tests;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Keywords;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Tests for the keyword expansion system.
/// </summary>
[TestFixture]
public class KeywordExpanderTests
{
  private KeywordExpander _expander = null!;

  /// <summary>
  /// Helper method to run a test and record its result for ratcheting.
  /// </summary>
  private void RunTest(string testName, Action test)
  {
    bool passed = true;
    Exception? exception = null;

    try
    {
      test();
    }
    catch (Exception ex)
    {
      passed = false;
      exception = ex;
    }

    // Record result in ratchet tracker
    RatchetTestTracker.Instance.RecordResult($"KeywordExpander/{testName}", passed);

    // Rethrow if test failed
    if (!passed && exception != null)
    {
      throw exception;
    }
  }

  [SetUp]
  public void SetUp()
  {
    _expander = KeywordExpander.CreateDefault();
  }

  [Test]
  public void Flying_ExpandsToEvasionEffect()
  {
    RunTest(
      nameof(Flying_ExpandsToEvasionEffect),
      () =>
      {
        var ability = _expander.Expand("Flying");

        Assert.That(ability, Is.InstanceOf<StaticAbility>());
        var staticAbility = (StaticAbility)ability;

        Assert.That(staticAbility.KeywordSource, Is.EqualTo("Flying"));
        Assert.That(staticAbility.Effect, Is.InstanceOf<EvasionEffect>());

        var evasion = (EvasionEffect)staticAbility.Effect;
        Assert.That(evasion.CanBeBlockedBy, Is.Not.Null);
        Assert.That(evasion.CanBeBlockedBy!.Characteristics, Contains.Item("flying"));
        Assert.That(evasion.CanBeBlockedBy!.Characteristics, Contains.Item("reach"));
      }
    );
  }

  [Test]
  public void FirstStrike_ExpandsToCombatDamageTimingEffect()
  {
    RunTest(
      nameof(FirstStrike_ExpandsToCombatDamageTimingEffect),
      () =>
      {
        var ability = _expander.Expand("First strike");

        Assert.That(ability, Is.InstanceOf<StaticAbility>());
        var staticAbility = (StaticAbility)ability;

        Assert.That(staticAbility.KeywordSource, Is.EqualTo("First strike"));
        Assert.That(staticAbility.Effect, Is.InstanceOf<CombatDamageTimingEffect>());

        var timing = (CombatDamageTimingEffect)staticAbility.Effect;
        Assert.That(timing.Timing, Is.EqualTo(CombatDamageTiming.First));
      }
    );
  }

  [Test]
  public void DoubleStrike_ExpandsToCombatDamageTimingBoth()
  {
    RunTest(
      nameof(DoubleStrike_ExpandsToCombatDamageTimingBoth),
      () =>
      {
        var ability = _expander.Expand("Double strike");

        Assert.That(ability, Is.InstanceOf<StaticAbility>());
        var staticAbility = (StaticAbility)ability;

        Assert.That(staticAbility.Effect, Is.InstanceOf<CombatDamageTimingEffect>());

        var timing = (CombatDamageTimingEffect)staticAbility.Effect;
        Assert.That(timing.Timing, Is.EqualTo(CombatDamageTiming.Both));
      }
    );
  }

  [Test]
  public void Lifelink_ExpandsToLifelinkEffect()
  {
    RunTest(
      nameof(Lifelink_ExpandsToLifelinkEffect),
      () =>
      {
        var ability = _expander.Expand("Lifelink");

        Assert.That(ability, Is.InstanceOf<StaticAbility>());
        var staticAbility = (StaticAbility)ability;

        Assert.That(staticAbility.KeywordSource, Is.EqualTo("Lifelink"));
        Assert.That(staticAbility.Effect, Is.InstanceOf<LifelinkEffect>());
      }
    );
  }

  [Test]
  public void Vigilance_ExpandsToVigilanceEffect()
  {
    RunTest(
      nameof(Vigilance_ExpandsToVigilanceEffect),
      () =>
      {
        var ability = _expander.Expand("Vigilance");

        Assert.That(ability, Is.InstanceOf<StaticAbility>());
        var staticAbility = (StaticAbility)ability;

        Assert.That(staticAbility.KeywordSource, Is.EqualTo("Vigilance"));
        Assert.That(staticAbility.Effect, Is.InstanceOf<VigilanceEffect>());
      }
    );
  }

  [Test]
  public void Protection_FromSingleColor_ExpandsCorrectly()
  {
    RunTest(
      nameof(Protection_FromSingleColor_ExpandsCorrectly),
      () =>
      {
        var ability = _expander.Expand("Protection", "red");

        Assert.That(ability, Is.InstanceOf<StaticAbility>());
        var staticAbility = (StaticAbility)ability;

        Assert.That(staticAbility.KeywordSource, Is.EqualTo("Protection"));
        Assert.That(staticAbility.Effect, Is.InstanceOf<ProtectionEffect>());

        var protection = (ProtectionEffect)staticAbility.Effect;
        Assert.That(protection.From, Has.Count.EqualTo(1));
        Assert.That(protection.From[0].Kind, Is.EqualTo(ProtectionQualityKind.Color));
        Assert.That(protection.From[0].Value, Is.EqualTo("red"));
      }
    );
  }

  [Test]
  public void Protection_FromMultipleSubtypes_ExpandsCorrectly()
  {
    RunTest(
      nameof(Protection_FromMultipleSubtypes_ExpandsCorrectly),
      () =>
      {
        var ability = _expander.Expand("Protection", "Demons and from Dragons");

        Assert.That(ability, Is.InstanceOf<StaticAbility>());
        var staticAbility = (StaticAbility)ability;

        Assert.That(staticAbility.Effect, Is.InstanceOf<ProtectionEffect>());

        var protection = (ProtectionEffect)staticAbility.Effect;
        Assert.That(protection.From, Has.Count.EqualTo(2));

        Assert.That(protection.From[0].Kind, Is.EqualTo(ProtectionQualityKind.Subtype));
        Assert.That(protection.From[0].Value, Is.EqualTo("Demons"));

        Assert.That(protection.From[1].Kind, Is.EqualTo(ProtectionQualityKind.Subtype));
        Assert.That(protection.From[1].Value, Is.EqualTo("Dragons"));
      }
    );
  }

  [Test]
  public void Protection_FromEverything_ExpandsCorrectly()
  {
    RunTest(
      nameof(Protection_FromEverything_ExpandsCorrectly),
      () =>
      {
        var ability = _expander.Expand("Protection", "everything");

        var protection = (ProtectionEffect)((StaticAbility)ability).Effect;

        Assert.That(protection.From, Has.Count.EqualTo(1));
        Assert.That(protection.From[0].Kind, Is.EqualTo(ProtectionQualityKind.Everything));
        Assert.That(protection.From[0].Value, Is.Null);
      }
    );
  }

  [Test]
  public void CanExpand_ReturnsTrueForRegisteredKeywords()
  {
    RunTest(
      nameof(CanExpand_ReturnsTrueForRegisteredKeywords),
      () =>
      {
        Assert.That(_expander.CanExpand("Flying"), Is.True);
        Assert.That(_expander.CanExpand("flying"), Is.True); // case insensitive
        Assert.That(_expander.CanExpand("FLYING"), Is.True);
        Assert.That(_expander.CanExpand("Protection"), Is.True);
      }
    );
  }

  [Test]
  public void CanExpand_ReturnsFalseForUnknownKeywords()
  {
    RunTest(
      nameof(CanExpand_ReturnsFalseForUnknownKeywords),
      () =>
      {
        Assert.That(_expander.CanExpand("NotAKeyword"), Is.False);
        Assert.That(_expander.CanExpand("Firebending"), Is.False);
      }
    );
  }

  [Test]
  public void Expand_ThrowsForUnknownKeyword()
  {
    RunTest(
      nameof(Expand_ThrowsForUnknownKeyword),
      () =>
      {
        Assert.Throws<KeyNotFoundException>(() => _expander.Expand("NotAKeyword"));
      }
    );
  }

  [Test]
  public void Expand_ThrowsWhenParameterRequiredButMissing()
  {
    RunTest(
      nameof(Expand_ThrowsWhenParameterRequiredButMissing),
      () =>
      {
        Assert.Throws<ArgumentException>(() => _expander.Expand("Protection"));
      }
    );
  }
}
