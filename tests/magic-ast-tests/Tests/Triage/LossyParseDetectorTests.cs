using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage;

namespace MagicAtlas.Ast.Tests.Tests.Triage;

/// <summary>
/// Guards <see cref="LossyParseAnalyzer"/> — the detector for lossy-but-clean
/// parses (structure dropped WITHOUT an UnparsedAbility) that the triage's per-line
/// diagnostics are blind to. Drives the REAL parser so the signal is validated
/// against actual AST output, not a hand-built stand-in.
/// </summary>
[TestFixture]
public class LossyParseDetectorTests
{
  private static LossyParseAnalyzer.LossySignal AnalyzeCard(string oracle)
  {
    var result = new OracleParser().Parse(oracle);
    return LossyParseAnalyzer.Analyze(oracle, result.Output.Abilities);
  }

  [Test]
  public void Keranos_line3_trigger_collapse_is_flagged_lossy()
  {
    // Keranos's third line collapses two "Whenever … this way" triggers plus a
    // reveal static into a single bare dealDamage spell — the motivating case.
    const string keranos =
      "Indestructible\n"
      + "As long as your devotion to blue and red is less than seven, Keranos isn't a creature.\n"
      + "Reveal the first card you draw on each of your turns. Whenever you reveal a land card this way, draw a card. Whenever you reveal a nonland card this way, Keranos deals 3 damage to any target.";

    var signal = AnalyzeCard(keranos);

    Assert.That(signal.SuspectedLossy, Is.True, "two 'Whenever' triggers were dropped");
    Assert.That(signal.TriggerOpeners, Is.GreaterThanOrEqualTo(2));
    Assert.That(signal.DroppedTriggers, Is.GreaterThanOrEqualTo(1));
  }

  [Test]
  public void Faithful_multi_trigger_card_is_not_flagged()
  {
    // Two ordinary triggered abilities that the parser models as two triggers —
    // openers and produced trigger nodes balance, so no deficit.
    const string faithful =
      "Whenever a creature you control dies, you gain 1 life.\n"
      + "Whenever you gain life, draw a card.";

    var signal = AnalyzeCard(faithful);

    Assert.That(signal.TriggerOpeners, Is.EqualTo(2));
    Assert.That(
      signal.SuspectedLossy,
      Is.False,
      $"balanced triggers must not flag (openers={signal.TriggerOpeners}, nodes={signal.TriggeredNodes})"
    );
  }

  [Test]
  public void No_trigger_card_is_not_flagged()
  {
    var signal = AnalyzeCard("Untap target artifact creature.");
    Assert.That(signal.TriggerOpeners, Is.EqualTo(0));
    Assert.That(signal.SuspectedLossy, Is.False);
  }

  [Test]
  public void Reminder_text_triggers_do_not_create_a_phantom_deficit()
  {
    // A keyword whose reminder text contains "When…" but which produces no
    // triggered ability for that reminder — stripping parentheticals must keep
    // the deficit at zero.
    var signal = AnalyzeCard("Flying");
    Assert.That(signal.SuspectedLossy, Is.False);
    // Reminder-style parenthetical with a "When" opener must be stripped.
    var withReminder = AnalyzeCard("Vigilance (Attacking doesn't cause this to tap.)");
    Assert.That(withReminder.SuspectedLossy, Is.False);
  }
}
