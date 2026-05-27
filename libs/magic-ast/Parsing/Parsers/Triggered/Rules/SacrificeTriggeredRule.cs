namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it" — triggered self-sacrifice on the creature that fired the
/// trigger (Rule 701.17). The pronoun "it" refers back to the trigger subject
/// and maps to <see cref="ObjectReferenceKind.It"/>, matching the pronoun-
/// reference convention used elsewhere in triggered rules.
/// </summary>
[TriggeredRule]
public sealed class SacrificeTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+(it|this\s+creature|this\s+permanent)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new SacrificeEffect { Target = ObjectReference.It() };
    return true;
  }
}
