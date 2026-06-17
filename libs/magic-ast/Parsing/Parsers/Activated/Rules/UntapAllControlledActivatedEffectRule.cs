namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// "Untap all [filter] you control." — mass-untap of all permanents matching a filter
/// under the controller's control. Activated-ability mirror of
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.UntapAllControlledRule"/>.
///
/// <para>Handled patterns (via <see cref="IActivatedEffectRule.TryMatch"/>):</para>
/// <list type="bullet">
///   <item>"Untap all creatures you control." — Aggravated Assault</item>
///   <item>"Untap all nonland permanents you control."</item>
///   <item>"Untap all permanents you control."</item>
///   <item>"Untap all artifacts you control."</item>
/// </list>
///
/// <para>
/// Emits an <see cref="UntapEffect"/> whose <see cref="UntapEffect.Target"/> has
/// <see cref="ObjectReferenceKind.Each"/> and the appropriate <see cref="ObjectFilter"/>
/// with <see cref="ObjectFilter.Controller"/> = <see cref="ControllerFilter.You"/>
/// (CR 701.19a: "to untap a permanent, rotate it back to the upright position from a
/// sideways position. Only tapped permanents can be untapped.").
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 992)]
public sealed class UntapAllControlledActivatedEffectRule : IActivatedEffectRule
{
  // Matches "Untap all <filter> you control" (with optional trailing period)
  private static readonly Regex Pattern = new(
    @"^Untap\s+all\s+(?<filter>.+?)\s+you\s+control\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return null;
    }

    // Attach the controller constraint: "you control" → Controller = You.
    filter = filter with { Controller = ControllerFilter.You };

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = filter,
      },
    };
  }
}
