namespace MagicAST.Parsing.Tokens;

using Superpower.Display;

/// <summary>
/// Token types recognized in Magic: The Gathering oracle text.
/// These represent the lexical units that the tokenizer produces.
/// </summary>
public enum OracleToken
{
  /// <summary>No token / error state.</summary>
  None,

  // ─────────────────────────────────────────────────────────────
  // Mana Symbols
  // ─────────────────────────────────────────────────────────────

  /// <summary>Generic mana symbol: {1}, {2}, {3}, etc.</summary>
  [Token(Category = "mana", Example = "{2}")]
  GenericMana,

  /// <summary>Variable mana symbol: {X}, {Y}, {Z}.</summary>
  [Token(Category = "mana", Example = "{X}")]
  VariableMana,

  /// <summary>White mana: {W}.</summary>
  [Token(Category = "mana", Example = "{W}")]
  WhiteMana,

  /// <summary>Blue mana: {U}.</summary>
  [Token(Category = "mana", Example = "{U}")]
  BlueMana,

  /// <summary>Black mana: {B}.</summary>
  [Token(Category = "mana", Example = "{B}")]
  BlackMana,

  /// <summary>Red mana: {R}.</summary>
  [Token(Category = "mana", Example = "{R}")]
  RedMana,

  /// <summary>Green mana: {G}.</summary>
  [Token(Category = "mana", Example = "{G}")]
  GreenMana,

  /// <summary>Colorless mana: {C}.</summary>
  [Token(Category = "mana", Example = "{C}")]
  ColorlessMana,

  /// <summary>Snow mana: {S}.</summary>
  [Token(Category = "mana", Example = "{S}")]
  SnowMana,

  /// <summary>Hybrid mana: {W/U}, {B/R}, etc.</summary>
  [Token(Category = "mana", Example = "{W/U}")]
  HybridMana,

  /// <summary>Phyrexian mana: {W/P}, {G/P}, etc.</summary>
  [Token(Category = "mana", Example = "{G/P}")]
  PhyrexianMana,

  /// <summary>Hybrid Phyrexian mana: {W/U/P}, etc.</summary>
  [Token(Category = "mana", Example = "{W/U/P}")]
  HybridPhyrexianMana,

  /// <summary>Two-generic hybrid mana: {2/W}, {2/U}, etc.</summary>
  [Token(Category = "mana", Example = "{2/W}")]
  TwoHybridMana,

  // ─────────────────────────────────────────────────────────────
  // Special Symbols
  // ─────────────────────────────────────────────────────────────

  /// <summary>Tap symbol: {T}.</summary>
  [Token(Category = "symbol", Example = "{T}")]
  TapSymbol,

  /// <summary>Untap symbol: {Q}.</summary>
  [Token(Category = "symbol", Example = "{Q}")]
  UntapSymbol,

  /// <summary>Energy symbol: {E}.</summary>
  [Token(Category = "symbol", Example = "{E}")]
  EnergySymbol,

  /// <summary>Planeswalker loyalty up symbol: +N.</summary>
  [Token(Category = "symbol", Example = "+1")]
  LoyaltyUp,

  /// <summary>Planeswalker loyalty down symbol: −N.</summary>
  [Token(Category = "symbol", Example = "−2")]
  LoyaltyDown,

  /// <summary>Planeswalker loyalty zero symbol: 0.</summary>
  [Token(Category = "symbol", Example = "0")]
  LoyaltyZero,

  // ─────────────────────────────────────────────────────────────
  // Punctuation
  // ─────────────────────────────────────────────────────────────

  /// <summary>Colon separator (for activated abilities).</summary>
  [Token(Example = ":")]
  Colon,

  /// <summary>Comma separator.</summary>
  [Token(Example = ",")]
  Comma,

  /// <summary>Period (sentence end).</summary>
  [Token(Example = ".")]
  Period,

  /// <summary>Slash (for P/T notation like 1/1).</summary>
  [Token(Example = "/")]
  Slash,

  /// <summary>Em dash (for ability words, modal choices).</summary>
  [Token(Example = "—")]
  EmDash,

  /// <summary>Bullet point (for modal choices).</summary>
  [Token(Example = "•")]
  Bullet,

  /// <summary>Open parenthesis (reminder text start).</summary>
  [Token(Example = "(")]
  OpenParen,

  /// <summary>Close parenthesis (reminder text end).</summary>
  [Token(Example = ")")]
  CloseParen,

