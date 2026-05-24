import PropTypes from 'prop-types';
import BoardCard from './BoardCard';

export default function BoardsGrid({ boards, onBoardClick }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {boards.map(board => (
        <BoardCard 
          key={board.id} 
          board={board} 
          onClick={onBoardClick} 
        />
      ))}
    </div>
  );
}

BoardsGrid.propTypes = {
  boards: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
    })
  ).isRequired,
  onBoardClick: PropTypes.func.isRequired,
};
