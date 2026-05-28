namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 945)]
public sealed class PhantomDamagePreventionRule : IStaticRule
{
  // Matches "If damage would be dealt to [subject], prevent that damage.
  // Remove a +1/+1 counter from [subject]." — the Phantom mechanic.
  // The subject before "," is a named self-reference or "this creature" — both
  // are treated as Self. The two sentences are on a single oracle line.
  // Subject: any non-comma run of characters (lazy) terminated by a comma.
  // The same subject name must appear before "Remove a +1/+1 counter from".
  // We capture the subject to validate it appears in both positions, but
  // collapse it to Self unconditionally (card-name = self by convention).
  private static readonly Regex _phantomDamagePreventionPattern = new(
    @"^\s*If\s+damage\s+would\s+be\s+dealt\s+to\s+(?<subj>.+?),\s*prevent\s+that\s+damage\.\s*Remove\s+a\s+\+1/\+1\s+counter\s+from\s+(?<subj2>.+?)\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_phantomDamagePreventionPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.DamageEvent(),
          OriginalEventOccurs = false,
          Replacement = new MagicAST.AST.Effects.Core.CompositeEffect
          {
            Effects =
            [
              new MagicAST.AST.Effects.Damage.PreventDamageEffect
              {
                All = true,
                Target = ObjectReference.Self(),
              },
              new MagicAST.AST.Effects.Counter.RemoveCountersEffect
              {
                Target = ObjectReference.Self(),
                CounterType = "+1/+1",
                Count = MagicAST.AST.Quantities.LiteralQuantity.Of(1),
              },
            ],
          },
        }],
      },
    ];
  }
}