  /// <summary>Open quote (quoted ability start).</summary>
  [Token(Example = "\"")]
  OpenQuote,

  /// <summary>Close quote (quoted ability end).</summary>
  [Token(Example = "\"")]
  CloseQuote,

  /// <summary>Newline / paragraph break (ability separator).</summary>
  [Token(Example = "\\n")]
  Newline,

  // ─────────────────────────────────────────────────────────────
  // Structural Words (Trigger Timing)
  // ─────────────────────────────────────────────────────────────

  /// <summary>"When" - single trigger.</summary>
  [Token(Category = "trigger", Example = "When")]
  When,

  /// <summary>"Whenever" - repeating trigger.</summary>
  [Token(Category = "trigger", Example = "Whenever")]
  Whenever,

  /// <summary>"At" - phase/step trigger.</summary>
  [Token(Category = "trigger", Example = "At")]
  At,

  // ─────────────────────────────────────────────────────────────
  // Structural Words (Conditionals)
  // ─────────────────────────────────────────────────────────────

  /// <summary>"If" - conditional clause.</summary>
  [Token(Category = "conditional", Example = "If")]
  If,

  /// <summary>"Unless" - negative conditional.</summary>
  [Token(Category = "conditional", Example = "Unless")]
  Unless,

  /// <summary>"Instead" - replacement effect marker.</summary>
  [Token(Category = "conditional", Example = "Instead")]
  Instead,

  /// <summary>"Would" - replacement effect marker.</summary>
  [Token(Category = "conditional", Example = "Would")]
  Would,

  // ─────────────────────────────────────────────────────────────
  // Structural Words (Modal/Choice)
  // ─────────────────────────────────────────────────────────────

  /// <summary>"Choose" - modal ability marker.</summary>
  [Token(Category = "modal", Example = "Choose")]
  Choose,

  /// <summary>"Or" - alternative choice.</summary>
  [Token(Category = "modal", Example = "Or")]
  Or,

  /// <summary>"And" - conjunction.</summary>
  [Token(Category = "modal", Example = "And")]
  And,

  /// <summary>"Then" - sequential effect.</summary>
  [Token(Category = "modal", Example = "Then")]
  Then,

  // ─────────────────────────────────────────────────────────────
  // Common Reference Words
  // ─────────────────────────────────────────────────────────────

  /// <summary>"Target" - targeting indicator.</summary>
  [Token(Category = "reference", Example = "Target")]
  Target,

  /// <summary>"This" - self-reference (this creature, this spell).</summary>
  [Token(Category = "reference", Example = "This")]
  This,

  /// <summary>"That" - back-reference (that creature, that player).</summary>
  [Token(Category = "reference", Example = "That")]
  That,

  /// <summary>"It" - pronoun reference.</summary>
  [Token(Category = "reference", Example = "It")]
  It,

  /// <summary>"You" - controller reference.</summary>
  [Token(Category = "reference", Example = "You")]
  You,

  /// <summary>"Your" - possessive controller reference.</summary>
  [Token(Category = "reference", Example = "Your")]
  Your,

  /// <summary>"Each" - distributive reference.</summary>
  [Token(Category = "reference", Example = "Each")]
  Each,

  /// <summary>"All" - collective reference.</summary>
  [Token(Category = "reference", Example = "All")]
  All,

  /// <summary>"Another" - exclusive reference.</summary>
  [Token(Category = "reference", Example = "Another")]
  Another,

  /// <summary>"Any" - unrestricted reference.</summary>
  [Token(Category = "reference", Example = "Any")]
  Any,

  // ─────────────────────────────────────────────────────────────
  // Numbers
  // ─────────────────────────────────────────────────────────────

  /// <summary>Numeric literal: 1, 2, 3, etc.</summary>
  [Token(Category = "number", Example = "3")]
  Number,

  /// <summary>Word number: one, two, three, etc.</summary>
  [Token(Category = "number", Example = "three")]
  WordNumber,

  // ─────────────────────────────────────────────────────────────
  // Generic Tokens
  // ─────────────────────────────────────────────────────────────

  /// <summary>A word (identifier, keyword, or unknown).</summary>
  [Token(Category = "word", Example = "creature")]
  Word,

  /// <summary>Reminder text content (parenthesized text).</summary>
  [Token(Category = "reminder", Example = "(This creature can't block.)")]
  ReminderText,

  /// <summary>Quoted ability text.</summary>
  [Token(Category = "quoted", Example = "\"When this creature dies, draw a card.\"")]
  QuotedText,
}
