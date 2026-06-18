namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.References;

/// <summary>
/// "Choose a counter on target permanent. Put an additional counter of that kind on that permanent."
/// — Ichormoon Gauntlet's triggered ability effect.
///
/// <para>
/// ANCHOR: fully anchored (^...$) over the combined two-sentence effect so it cannot match as a
/// substring of other triggered effect text. The two-sentence structure is matched as a unit
/// because splitting them by ". " would lose the "of that kind" anaphor linking the second
/// sentence to the choice made in the first.
/// </para>
///
/// <para>
/// CR 122.1: counters are placed on permanents; the chosen kind must already be present.
/// This produces a <see cref="PutAdditionalCounterOfChosenKindEffect"/> whose <c>Target</c>
/// is the "target permanent" reference established by "choose a counter on target permanent".
/// </para>
///
/// <para>
/// Priority 999: fires before generic put-counter rules to avoid partial matches.
/// </para>
/// </summary>
[TriggeredRule(Priority = 999)]
public sealed class ChooseAndPutAdditionalCounterOfChosenKindRule : ITriggeredRule
{
  // Full two-sentence effect, anchored. Tolerates minor whitespace/period variation.
  // "choose a counter on target permanent" then "put an additional counter of that kind on that permanent"
  private static readonly Regex _pattern = new(
    @"^[Cc]hoose\s+a\s+counter\s+on\s+target\s+permanent\.\s+[Pp]ut\s+an\s+additional\s+counter\s+of\s+that\s+kind\s+on\s+that\s+permanent\.?$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new PutAdditionalCounterOfChosenKindEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target },
    };
    return true;
  }
}
