namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Recognises the token-copy-with-haste-and-end-step-sacrifice pattern
/// for a creature the controller controls (Electroduplicate).
/// CR 707.2: except-clause modifications are copiable values printed on the token.
/// The trigger fires at the beginning of the end step (no "next" qualifier).
/// Priority 68 — above generic copy-token rules (65) and the exile-variant (67).
/// </summary>
[SpellRule(Priority = 68)]
public sealed class CreateCopyTokenYouControlWithHasteAndEndStepSacrificeRule : ISpellRule
{
  // Matches the oracle text for Electroduplicate:
  //   Create a token that's a copy of target creature you control, except it has haste and
  //   "At the beginning of the end step, sacrifice this token."
  // Pattern uses C# verbatim-string "" = one literal double-quote.
  private static readonly Regex _pattern = new Regex(
    @"^create\s+a\s+token\s+that's\s+a\s+copy\s+of\s+target\s+creature\s+you\s+control,\s+except\s+it\s+has\s+haste\s+and\s+"
    + @""""
    + @"at\s+the\s+beginning\s+of\s+the\s+end\s+step,\s+sacrifice\s+this\s+token\."
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
          Controller = ControllerFilter.You,
        },
      },
      Modifications =
      [
        new KeywordAbilityAdder { Keywords = [KeywordAbility.Haste] },
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
            Effects = [new SacrificeEffect { Target = ObjectReference.Self() }],
          },
        },
      ],
    };
    return true;
  }
}
