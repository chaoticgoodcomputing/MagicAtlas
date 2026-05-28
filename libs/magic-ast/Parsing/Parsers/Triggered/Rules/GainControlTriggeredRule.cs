namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// "gain control of target [filter] [until end of turn]" —
/// single-target control-change on the triggered side (ETB, dies, upkeep, etc.).
///
/// Delegates single-noun filter parsing to <see cref="SpellRuleHelpers.ParseTargetFilter"/>.
/// Also handles the controller qualifier "an opponent controls" after the filter phrase,
/// which is the canonical way oracle text restricts steal effects to opposing permanents
/// (Rule 109.5 — control relationship).
///
/// Duration is optional:
/// <list type="bullet">
///   <item>"until end of turn" → <see cref="UntilEndOfTurnDuration"/></item>
///   <item>No duration clause → <see langword="null"/> (permanent control change)</item>
/// </list>
///
/// The optional "you may" prefix sets <see cref="GainControlEffect.IsOptional"/> = <see langword="true"/>.
///
/// Recognized filter nouns (via <see cref="SpellRuleHelpers.ParseTargetFilter"/>):
/// creature, permanent, artifact, enchantment, planeswalker, land, and richer filter
/// phrases (color + type, non- prefix, etc.).
///
/// Rule 701.3 (control-changing effects) + Rule 115.1 (target).
/// </summary>
[TriggeredRule]
public sealed class GainControlTriggeredRule : ITriggeredRule
{
  // Optional "you may " prefix, followed by "gain control of target <filter>"
  // with an optional "an opponent controls" qualifier and an optional "until end of turn" clause.
  // The filter is captured greedily up to the first occurrence of either:
  //   - " an opponent controls"
  //   - " until end of turn"
  //   - end of the match
  // Sentence-boundary lookahead (a literal period or end-of-string) lets this rule match
  // when the effect-part begins with this sentence but is followed by additional sentences.
  private static readonly Regex Pattern = new(
    @"^(?:you\s+may\s+)?gain\s+control\s+of\s+target\s+(?<filter>(?:(?!an\s+opponent\s+controls|until\s+end\s+of\s+turn).)+?)\s*(?:an\s+opponent\s+controls\s*)?(?:until\s+end\s+of\s+turn\s*)?(?:[.!]|$)",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // Normalise: trim outer whitespace but do NOT strip the trailing period here —
    // the pattern uses (?:[.!]|$) to anchor the first-sentence boundary, so the
    // period must be present for multi-sentence inputs. For single-sentence input
    // (no period) the $ alternative fires.
    var trimmed = text.Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var isOptional = Regex.IsMatch(trimmed, @"^\s*you\s+may\b", RegexOptions.IgnoreCase);
    var filterPhrase = m.Groups["filter"].Value.Trim().TrimEnd('.');

    // Detect "an opponent controls" qualifier.
    var hasOpponentController = Regex.IsMatch(
      trimmed,
      @"\ban\s+opponent\s+controls\b",
      RegexOptions.IgnoreCase
    );

    // Detect "until end of turn" duration.
    var hasUntilEndOfTurn = Regex.IsMatch(
      trimmed,
      @"\buntil\s+end\s+of\s+turn\b",
      RegexOptions.IgnoreCase
    );

    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return false;
    }

    // Apply opponent controller qualifier if present.
    if (hasOpponentController)
    {
      filter = filter with { Controller = ControllerFilter.Opponent };
    }

    Duration? duration = hasUntilEndOfTurn ? new UntilEndOfTurnDuration() : null;

    effect = new GainControlEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
      IsOptional = isOptional,
      Duration = duration,
    };
    return true;
  }
}
