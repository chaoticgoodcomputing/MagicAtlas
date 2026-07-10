namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// Canonical identity of a Magic keyword ability (CR 702). A structured
/// alternative to the bare keyword strings the AST has historically carried —
/// casing-proof and exhaustively matchable.
///
/// <para>
/// This is the typed target of <c>Ability.KeywordSource</c> (ADR 0001): every
/// keyword-as-string a producer emits maps to a member here. Members whose
/// default serialization (the member name verbatim) differs from the exact
/// printed string carry <see cref="JsonStringEnumMemberNameAttribute"/> so the
/// serialized JSON round-trips unchanged (e.g. <c>BattleCry</c> ⇄
/// <c>"Battle cry"</c>).
/// </para>
///
/// <para>
/// Parameterized keywords carry their parameter separately (in the expanded
/// effect / reminder), so only their bare identity appears here:
/// <c>Affinity</c> (the "for &lt;type&gt;" lives in the effect),
/// <c>Champion</c> (the championed type lives in <c>ChampionEffect</c>),
/// <c>Landcycling</c> (the land type lives in <c>TypecyclingEffect.Type</c>).
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

  /// <summary>Afflict (CR 702).</summary>
  Afflict,

  /// <summary>Afterlife (CR 702).</summary>
  Afterlife,

  /// <summary>Aftermath (CR 702).</summary>
  Aftermath,

  /// <summary>Amplify (CR 702).</summary>
  Amplify,

  /// <summary>Ascend (CR 702).</summary>
  Ascend,

  /// <summary>Assist (CR 702).</summary>
  Assist,

  /// <summary>Awaken (CR 702).</summary>
  Awaken,

  /// <summary>Backup (CR 702).</summary>
  Backup,

  /// <summary>Banding (CR 702).</summary>
  Banding,

  /// <summary>Bargain (CR 702).</summary>
  Bargain,

  /// <summary>Battle cry (CR 702).</summary>
  [JsonStringEnumMemberName("Battle cry")]
  BattleCry,

  /// <summary>Bestow (CR 702).</summary>
  Bestow,

  /// <summary>Blitz (CR 702).</summary>
  Blitz,

  /// <summary>Bloodthirst (CR 702).</summary>
  Bloodthirst,

  /// <summary>Bushido (CR 702).</summary>
  Bushido,

  /// <summary>Buyback (CR 702).</summary>
  Buyback,

  /// <summary>Cascade (CR 702).</summary>
  Cascade,

  /// <summary>Casualty (CR 702.153). Parameterized — the integer N is carried by the
  /// expanded sacrifice cost, not this identity.</summary>
  Casualty,

  /// <summary>Changeling (CR 702).</summary>
  Changeling,

  /// <summary>Choose a Background (CR 702).</summary>
  [JsonStringEnumMemberName("Choose a Background")]
  ChooseABackground,

  /// <summary>Cipher (CR 702).</summary>
  Cipher,

  /// <summary>Conspire (CR 702).</summary>
  Conspire,

  /// <summary>Converge (CR 702).</summary>
  Converge,

  /// <summary>Convoke (CR 702).</summary>
  Convoke,

  /// <summary>Crew (CR 702).</summary>
  Crew,

  /// <summary>Cumulative upkeep (CR 702).</summary>
  [JsonStringEnumMemberName("Cumulative upkeep")]
  CumulativeUpkeep,

  /// <summary>Cycling (CR 702).</summary>
  Cycling,

  /// <summary>Dash (CR 702).</summary>
  Dash,

  /// <summary>Daybound (CR 702).</summary>
  Daybound,

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

  /// <summary>Devour (CR 702).</summary>
  Devour,

  /// <summary>Disguise (CR 702).</summary>
  Disguise,

  /// <summary>
  /// Disturb (CR 702.146a). Parameterized — the alternative cost is carried by the
  /// expanded <c>AlternativeCastEffect</c>, not this identity.
  /// </summary>
  Disturb,

  /// <summary>Doctor's companion (CR 702).</summary>
  [JsonStringEnumMemberName("Doctor's companion")]
  DoctorsCompanion,

  /// <summary>Double strike (CR 702).</summary>
  [JsonStringEnumMemberName("Double strike")]
  DoubleStrike,

  /// <summary>Dredge (CR 702).</summary>
  Dredge,

  /// <summary>Echo (CR 702).</summary>
  Echo,

  /// <summary>Embalm (CR 702).</summary>
  Embalm,

  /// <summary>Emerge (CR 702).</summary>
  Emerge,

  /// <summary>Enchant (CR 702.5).</summary>
  Enchant,

  /// <summary>Encore (CR 702).</summary>
  Encore,

  /// <summary>Enlist (CR 702).</summary>
  Enlist,

  /// <summary>Entwine (CR 702).</summary>
  Entwine,

  /// <summary>Equip (CR 702.6) — the activated ability that attaches an Equipment to a creature. The keyword identity a reference filter matches on (e.g. Strong Back's "Equip abilities you activate") per ADR 0003.</summary>
  Equip,

  /// <summary>Escalate (CR 702).</summary>
  Escalate,

  /// <summary>Escape (CR 702).</summary>
  Escape,

  /// <summary>Eternalize (CR 702).</summary>
  Eternalize,

  /// <summary>Evoke (CR 702).</summary>
  Evoke,

  /// <summary>Evolve (CR 702).</summary>
  Evolve,

  /// <summary>Exalted (CR 702).</summary>
  Exalted,

  /// <summary>Exert (CR 701.43) — keyword action. An optional cost to attack that
  /// causes the creature not to untap during its controller's next untap step.
  /// Linked with a "When you do" triggered ability on the same card (CR 607.2h).</summary>
  Exert,

  /// <summary>Exploit (CR 702).</summary>
  Exploit,

  /// <summary>Extort (CR 702).</summary>
  Extort,

  /// <summary>Fabricate (CR 702).</summary>
  Fabricate,

  /// <summary>Fading (CR 702).</summary>
  Fading,

  /// <summary>Fear (CR 702).</summary>
  Fear,

  /// <summary>Firebending (CR 702.189).</summary>
  Firebending,

  /// <summary>First strike (CR 702).</summary>
  [JsonStringEnumMemberName("First strike")]
  FirstStrike,

  /// <summary>Flanking (CR 702).</summary>
  Flanking,

  /// <summary>Flash (CR 702).</summary>
  Flash,

  /// <summary>Flashback (CR 702).</summary>
  Flashback,

  /// <summary>For Mirrodin! (CR 702).</summary>
  [JsonStringEnumMemberName("For Mirrodin")]
  ForMirrodin,

  /// <summary>Forestwalk (CR 702.14).</summary>
  Forestwalk,

  /// <summary>Forage (CR 701.61) — keyword action cost. "Exile three cards from your graveyard or sacrifice a Food."</summary>
  Forage,

  /// <summary>Foretell (CR 702).</summary>
  Foretell,

  /// <summary>Freerunning (CR 702).</summary>
  Freerunning,

  /// <summary>Fuse (CR 702).</summary>
  Fuse,

  /// <summary>Graft (CR 702).</summary>
  Graft,

  /// <summary>Harmonize (CR 702).</summary>
  Harmonize,

  /// <summary>Haste (CR 702).</summary>
  Haste,

  /// <summary>Haunt (CR 702).</summary>
  Haunt,

  /// <summary>Hexproof (CR 702).</summary>
  Hexproof,

  /// <summary>Hideaway (CR 702).</summary>
  Hideaway,

  /// <summary>Horsemanship (CR 702).</summary>
  Horsemanship,

  /// <summary>Improvise (CR 702).</summary>
  Improvise,

  /// <summary>Increment (CR 702).</summary>
  Increment,

  /// <summary>Indestructible (CR 702).</summary>
  Indestructible,

  /// <summary>Infect (CR 702).</summary>
  Infect,

  /// <summary>Ingest (CR 702).</summary>
  Ingest,

  /// <summary>Intimidate (CR 702).</summary>
  Intimidate,

  /// <summary>Islandwalk (CR 702.14).</summary>
  Islandwalk,

  /// <summary>Job select (CR 702).</summary>
  [JsonStringEnumMemberName("Job select")]
  JobSelect,

  /// <summary>Jump-start (CR 702).</summary>
  [JsonStringEnumMemberName("Jump-start")]
  JumpStart,

  /// <summary>Kicker (CR 702.33).</summary>
  Kicker,

  /// <summary>Learn (CR 702).</summary>
  Learn,

  /// <summary>Level up (CR 702.87).</summary>
  [JsonStringEnumMemberName("Level up")]
  LevelUp,

  /// <summary>Lifelink (CR 702).</summary>
  Lifelink,

  /// <summary>Living weapon (CR 702).</summary>
  [JsonStringEnumMemberName("Living weapon")]
  LivingWeapon,

  /// <summary>Madness (CR 702).</summary>
  Madness,

  /// <summary>Mayhem (CR 702).</summary>
  Mayhem,

  /// <summary>Megamorph (CR 702).</summary>
  Megamorph,

  /// <summary>Melee (CR 702).</summary>
  Melee,

  /// <summary>Menace (CR 702).</summary>
  Menace,

  /// <summary>Mentor (CR 702).</summary>
  Mentor,

  /// <summary>Miracle (CR 702).</summary>
  Miracle,

  /// <summary>Mobilize (CR 702).</summary>
  Mobilize,

  /// <summary>Modular (CR 702).</summary>
  Modular,

  /// <summary>Morph (CR 702).</summary>
  Morph,

  /// <summary>Mountainwalk (CR 702.14).</summary>
  Mountainwalk,

  /// <summary>Multikicker (CR 702.33).</summary>
  Multikicker,

  /// <summary>Mutate (CR 702).</summary>
  Mutate,

  /// <summary>Myriad (CR 702).</summary>
  Myriad,

  /// <summary>Nightbound (CR 702).</summary>
  Nightbound,

  /// <summary>Ninjutsu (CR 702).</summary>
  Ninjutsu,

  /// <summary>Offspring (CR 702).</summary>
  Offspring,

  /// <summary>Outlast (CR 702).</summary>
  Outlast,

  /// <summary>Overload (CR 702).</summary>
  Overload,

  /// <summary>Partner (CR 702.124).</summary>
  Partner,

  /// <summary>Partner with (CR 702.124).</summary>
  [JsonStringEnumMemberName("Partner with")]
  PartnerWith,

  /// <summary>Persist (CR 702).</summary>
  Persist,

  /// <summary>Phasing (CR 702).</summary>
  Phasing,

  /// <summary>Plainswalk (CR 702.14).</summary>
  Plainswalk,

  /// <summary>Plot (CR 702).</summary>
  Plot,

  /// <summary>Prepared (CR 702).</summary>
  Prepared,

  /// <summary>Protection (CR 702.16).</summary>
  Protection,

  /// <summary>Prototype (CR 702).</summary>
  Prototype,

  /// <summary>Provoke (CR 702).</summary>
  Provoke,

  /// <summary>Prowess (CR 702).</summary>
  Prowess,

  /// <summary>Rampage (CR 702.23).</summary>
  Rampage,

  /// <summary>Rebound (CR 702).</summary>
  Rebound,

  /// <summary>Reconfigure (CR 702).</summary>
  Reconfigure,

  /// <summary>Recover (CR 702).</summary>
  Recover,

  /// <summary>Reinforce (CR 702.77).</summary>
  Reinforce,

  /// <summary>Renown (CR 702).</summary>
  Renown,

  /// <summary>Replicate (CR 702).</summary>
  Replicate,

  /// <summary>Retrace (CR 702).</summary>
  Retrace,

  /// <summary>Riot (CR 702).</summary>
  Riot,

  /// <summary>Ripple (CR 702.60).</summary>
  Ripple,

  /// <summary>Saddle (CR 702).</summary>
  Saddle,

  /// <summary>Scavenge (CR 702).</summary>
  Scavenge,

  /// <summary>Shroud (CR 702).</summary>
  Shroud,

  /// <summary>Skulk (CR 702).</summary>
  Skulk,

  /// <summary>Sneak (CR 702).</summary>
  Sneak,

  /// <summary>Soulbond (CR 702).</summary>
  Soulbond,

  /// <summary>Soulshift (CR 702).</summary>
  Soulshift,

  /// <summary>Spectacle (CR 702).</summary>
  Spectacle,

  /// <summary>Splice (CR 702).</summary>
  Splice,

  /// <summary>Split second (CR 702).</summary>
  [JsonStringEnumMemberName("Split second")]
  SplitSecond,

  /// <summary>Spree (CR 702).</summary>
  Spree,

  /// <summary>Squad (CR 702).</summary>
  Squad,

  /// <summary>Start your engines! (CR 702).</summary>
  [JsonStringEnumMemberName("Start your engines")]
  StartYourEngines,

  /// <summary>Storm (CR 702).</summary>
  Storm,

  /// <summary>Sunburst (CR 702).</summary>
  Sunburst,

  /// <summary>Surge (CR 702).</summary>
  Surge,

  /// <summary>Suspend (CR 702).</summary>
  Suspend,

  /// <summary>Swampwalk (CR 702.14).</summary>
  Swampwalk,

  /// <summary>TakeInitiative (CR 702).</summary>
  TakeInitiative,

  /// <summary>Totem armor (CR 702.89) — the obsolete name, retained for cards still printed with it.</summary>
  [JsonStringEnumMemberName("Totem armor")]
  TotemArmor,

  /// <summary>
  /// Umbra armor (CR 702.89) — the current Oracle name for the keyword formerly
  /// printed as "totem armor". Per CR 702.89 the comp-rules glossary lists "Totem
  /// Armor (Obsolete)" as renamed to "Umbra Armor", and the Oracle reference updates
  /// the text of older cards to "umbra armor". A distinct enum member (rather than
  /// reusing <see cref="TotemArmor"/>) so the recorded keyword matches the card's
  /// current Oracle wording faithfully.
  /// </summary>
  [JsonStringEnumMemberName("Umbra armor")]
  UmbraArmor,

  /// <summary>Toxic (CR 702).</summary>
  Toxic,

  /// <summary>Training (CR 702).</summary>
  Training,

  /// <summary>Trample (CR 702).</summary>
  Trample,

  /// <summary>Transmute (CR 702).</summary>
  Transmute,

  /// <summary>Tribute (CR 702).</summary>
  Tribute,

  /// <summary>Undying (CR 702).</summary>
  Undying,

  /// <summary>Unearth (CR 702).</summary>
  Unearth,

  /// <summary>Unleash (CR 702).</summary>
  Unleash,

  /// <summary>Vanishing (CR 702).</summary>
  Vanishing,

  /// <summary>Vigilance (CR 702).</summary>
  Vigilance,

  /// <summary>Ward (CR 702.21).</summary>
  Ward,

  /// <summary>Warp (CR 702).</summary>
  Warp,

  /// <summary>Web-slinging (CR 702).</summary>
  [JsonStringEnumMemberName("Web-slinging")]
  WebSlinging,

  /// <summary>Wither (CR 702).</summary>
  Wither,

  /// <summary>
  /// Affinity (CR 702.41). Parameterized — the "for &lt;type&gt;" parameter is
  /// carried by the expanded effect, not this identity.
  /// </summary>
  Affinity,

  /// <summary>
  /// Champion (CR 702.72). Parameterized — the championed type is carried by
  /// <c>ChampionEffect</c>, not this identity.
  /// </summary>
  Champion,

  /// <summary>
  /// Landcycling (CR 702.29). Parameterized — the searched land type is carried
  /// by <c>TypecyclingEffect.Type</c>, not this identity. Single-word land-type
  /// cycling variants (Forestcycling, Swampcycling, …) and "Basic landcycling"
  /// all collapse to this identity.
  /// </summary>
  Landcycling,

  /// <summary>
  /// Compleated (CR 702.150). A static ability on Phyrexian planeswalkers that
  /// causes the planeswalker to enter with two fewer loyalty counters for each
  /// Phyrexian mana symbol whose cost was paid with 2 life.
  /// </summary>
  Compleated,
}
