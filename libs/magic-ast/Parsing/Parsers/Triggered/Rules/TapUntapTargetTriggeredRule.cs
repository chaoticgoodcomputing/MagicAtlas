namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Triggered-ability rule for tap, untap, and tap-or-untap effects on a named target.
/// Covers three surface patterns:
/// <list type="bullet">
///   <item>"tap target [filter]" → <see cref="TapEffect"/></item>
///   <item>"untap target [filter]" → <see cref="UntapEffect"/></item>
///   <item>"(you may) tap or untap target [filter]" → <see cref="TapOrUntapEffect"/></item>
/// </list>
/// The filter phrase may be a single card type ("creature") or a disjunction
/// ("artifact or creature", "creature or land", "artifact, creature, or land").
/// Rule 701.26 (Tap and Untap).
/// </summary>
[TriggeredRule]
public sealed class TapUntapTargetTriggeredRule : ITriggeredRule
{
  // Named groups:
  //   optional  — "you may" prefix (present ⇒ IsOptional = true)
  //   verb      — "tap or untap" | "untap" | "tap"
  //   types     — everything after "target" (the card-type disjunction phrase)
  private static readonly Regex Pattern = new(
    @"^(?<optional>you\s+may\s+)?(?<verb>tap\s+or\s+untap|untap|tap)\s+target\s+(?<types>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var isOptional = m.Groups["optional"].Success;
    var verb = m.Groups["verb"].Value.Trim().ToLowerInvariant();
    var typesPhrase = m.Groups["types"].Value.Trim();

    // Build the target filter from the type disjunction phrase.
    // Delegate to SpellRuleHelpers.SplitTypeDisjunction so we share the same
    // "creature or land", "artifact, creature, or land" lexer as spell rules.
    var types = SpellRuleHelpers
      .SplitTypeDisjunction(typesPhrase)
      .Where(t => t.Length > 0)
      .ToList();

    if (types.Count == 0)
    {
      return false;
    }

    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = types },
    };

    effect = verb switch
    {
      "tap or untap" => MagicAST.AST.Effects.Core.EffectWrap.Optional(new TapOrUntapEffect { Target = target}, isOptional),
      "untap" => MagicAST.AST.Effects.Core.EffectWrap.Optional(new UntapEffect { Target = target}, isOptional),
      _ => MagicAST.AST.Effects.Core.EffectWrap.Optional(new TapEffect { Target = target}, isOptional),
    };
    return true;
  }
}
