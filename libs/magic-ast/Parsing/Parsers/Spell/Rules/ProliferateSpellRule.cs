namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Proliferate (Rule 701.27) in a spell or activated-ability context.
///
/// <para>Handled patterns (via <see cref="ISpellRule.TryMatch"/>):</para>
/// <list type="bullet">
///   <item>"Proliferate." — standalone spell or activated effect.</item>
/// </list>
///
/// <para>Handled patterns (via <see cref="IMultiSpellRule.TryMatchMulti"/>):</para>
/// <list type="bullet">
///   <item>"Destroy target [filter], then proliferate." — emits a flat
///     [<see cref="DestroyEffect"/>, <see cref="ProliferateEffect"/>] list.
///     The "then proliferate" suffix is a sequencing marker; both effects are
///     siblings on the ability's Effects list. (Spread the Sickness pattern.)</item>
/// </list>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class ProliferateSpellRule : ISpellRule, IMultiSpellRule
{
  // Matches "Destroy target <filter>, then proliferate"
  // Reminder text has already been stripped by the dispatcher.
  private static readonly Regex DestroyThenProliferatePattern = new(
    @"^Destroy\s+target\s+(?<filter>.+?),\s+then\s+proliferate$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <inheritdoc cref="ISpellRule.TryMatch"/>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!text.Equals("Proliferate", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    effect = new ProliferateEffect();
    return true;
  }

  /// <inheritdoc cref="IMultiSpellRule.TryMatchMulti"/>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = DestroyThenProliferatePattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return false;
    }

    effects = new List<Effect>
    {
      new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = filter,
        },
      },
      new ProliferateEffect(),
    };
    return true;
  }
}
