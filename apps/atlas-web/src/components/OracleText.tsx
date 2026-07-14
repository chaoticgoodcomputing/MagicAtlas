// Oracle port highlighting — "the text is the legend."
//
// Renders a card's oracle text with each port clause underlined in its family
// hue and tagged ◂ consume / emit ▸. Optionally focusable: clicking a clause
// reports which side (consume|emit) was clicked, and the caller can dim the
// opposite side by passing `focus`.

import type { CSSProperties } from "react";
import { famHue, type OracleCard, type Side } from "../data/mock";

export function OracleText({
  oracle,
  focus = "both",
  onFocus,
}: {
  oracle: OracleCard;
  focus?: Side | "both";
  onFocus?: (side: Side) => void;
}) {
  return (
    <div className="oracle-text">
      {oracle.segs.map((seg, i) => {
        if (!seg.role || !seg.fam) return <span key={i}>{seg.t}</span>;
        const hue = famHue(seg.fam);
        const dim = focus !== "both" && focus !== seg.role;
        const style = { "--seg-hue": hue } as CSSProperties;
        const tag = seg.role === "consume" ? `◂ consume:${seg.fam}` : `emit ▸:${seg.fam}`;
        return (
          <span
            key={i}
            className={`oracle-seg port${dim ? " dim" : ""}`}
            style={style}
            onClick={onFocus ? () => onFocus(seg.role as Side) : undefined}
            role={onFocus ? "button" : undefined}
            tabIndex={onFocus ? 0 : undefined}
          >
            {seg.t}
            <sup className="port-tag">{tag}</sup>
          </span>
        );
      })}
    </div>
  );
}
