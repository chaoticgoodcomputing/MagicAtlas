namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Recognises the token-copy-with-haste-and-end-step-exile pattern.
/// CR 707.2: except-clause modifications are copiable values printed on the token.
/// The trigger fires at the beginning of the end step (no "next" qualifier).
/// Priority 67 -- above generic copy-token rules (65).
/// </summary>
[SpellRule(Priority = 67)]
public sealed class CreateCopyTokenWithHasteAndEndStepExileRule : ISpellRule
{
  // Matches the oracle text for Heat Shimmer / Twinflame:
  //   Create a token that's a copy of target creature, except it has haste and
  //   "At the beginning of the end step, exile this token."
  // Pattern uses C# verbatim-string "" = one literal double-quote.
  private static readonly Regex _pattern = new Regex(
    @"^create\s+a\s+token\s+that's\s+a\s+copy\s+of\s+target\s+creature,\s+except\s+it\s+has\s+haste\s+and\s+"
    + @""""
    + @"at\s+the\s+beginning\s+of\s+the\s+end\s+step,\s+exile\s+this\s+token\."
    + @""""
    + "$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
        },
      },
      Modifications =
      [
        new AbilityAdder { AbilityText = "haste" },
        new TriggeredAbilityAdder
        {
          Ability = new TriggeredAbility
          {
            Trigger = new MagicAST.AST.Triggers.TriggerCondition
            {
              Timing = MagicAST.AST.Triggers.TriggerTiming.At,
              Event = new MagicAST.AST.References.GameTime
              {
                Part = MagicAST.AST.References.TurnPart.End,
                Edge = MagicAST.AST.References.TimeBoundary.Beginning,
              },
            },
            Effects = [new ExileEffect { Target = ObjectReference.Self() }],
          },
        },
      ],
    };
    return true;
  }
}
