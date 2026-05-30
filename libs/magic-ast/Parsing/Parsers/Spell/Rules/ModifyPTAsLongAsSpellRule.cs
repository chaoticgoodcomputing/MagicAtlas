namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the P/T-modification shape with an "as long as [condition]" duration:
///   "Target creature gets +N/+M for as long as [condition]."
///   "Target creature gets -N/-M for as long as [condition]."
///
/// The condition text (e.g. "this artifact remains tapped") is stored verbatim on
/// <see cref="AsLongAsDuration.Condition"/> — not parsed further. This mirrors how
/// <see cref="StaticAbilityParser.TryParseAsLongAsStaticGrant"/> handles the same
/// duration suffix on static ability grants.
///
/// Examples:
/// <list type="bullet">
///   <item>"Target creature gets +1/+1 for as long as this artifact remains tapped."  (Tawnos's Weaponry)</item>
///   <item>"Target creature gets +0/+2 for as long as this artifact remains tapped."  (Spirit Shield)</item>
///   <item>"Target creature gets +0/+3 for as long as this artifact remains tapped."  (Endoskeleton)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class ModifyPTAsLongAsSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+for\s+as\s+long\s+as\s+(?<cond>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);
    var condition = m.Groups["cond"].Value.Trim();

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = new AsLongAsDuration { Condition = MagicAST.Parsing.ConditionParser.Parse(condition) },
    };
    return true;
  }
}
