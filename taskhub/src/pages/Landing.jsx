import Navbar from "../components/Navbar";

export default function LandingPage() {
  return (
    <div className="min-h-screen flex flex-col" style={{ background: "#e8f4f8", fontFamily: "'Space Grotesk', sans-serif" }}>

      <Navbar />

      {/* ── HERO ── */}
      <div className="grid grid-cols-2 flex-1" style={{ minHeight: "calc(100vh - 64px - 44px)" }}>

        {/* LEFT */}
        <div
          className="flex flex-col justify-center border-r-[3px] border-[#1a1a2e]"
          style={{ background: "#e8f4f8", padding: "clamp(24px,3.5vw,52px)" }}
        >
          {/* Badge */}
          <span
            className="inline-block font-black uppercase border-2 border-[#1a1a2e] text-white mb-4 w-fit"
            style={{ background: "#ff6b6b", fontSize: "clamp(8px,0.65vw,10px)", letterSpacing: "2px", padding: "3px 10px" }}
          >
            ★ BETA — FREE
          </span>

          {/* H1 */}
          <h1
            className="font-black uppercase text-[#1a1a2e] leading-[0.95] mb-4"
            style={{ fontSize: "clamp(30px,4.5vw,60px)" }}
          >
            MANAGE<br />
            TASKS<br />
            EFFORT-<br />
            <span
              className="inline-block text-[#1a1a2e] border-2 border-[#1a1a2e] -rotate-1"
              style={{ background: "#4ecdc4", padding: "0 8px" }}
            >
              LESSLY
            </span>
          </h1>

          {/* Description */}
          <p
            className="text-[#4a5568] font-medium leading-relaxed mb-5"
            style={{ fontSize: "clamp(11px,0.85vw,13px)", maxWidth: "min(300px,90%)" }}
          >
            TaskHub helps you organize, track, and complete your projects with an intuitive Kanban board and powerful collaboration tools.
          </p>

          {/* CTAs */}
          <div className="flex">
            <a
              href="/register"
              className="font-black uppercase border-2 border-[#1a1a2e] hover:brightness-90 transition-all"
              style={{ background: "#f7c948", color: "#1a1a2e", fontSize: "clamp(9px,0.8vw,12px)", letterSpacing: "1px", padding: "clamp(8px,1vw,12px) clamp(14px,1.5vw,20px)" }}
            >
              GET STARTED →
            </a>
            <a
              href="/login"
              className="font-bold uppercase border-2 border-l-0 border-[#1a1a2e] hover:bg-[#d8ecf2] transition-all"
              style={{ background: "#e8f4f8", color: "#1a1a2e", fontSize: "clamp(9px,0.8vw,12px)", letterSpacing: "1px", padding: "clamp(8px,1vw,12px) clamp(14px,1.5vw,20px)" }}
            >
              SIGN IN
            </a>
          </div>
        </div>

        {/* RIGHT */}
        <div
          className="flex flex-col justify-center gap-[clamp(8px,1vw,12px)]"
          style={{ background: "#1a1a2e", padding: "clamp(16px,2.5vw,32px)" }}
        >
          {[
            { num: "01", color: "#f7c948", title: "KANBAN BOARDS", desc: "Drag-drop workflows built for speed and clarity." },
            { num: "02", color: "#4ecdc4", title: "REAL-TIME SYNC",  desc: "Instant updates across all team members." },
            { num: "03", color: "#ff6b6b", title: "ROLE ACCESS",     desc: "Granular permissions for every team role." },
          ].map(({ num, color, title, desc }) => (
            <div
              key={num}
              className="flex items-start gap-3 border-2"
              style={{
                borderColor: color,
                background: `${color}14`,
                padding: "clamp(10px,1.2vw,16px) clamp(12px,1.5vw,18px)",
              }}
            >
              <span className="font-black leading-none shrink-0" style={{ color, fontSize: "clamp(18px,2vw,26px)", minWidth: "clamp(28px,2.5vw,36px)" }}>
                {num}
              </span>
              <div>
                <div className="font-black uppercase mb-1" style={{ color, fontSize: "clamp(9px,0.75vw,11px)", letterSpacing: "0.5px" }}>
                  {title}
                </div>
                <div style={{ color: "rgba(255,255,255,0.4)", fontSize: "clamp(9px,0.7vw,11px)", lineHeight: 1.4 }}>
                  {desc}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* ── BOTTOM STRIP ── */}
      <div
        className="flex items-stretch border-t-[3px] border-[#1a1a2e]"
        style={{ background: "#f7c948", height: "clamp(36px,4vh,48px)" }}
      >
        {["KANBAN BOARDS", "REAL-TIME UPDATES", "TEAM COLLABORATION", "ROLE-BASED ACCESS"].map((t, i) => (
          <div
            key={t}
            className="flex items-center gap-2 font-black uppercase text-[#1a1a2e] flex-1 justify-center"
            style={{
              fontSize: "clamp(8px,0.65vw,10px)",
              letterSpacing: "1.5px",
              borderRight: i < 3 ? "2px solid #1a1a2e" : "none",
            }}
          >
            <span className="w-[6px] h-[6px] rounded-full bg-[#1a1a2e] shrink-0" />
            {t}
          </div>
        ))}
      </div>
    </div>
  );
}