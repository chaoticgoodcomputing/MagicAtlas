namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it gets +N/+N until end of turn" — triggered P/T modification on the creature
/// that fired the trigger (Rule 508 attack trigger pattern). The subject "it" refers
/// to the attacking or otherwise-triggering creature and maps to
/// <see cref="ObjectReferenceKind.It"/>.
/// </summary>
[TriggeredRule]
public sealed class ModifyPTTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();

    // Detect the "gets +N/+N [until end of turn]" pattern. Accepts positive or
    // negative modifiers. Subject must be "it" (pronoun for the triggering creature).
    if (!lower.Contains("gets") || !Regex.IsMatch(lower, @"[+-]\d+/[+-]\d+"))
    {
      return false;
    }

    // Parse "it gets +P/+T", "this creature gets +P/+T", or "target [creature] gets +P/+T"
    ObjectReference target;
    if (Regex.IsMatch(lower, @"\bit\s+gets\b"))
    {
      target = ObjectReference.It();
    }
    else if (Regex.IsMatch(lower, @"\bthis\s+creature\s+gets\b"))
    {
      target = ObjectReference.Self();
    }
    else if (Regex.IsMatch(lower, @"\btarget\s+creature\b"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };
    }
    else
    {
      return false;
    }

    var ptMatch = Regex.Match(text, @"(?<p>[+-]\d+)/(?<t>[+-]\d+)");
    if (!ptMatch.Success)
    {
      return false;
    }

    var power = int.Parse(ptMatch.Groups["p"].Value);
    var toughness = int.Parse(ptMatch.Groups["t"].Value);

    MagicAST.AST.Effects.Duration? duration = null;
    if (lower.Contains("until end of turn"))
    {
      duration = new UntilEndOfTurnDuration();
    }

    effect = new ModifyPTEffect
    {
      Target = target,
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = duration,
    };
    return true;
  }
}
