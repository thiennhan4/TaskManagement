import Navbar from "../components/Navbar";
import { Link } from "react-router-dom";

export default function RegisterPage() {
  return (
    <div className="min-h-screen flex flex-col bg-[#e8f4f8]" style={{ fontFamily: "'Space Grotesk', sans-serif" }}>
      <Navbar />

      {/* BODY */}
      <div className="grid grid-cols-1 md:grid-cols-2 flex-1" style={{ minHeight: "calc(100vh - 64px)" }}>
        
        {/* LEFT PANEL */}
        <div
          className="flex flex-col justify-center border-r-[3px] border-[#1a1a2e] relative overflow-hidden"
          style={{ background: "#1a1a2e", padding: "clamp(40px,6vw,80px)" }}
        >
          {/* Badge Strip */}
          <div className="mb-8">
            <span
              className="inline-block font-black uppercase text-[#1a1a2e] px-8 py-2 border-2 border-[#4ecdc4]/20"
              style={{ background: "#4ecdc4", fontSize: "12px", letterSpacing: "2px" }}
            >
              START FREE TODAY
            </span>
          </div>

          <h2
            className="font-black uppercase text-white leading-[1] mb-6"
            style={{ fontSize: "clamp(32px,4vw,56px)" }}
          >
            CREATE<br />
            YOUR <span className="text-[#1a1a2e] px-2 inline-block bg-[#f7c948]">TEAM</span><br />
            WORKSPACE
          </h2>

          <p className="text-white/50 mb-10 font-medium max-w-[400px]" style={{ fontSize: "14px", lineHeight: 1.6 }}>
            Join thousands of teams already using TaskHub to manage their work effortlessly.
          </p>

          <div className="flex flex-col gap-4">
            {[
              { color: "#f7c948", label: "FREE FOREVER PLAN" },
              { color: "#4ecdc4", label: "NO CREDIT CARD NEEDED" },
              { color: "#ff6b6b", label: "SETUP IN 60 SECONDS" },
            ].map(({ color, label }) => (
              <div key={label} className="flex items-center gap-3">
                <span className="rounded-full shrink-0" style={{ width: 10, height: 10, background: color }} />
                <span className="font-black uppercase text-white/70 tracking-widest" style={{ fontSize: "11px" }}>
                  {label}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* RIGHT PANEL - FORM */}
        <div
          className="flex flex-col justify-center bg-[#e8f4f8]"
          style={{ padding: "clamp(40px,6vw,80px)" }}
        >
          <div className="max-w-[460px] w-full mx-auto">
            <h3 className="font-black uppercase text-[#1a1a2e] mb-2" style={{ fontSize: "28px", letterSpacing: "1px" }}>
              CREATE ACCOUNT
            </h3>
            <p className="text-[#718096] font-medium mb-8" style={{ fontSize: "14px" }}>
              Start managing your tasks today
            </p>

            {/* Google Button */}
            <button
              className="w-full flex items-center justify-center gap-3 font-black uppercase border-[3px] border-[#1a1a2e] bg-white hover:bg-gray-50 transition-all mb-8 py-3"
              style={{ fontSize: "12px", letterSpacing: "1px" }}
            >
              <img src="https://www.gstatic.com/firebasejs/ui/2.0.0/images/auth/google.svg" alt="Google" className="w-5 h-5" />
              SIGN UP WITH GOOGLE
            </button>

            {/* Divider */}
            <div className="flex items-center gap-4 mb-8">
              <div className="flex-1 h-[2px] bg-[#1a1a2e]/10" />
              <span className="font-black uppercase text-[#aaa] tracking-[2px]" style={{ fontSize: "10px" }}>OR WITH EMAIL</span>
              <div className="flex-1 h-[2px] bg-[#1a1a2e]/10" />
            </div>

            {/* Fields */}
            <form className="flex flex-col gap-5">
              <div>
                <label className="block font-black uppercase text-[#1a1a2e] mb-2 tracking-widest" style={{ fontSize: "11px" }}>
                  FULL NAME
                </label>
                <input
                  type="text"
                  placeholder="Your full name"
                  className="w-full bg-[#333] border-[3px] border-[#1a1a2e] text-white font-medium outline-none focus:border-[#4ecdc4] transition-all px-4 py-3 rounded-lg"
                  style={{ fontSize: "15px" }}
                />
              </div>

              <div>
                <label className="block font-black uppercase text-[#1a1a2e] mb-2 tracking-widest" style={{ fontSize: "11px" }}>
                  EMAIL
                </label>
                <input
                  type="email"
                  placeholder="you@example.com"
                  className="w-full bg-[#333] border-[3px] border-[#1a1a2e] text-white font-medium outline-none focus:border-[#4ecdc4] transition-all px-4 py-3 rounded-lg"
                  style={{ fontSize: "15px" }}
                />
              </div>

              <div>
                <label className="block font-black uppercase text-[#1a1a2e] mb-2 tracking-widest" style={{ fontSize: "11px" }}>
                  PASSWORD
                </label>
                <input
                  type="password"
                  placeholder="Min 6 characters"
                  className="w-full bg-[#333] border-[3px] border-[#1a1a2e] text-white font-medium outline-none focus:border-[#4ecdc4] transition-all px-4 py-3 rounded-lg"
                  style={{ fontSize: "15px" }}
                />
              </div>

              {/* Submit */}
              <button
                type="submit"
                className="w-full font-black uppercase border-[3px] border-[#1a1a2e] bg-[#4ecdc4] text-[#1a1a2e] hover:brightness-95 transition-all py-4 mt-4"
                style={{ fontSize: "14px", letterSpacing: "1px" }}
              >
                CREATE ACCOUNT →
              </button>
            </form>

            {/* Footer */}
            <p className="text-center text-[#718096] font-bold uppercase mt-8 tracking-widest" style={{ fontSize: "11px" }}>
              ALREADY HAVE AN ACCOUNT?{" "}
              <Link to="/login" className="text-[#4ecdc4] underline decoration-2 underline-offset-4">
                SIGN IN
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}