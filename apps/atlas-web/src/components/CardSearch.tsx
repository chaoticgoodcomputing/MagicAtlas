// A live card-name search box with an autocomplete dropdown, backed by the API
// (cardRows name filter). Keyboard-navigable (↑/↓/Enter/Escape). Used by the
// Card Explorer to replace the old fixed chip list — any of the ~38k cards.

import { useEffect, useRef, useState } from "react";
import { useQuery } from "@apollo/client";
import { CARD_SEARCH_QUERY } from "../queries";

// MTG names are Title Case; `contains` is case-sensitive, so we search the raw
// text and a title-cased variant. Only capitalises the first letter of each
// space-separated word (leaves apostrophes/minor punctuation alone).
const titleCase = (s: string): string =>
  s.replace(/(^|\s)([a-z])/g, (_, p: string, c: string) => p + c.toUpperCase());

interface Match { name: string; typeLine: string | null; }

export function CardSearch({
  onSelect,
  placeholder = "Search any card…",
}: {
  onSelect: (name: string) => void;
  placeholder?: string;
}) {
  const [input, setInput] = useState("");
  const [q, setQ] = useState("");
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const boxRef = useRef<HTMLDivElement>(null);

  // Debounce the query behind the raw input so we don't fire per keystroke.
  useEffect(() => {
    const t = setTimeout(() => setQ(input.trim()), 150);
    return () => clearTimeout(t);
  }, [input]);

  const { data } = useQuery(CARD_SEARCH_QUERY, {
    variables: { q, q2: titleCase(q) },
    skip: q.length < 2,
  });
  const matches: Match[] = data?.discover?.atlas?.cardRows?.nodes ?? [];

  // Close the dropdown on an outside click.
  useEffect(() => {
    const onDoc = (e: MouseEvent) => {
      if (boxRef.current && !boxRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, []);

  const pick = (name: string) => {
    onSelect(name);
    setInput("");
    setQ("");
    setOpen(false);
  };

  const showList = open && q.length >= 2 && matches.length > 0;

  return (
    <div className="card-search" ref={boxRef}>
      <input
        className="input"
        value={input}
        placeholder={placeholder}
        aria-label="Search cards"
        onChange={(e) => { setInput(e.target.value); setOpen(true); setActive(0); }}
        onFocus={() => setOpen(true)}
        onKeyDown={(e) => {
          if (!showList) return;
          if (e.key === "ArrowDown") { e.preventDefault(); setActive((a) => Math.min(a + 1, matches.length - 1)); }
          else if (e.key === "ArrowUp") { e.preventDefault(); setActive((a) => Math.max(a - 1, 0)); }
          else if (e.key === "Enter") { e.preventDefault(); pick(matches[active].name); }
          else if (e.key === "Escape") { setOpen(false); }
        }}
      />
      {showList && (
        <ul className="card-search-menu">
          {matches.map((m, i) => (
            <li
              key={m.name}
              className={i === active ? "active" : ""}
              onMouseEnter={() => setActive(i)}
              // mousedown (not click) so it fires before the input blur closes us.
              onMouseDown={(e) => { e.preventDefault(); pick(m.name); }}
            >
              <span className="cs-name">{m.name}</span>
              {m.typeLine && <span className="cs-type">{m.typeLine}</span>}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
