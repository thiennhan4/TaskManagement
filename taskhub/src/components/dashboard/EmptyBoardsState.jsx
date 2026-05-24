import PropTypes from 'prop-types';
import { Plus } from 'lucide-react';

export default function EmptyBoardsState({ onCreateBoard }) {
  return (
    <div className="border-[3px] border-[#1a1a2e] bg-white p-12 text-center shadow-[8px_8px_0px_#1a1a2e] flex flex-col items-center">
      <div className="w-16 h-16 bg-[#f7c948] border-[3px] border-[#1a1a2e] rounded-full flex items-center justify-center mb-6">
        <Plus className="text-[#1a1a2e] w-8 h-8" strokeWidth={3} />
      </div>
      <h3 className="font-black uppercase text-[#1a1a2e] text-2xl mb-2">NO BOARDS YET</h3>
      <p className="text-[#1a1a2e]/60 font-bold uppercase tracking-widest text-sm mb-8 max-w-md">
        Create your first board to start managing tasks with your team
      </p>
      <button 
        onClick={onCreateBoard} 
        className="inline-block px-8 py-4 bg-[#ff6b6b] text-[#1a1a2e] font-black uppercase text-sm tracking-widest border-[3px] border-[#1a1a2e] hover:-translate-y-1 hover:shadow-[4px_4px_0px_#1a1a2e] transition-all cursor-pointer"
      >
        CREATE FIRST BOARD →
      </button>
    </div>
  );
}

EmptyBoardsState.propTypes = {
  onCreateBoard: PropTypes.func.isRequired,
};
