import PropTypes from 'prop-types';
import { Plus } from 'lucide-react';

export default function DashboardHeader({ userFullName, onCreateBoard }) {
  return (
    <div className="mb-10 flex flex-col sm:flex-row justify-between items-start sm:items-end gap-6">
      <div>
        <h1 className="font-black uppercase text-[#1a1a2e] mb-2 leading-none" style={{ fontSize: "clamp(32px, 5vw, 48px)" }}>
          WELCOME BACK,<br />
          <span className="text-[#1a1a2e] px-2 inline-block bg-[#f7c948]">
            {userFullName || 'USER'}
          </span>
        </h1>
        <p className="text-[#1a1a2e]/70 font-bold uppercase tracking-widest text-sm mt-4">
          Here are your active boards
        </p>
      </div>
      <button 
        onClick={onCreateBoard}
        className="flex items-center gap-2 px-8 py-4 bg-[#4ecdc4] text-[#1a1a2e] text-sm font-black uppercase tracking-widest border-[3px] border-[#1a1a2e] hover:-translate-y-1 hover:shadow-[4px_4px_0px_#1a1a2e] transition-all cursor-pointer"
      >
        <Plus size={20} strokeWidth={3} />
        CREATE BOARD
      </button>
    </div>
  );
}

DashboardHeader.propTypes = {
  userFullName: PropTypes.string,
  onCreateBoard: PropTypes.func.isRequired,
};
