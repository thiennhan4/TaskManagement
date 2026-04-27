import Tag from "./Tag";

export default function Hero() {
  return (
    <section className="relative flex items-center justify-center min-h-[90vh] pt-24 px-6 text-center">
      <div 
        className="absolute inset-0 pointer-events-none" 
        style={{ background: 'radial-gradient(ellipse 80% 50% at 50% 40%, rgba(139,92,246,0.15) 0%, transparent 70%)' }}
      ></div>

      <div className="max-w-5xl mx-auto relative z-10">
        
        {/* Badge */}
        <div className="mb-6">
          <span className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-violet-500/20 border border-violet-500/30 text-violet-300 text-sm">
            Now in beta
          </span>
        </div>

        {/* Title */}
        <h1 className="text-6xl md:text-7xl font-black text-white mb-4 leading-tight font-['Syne']">
          Manage tasks <br />
          <span className="bg-gradient-to-r from-blue-400 via-violet-400 to-pink-400 bg-clip-text text-transparent">
            effortlessly
          </span>
        </h1>

        {/* Description */}
        <p className="text-gray-400 text-lg max-w-xl mx-auto mb-8 leading-relaxed">
          TaskHub helps you organize, track, and complete your projects
          with an intuitive Kanban board and powerful collaboration tools.
        </p>

        {/* Buttons */}
        <div className="flex items-center justify-center gap-4 mb-8">
          <button className="px-6 py-3 bg-violet-600 hover:bg-violet-500 text-white rounded-lg font-semibold flex items-center gap-2 transition-all">
            Get Started Free
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
          </button>

          <button className="px-6 py-3 text-gray-300 hover:text-white transition-colors font-medium">
            Sign In
          </button>
        </div>

        {/* Tags */}
        <div className="flex items-center justify-center gap-6 flex-wrap">
          <Tag text="Kanban Boards" />
          <Tag text="Real-time Updates" />
          <Tag text="Team Collaboration" />
          <Tag text="Role-based Access" />
        </div>

      </div>
    </section>
  );
}