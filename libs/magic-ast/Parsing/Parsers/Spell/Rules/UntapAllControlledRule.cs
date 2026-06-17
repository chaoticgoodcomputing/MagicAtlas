namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap all [filter] you control." — mass-untap spell targeting every
/// qualifying permanent under the controller's control (CR 701.19a).
///
/// <para>Handled patterns (via <see cref="ISpellRule.TryMatch"/>):</para>
/// <list type="bullet">
///   <item>"Untap all nonland permanents you control." — Dramatic Reversal</item>
///   <item>"Untap all permanents you control."</item>
///   <item>"Untap all creatures you control."</item>
///   <item>"Untap all artifacts you control."</item>
/// </list>
///
/// <para>
/// Emits an <see cref="UntapEffect"/> whose <see cref="UntapEffect.Target"/> has
/// <see cref="ObjectReferenceKind.Each"/> and the appropriate <see cref="ObjectFilter"/>
/// with <see cref="ObjectFilter.Controller"/> = <see cref="ControllerFilter.You"/>.
/// </para>
/// </summary>
[SpellRule]
public sealed class UntapAllControlledRule : ISpellRule
{
  // Matches "Untap all <filter> you control"
  // The filter phrase may be multi-word (e.g. "nonland permanents").
  private static readonly Regex Pattern = new(
    @"^Untap\s+all\s+(?<filter>.+?)\s+you\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var m = Pattern.Match(trimmed);
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

    // Attach the controller constraint: "you control" → Controller = You.
    filter = filter with { Controller = ControllerFilter.You };

    effect = new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = filter,
      },
    };
    return true;
  }
}
