namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Cost increase effect: spells matching the containing ability's
/// <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/> filter
/// cost more to cast. A spell's total cost is "locked in" before payments are made
/// (CR 601.2), and the mana component of that total is what the caster pays (CR 118.7).
///
/// <para>
/// Five shapes share this node:
/// <list type="bullet">
///   <item>"Noncreature spells cost {1} more to cast." (Thorn of Amethyst / Sphere of
///         Resistance) — the filter sits on the enclosing StaticAbility.AffectedObjects;
///         this effect carries only the Amount (a purely generic increase).</item>
///   <item>"Spells your opponents cast that target this creature cost {N} more to cast."
///         (pre-Ward pattern) — a targeting condition with CasterFilter and TargetedObject.</item>
///   <item>"Red spells you cast cost {R} more to cast." (Ruby Leech / the Nemesis Leech
///         cycle) — the increase is a specific COLORED mana symbol, not purely generic, so
///         it is carried in <see cref="ManaSymbols"/> with a zero generic Amount. Colored
///         mana is load-bearing and must not be flattened to generic {1} (CR 601.2's
///         Altar's Reap example distinguishes {B} from {1}).</item>
///   <item>"This spell costs {cost} more to cast for each target beyond the first."
///         (Strive — an ability word, CR 207.2c; carries no rules meaning of its own,
///         so the sentence itself is the operative static ability, CR 604.1) —
///         self-referential: no AffectedObjects/TargetedObject/CasterFilter is set
///         (the spell taxes only itself), and <see cref="PerTargetBeyondFirst"/> is
///         true. CR 601.2f: the total cost is the mana cost plus all additional costs
///         and cost increases.</item>
///   <item>"Spells your opponents cast that target this creature cost an additional
///         3 life to cast." (Terror of the Peaks) — the same targeting-tax shape as
///         the pre-Ward pattern above (CasterFilter + TargetedObject), but the increase
///         is paid in a NON-MANA currency (life) rather than mana, so it is carried in
///         <see cref="LifeAmount"/> instead of <see cref="Amount"/>/<see cref="ManaSymbols"/>;
///         <see cref="Amount"/> is a zero literal (no mana component at all), mirroring how
///         the colored-only Ruby Leech shape zeroes Amount when the increase lives entirely
///         in a different field.</item>
/// </list>
/// </para>
/// </summary>
[OracleEffect("costIncrease")]
public sealed record CostIncreaseEffect : Effect
{
  /// <summary>
  /// The amount of the increase (the generic-mana component of the total increase).
  /// </summary>
  public required Quantity Amount { get; init; }

  /// <summary>
  /// Specific colored/colorless mana symbols added to the total cost when the increase
  /// is not purely generic — "Red spells you cast cost {R} more to cast" (Ruby Leech /
  /// the Nemesis Leech cycle). Total increase = <see cref="Amount"/> (generic component)
  /// + these symbols; CR 601.2, CR 118.7. Null when the increase is purely generic
  /// (Thorn of Amethyst / targeting-tax shapes), preserving those existing encodings.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<ManaSymbol>? ManaSymbols { get; init; }

  /// <summary>
  /// Life increase to the total cost — "cost an additional 3 life to cast" (Terror
  /// of the Peaks). A distinct currency from <see cref="Amount"/>/<see cref="ManaSymbols"/>
  /// (both mana): CR 601.2f's total cost sums "all additional costs and cost increases",
  /// and a life increase is not a mana symbol, so it is not flattened into the mana
  /// <see cref="Amount"/> (which stays a zero literal for this shape) nor into
  /// <see cref="ManaSymbols"/> (there is no {L} mana symbol — life is paid directly
  /// from the caster's life total, CR 119). Null for every mana-only shape above.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? LifeAmount { get; init; }

  /// <summary>
  /// The object that affected spells must target for the increase to apply.
  /// Typically <see cref="ObjectReferenceKind.Self"/> ("this creature").
  /// Null when the increase is unconditional (Thorn of Amethyst shape).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? TargetedObject { get; init; }

  /// <summary>
  /// Filter on whose spells are affected. Typically
  /// <see cref="ControllerFilter.Opponent"/> ("your opponents cast").
  /// When null, all spells are affected regardless of who casts them.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? CasterFilter { get; init; }

  /// <summary>
  /// "for each target beyond the first" — Strive's target-count cost-scaling axis
  /// (Kiora's Dismissal: "This spell costs {U} more to cast for each target beyond
  /// the first."). True when <see cref="Amount"/>/<see cref="ManaSymbols"/> is paid
  /// once per target chosen for THIS spell beyond the first, rather than as a flat
  /// one-time increase (CR 601.2f — additional costs and cost increases sum into the
  /// total cost). Distinct from <see cref="TargetedObject"/>, which names an object
  /// OTHER spells must target for THEIR cost to increase (Spiketail Drake); here the
  /// tax is on the ability's own spell and scales with its own chosen target count —
  /// a targeting-choice quantity outside <see cref="MagicAST.AST.References.ObjectFilter"/>
  /// scope (identical "beyond the first" doctrine to Rampage/Melee's residual, but
  /// here the axis is boolean rather than free text because the per-unit increment is
  /// already fully carried by <see cref="Amount"/>/<see cref="ManaSymbols"/>). Default
  /// false, preserving the three existing shapes above.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool PerTargetBeyondFirst { get; init; }
}
