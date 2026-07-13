namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it" / "sacrifice this creature" / "sacrifice this permanent" —
/// triggered self-sacrifice (Rule 701.21a — Sacrifice). The reference kind depends
/// on the surface form: the pronoun "it" refers back to a previously-mentioned
/// object and maps to <see cref="ObjectReferenceKind.It"/>, whereas the explicit
/// "this creature"/"this permanent" self-reference (no antecedent) maps to
/// <see cref="ObjectReferenceKind.Self"/> — matching the canonical
/// <see cref="ObjectReference"/> convention (Self = "this creature", It = a prior
/// object) and every existing "sacrifice this creature" gold (Longhorn Firebeast,
/// Wild Leotau, Whipstitched Zombie).
/// </summary>
[TriggeredRule]
public sealed class SacrificeTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+(?<ref>it|this\s+creature|this\s+permanent)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text);
    if (!match.Success)
    {
      return false;
    }

    // "it" is an anaphoric pronoun (prior object); "this creature"/"this permanent"
    // is an explicit self-reference with no antecedent.
    var isPronoun = match.Groups["ref"].Value.Trim().Equals("it", System.StringComparison.OrdinalIgnoreCase);
    effect = new SacrificeEffect
    {
      Target = isPronoun ? ObjectReference.It() : ObjectReference.Self(),
    };
    return true;
  }
}
