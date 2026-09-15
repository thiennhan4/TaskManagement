import Navbar from "../components/Navbar";
import { Link } from "react-router-dom";

export default function Home() {
  return (
    <div className="min-h-screen flex flex-col font-sans bg-ice overflow-x-hidden">
      <Navbar />
      
      {/* 2-Column Hero */}
      <main className="flex-1 flex flex-col md:flex-row min-h-[calc(100vh-64px)]">
        
        {/* Left Column */}
        <div className="w-full md:w-1/2 bg-ice p-fluid-lg flex flex-col justify-center border-b-[3px] md:border-b-0 md:border-r-[3px] border-navy">
          <div className="max-w-[600px] mx-auto md:mx-0 w-full">
            <div className="mb-fluid-md">
              <span className="inline-block bg-coral text-white border-[2px] border-navy font-black text-[clamp(9px,0.7vw,11px)] px-[10px] py-[3px] uppercase tracking-wider">
                ★ BETA — FREE
              </span>
            </div>
            
            <h1 className="font-black uppercase leading-[0.95] text-fluid-h1 text-navy mb-8">
              MANAGE<br />
              TASKS<br />
              EFFORT-<br />
              <span className="inline-block bg-teal text-navy border-[2px] border-navy px-[8px] py-0 -rotate-1 mt-2 text-fluid-h1 leading-[0.95]">
                LESSLY
              </span>
            </h1>

            <p className="text-[clamp(11px,0.85vw,13px)] text-graytext font-medium w-full max-w-[min(280px,90%)] mb-[clamp(14px,2vw,24px)] leading-[1.65]">
              TaskHub helps you organize, track, and complete your projects with an intuitive Kanban board and powerful collaboration tools.
            </p>

            <div className="flex flex-col sm:flex-row w-fit">
              <Link to="/register" className="bg-yellow text-navy border-[2px] border-navy font-black uppercase text-fluid-sm px-[clamp(14px,1.5vw,22px)] py-[clamp(8px,1vw,12px)] hover:brightness-90 transition-all text-center">
                GET STARTED →
              </Link>
              <Link to="/login" className="bg-ice text-navy border-[2px] sm:border-l-0 border-navy font-black uppercase text-fluid-sm px-[clamp(14px,1.5vw,22px)] py-[clamp(8px,1vw,12px)] hover:bg-gray-200 transition-all text-center border-t-0 sm:border-t-[2px]">
                SIGN IN
              </Link>
            </div>
          </div>
        </div>

        {/* Right Column */}
        <div className="w-full md:w-1/2 bg-navy p-fluid-md flex flex-col justify-center gap-[clamp(8px,1vw,12px)]">
          {/* Card 1 */}
          <div className="border-[2px] border-yellow bg-yellow/10 px-[clamp(12px,1.5vw,18px)] py-[clamp(10px,1.2vw,16px)] flex flex-col hover:bg-yellow/20 transition-colors">
            <span className="text-yellow font-black text-[clamp(18px,2vw,26px)] min-w-[clamp(28px,2.5vw,36px)] mb-3 leading-none">01</span>
            <h3 className="text-yellow font-black uppercase text-[clamp(10px,0.8vw,12px)] tracking-[0.5px] mb-2">KANBAN BOARDS</h3>
            <p className="text-white/60 text-[clamp(9px,0.7vw,11px)] font-medium leading-[1.4]">Visualize your workflow with drag-and-drop boards.</p>
          </div>
          {/* Card 2 */}
          <div className="border-[2px] border-teal bg-teal/10 px-[clamp(12px,1.5vw,18px)] py-[clamp(10px,1.2vw,16px)] flex flex-col hover:bg-teal/20 transition-colors">
            <span className="text-teal font-black text-[clamp(18px,2vw,26px)] min-w-[clamp(28px,2.5vw,36px)] mb-3 leading-none">02</span>
            <h3 className="text-teal font-black uppercase text-[clamp(10px,0.8vw,12px)] tracking-[0.5px] mb-2">REAL-TIME UPDATES</h3>
            <p className="text-white/60 text-[clamp(9px,0.7vw,11px)] font-medium leading-[1.4]">See changes instantly across your entire team.</p>
          </div>
          {/* Card 3 */}
          <div className="border-[2px] border-coral bg-coral/10 px-[clamp(12px,1.5vw,18px)] py-[clamp(10px,1.2vw,16px)] flex flex-col hover:bg-coral/20 transition-colors">
            <span className="text-coral font-black text-[clamp(18px,2vw,26px)] min-w-[clamp(28px,2.5vw,36px)] mb-3 leading-none">03</span>
            <h3 className="text-coral font-black uppercase text-[clamp(10px,0.8vw,12px)] tracking-[0.5px] mb-2">ROLE-BASED ACCESS</h3>
            <p className="text-white/60 text-[clamp(9px,0.7vw,11px)] font-medium leading-[1.4]">Control who sees and edits what with granular permissions.</p>
          </div>
        </div>
      </main>

      {/* Bottom Strip */}
      <div className="w-full bg-yellow border-t-[3px] border-navy grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 min-h-[clamp(36px,4vh,48px)]">
        {["KANBAN BOARDS", "REAL-TIME UPDATES", "TEAM COLLABORATION", "ROLE-BASED ACCESS"].map((tag, idx) => (
          <div key={idx} className={`flex items-center justify-center gap-3 border-b-[2px] lg:border-b-0 border-navy py-2 px-[clamp(12px,1.5vw,20px)] hover:bg-yellow/80 transition-colors ${idx !== 3 ? 'lg:border-r-[2px]' : ''} ${idx % 2 === 0 ? 'sm:border-r-[2px]' : ''}`}>
            <div className="w-[6px] h-[6px] bg-navy shrink-0"></div>
            <span className="font-black text-navy uppercase text-[clamp(9px,0.7vw,11px)] tracking-wider text-center">{tag}</span>
          </div>
        ))}
      </div>
    </div>
  );
}