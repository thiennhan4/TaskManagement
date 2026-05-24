import PropTypes from 'prop-types';

export default function BoardCard({ board, onClick }) {
  return (
    <div 
      onClick={() => onClick(board.id)}
      className="bg-white border-[3px] border-[#1a1a2e] p-6 hover:-translate-y-2 hover:shadow-[8px_8px_0px_#1a1a2e] transition-all duration-200 group cursor-pointer flex flex-col justify-between min-h-[160px]"
    >
      <div>
        <h3 className="text-xl font-black uppercase text-[#1a1a2e] mb-3 leading-tight group-hover:text-[#4ecdc4] transition-colors">
          {board.name || 'UNTITLED BOARD'}
        </h3>
        <p className="text-[#1a1a2e]/60 text-sm font-bold tracking-wide line-clamp-2">
          Workspace Board
        </p>
      </div>
      <div className="mt-6 flex justify-end">
        <span className="w-8 h-8 bg-[#f7c948] border-[2px] border-[#1a1a2e] rounded-full flex items-center justify-center group-hover:scale-110 transition-transform">
          <span className="font-black text-[#1a1a2e] text-xs">→</span>
        </span>
      </div>
    </div>
  );
}

BoardCard.propTypes = {
  board: PropTypes.shape({
    id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
    name: PropTypes.string,
  }).isRequired,
  onClick: PropTypes.func.isRequired,
};
