namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// Canonical identity of a Magic keyword ability (CR 702). A structured
/// alternative to the bare keyword strings the AST has historically carried —
/// casing-proof and exhaustively matchable.
///
/// <para>
/// Seeded with the keyword abilities currently structured by
/// <see cref="KeywordCharacteristic"/> (the evasion-relevant keywords that
/// appear inside <see cref="ObjectFilter.Characteristics"/>). It grows as
/// further keyword-as-string sites are subsumed — notably the planned
/// migration of <c>Ability.KeywordSource</c>. Only parameterless keyword
/// abilities belong here; parameterized keywords (Protection from …, Enchant …,
/// landcycling) carry their parameter separately and are added when that
/// migration lands.
/// </para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KeywordAbility
{
  /// <summary>Flying (CR 702.9).</summary>
  Flying,

  /// <summary>Reach (CR 702.17).</summary>
  Reach,

  /// <summary>Shadow (CR 702.28).</summary>
  Shadow,

  /// <summary>Aftermath (CR 702).</summary>
  Aftermath,

  /// <summary>Ascend (CR 702).</summary>
  Ascend,

  /// <summary>Assist (CR 702).</summary>
  Assist,

  /// <summary>Banding (CR 702).</summary>
  Banding,

  /// <summary>Bargain (CR 702).</summary>
  Bargain,

  /// <summary>BattleCry (CR 702).</summary>
  BattleCry,

  /// <summary>Cascade (CR 702).</summary>
  Cascade,

  /// <summary>Changeling (CR 702).</summary>
  Changeling,

  /// <summary>Cipher (CR 702).</summary>
  Cipher,

  /// <summary>Conspire (CR 702).</summary>
  Conspire,

  /// <summary>Converge (CR 702).</summary>
  Converge,

  /// <summary>Convoke (CR 702).</summary>
  Convoke,

  /// <summary>Deathtouch (CR 702).</summary>
  Deathtouch,

  /// <summary>Defender (CR 702).</summary>
  Defender,

  /// <summary>Delve (CR 702).</summary>
  Delve,

  /// <summary>Dethrone (CR 702).</summary>
  Dethrone,

  /// <summary>Devoid (CR 702).</summary>
  Devoid,

  /// <summary>DoctorsCompanion (CR 702).</summary>
  DoctorsCompanion,

  /// <summary>Enlist (CR 702).</summary>
  Enlist,

  /// <summary>Equip (CR 702.6) — the activated ability that attaches an Equipment to a creature. The keyword identity a reference filter matches on (e.g. Strong Back's "Equip abilities you activate") per ADR 0003.</summary>
  Equip,

  /// <summary>Evolve (CR 702).</summary>
  Evolve,

  /// <summary>Exalted (CR 702).</summary>
  Exalted,

  /// <summary>Exploit (CR 702).</summary>
  Exploit,

  /// <summary>Extort (CR 702).</summary>
  Extort,

  /// <summary>Flanking (CR 702).</summary>
  Flanking,

  /// <summary>ForMirrodin (CR 702).</summary>
  ForMirrodin,

  /// <summary>Fuse (CR 702).</summary>
  Fuse,

  /// <summary>Haste (CR 702).</summary>
  Haste,

  /// <summary>Haunt (CR 702).</summary>
  Haunt,

  /// <summary>Hexproof (CR 702).</summary>
  Hexproof,

  /// <summary>Horsemanship (CR 702).</summary>
  Horsemanship,

  /// <summary>Improvise (CR 702).</summary>
  Improvise,

  /// <summary>Indestructible (CR 702).</summary>
  Indestructible,

  /// <summary>Infect (CR 702).</summary>
  Infect,

  /// <summary>Ingest (CR 702).</summary>
  Ingest,

  /// <summary>JobSelect (CR 702).</summary>
  JobSelect,

  /// <summary>JumpStart (CR 702).</summary>
  JumpStart,

  /// <summary>Learn (CR 702).</summary>
  Learn,

  /// <summary>LivingWeapon (CR 702).</summary>
  LivingWeapon,

  /// <summary>Melee (CR 702).</summary>
  Melee,

  /// <summary>Mentor (CR 702).</summary>
  Mentor,

  /// <summary>Myriad (CR 702).</summary>
  Myriad,

  /// <summary>Persist (CR 702).</summary>
  Persist,

  /// <summary>Phasing (CR 702).</summary>
  Phasing,

  /// <summary>Prepared (CR 702).</summary>
  Prepared,

  /// <summary>Provoke (CR 702).</summary>
  Provoke,

  /// <summary>Prowess (CR 702).</summary>
  Prowess,

  /// <summary>Rebound (CR 702).</summary>
  Rebound,

  /// <summary>Retrace (CR 702).</summary>
  Retrace,

  /// <summary>Riot (CR 702).</summary>
  Riot,

  /// <summary>Shroud (CR 702).</summary>
  Shroud,

  /// <summary>Skulk (CR 702).</summary>
  Skulk,

  /// <summary>Soulbond (CR 702).</summary>
  Soulbond,

  /// <summary>SplitSecond (CR 702).</summary>
  SplitSecond,

  /// <summary>Spree (CR 702).</summary>
  Spree,

  /// <summary>StartYourEngines (CR 702).</summary>
  StartYourEngines,

  /// <summary>Storm (CR 702).</summary>
  Storm,

  /// <summary>Sunburst (CR 702).</summary>
  Sunburst,

  /// <summary>TakeInitiative (CR 702).</summary>
  TakeInitiative,

  /// <summary>TotemArmor (CR 702).</summary>
  TotemArmor,

  /// <summary>Training (CR 702).</summary>
  Training,

  /// <summary>Trample (CR 702).</summary>
  Trample,

  /// <summary>Undying (CR 702).</summary>
  Undying,

  /// <summary>Unleash (CR 702).</summary>
  Unleash,

  /// <summary>Vigilance (CR 702).</summary>
  Vigilance,

  /// <summary>Wither (CR 702).</summary>
  Wither,
}
