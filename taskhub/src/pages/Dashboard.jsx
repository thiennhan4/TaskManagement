import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { boardApi } from '../api/boardApi';
import { Plus } from 'lucide-react';

export default function Dashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [boards, setBoards] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchBoards();
  }, []);

  const fetchBoards = async () => {
    try {
      setLoading(true);
      const res = await boardApi.getBoards();
      setBoards(res.data.data);
    } catch (err) {
      console.error('Failed to fetch boards', err);
    } finally {
      setLoading(false);
    }
  };

  const createBoard = async () => {
    const title = prompt('Enter board title:');
    if (!title) return;
    
    try {
      const res = await boardApi.createBoard({ title, description: '' });
      setBoards([...boards, res.data.data]);
    } catch (err) {
      console.error('Failed to create board', err);
      alert('Failed to create board');
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 animate-fade-in">
      <div className="mb-8 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-white">
            Welcome back,{' '}
            <span className="bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              {user?.fullName}
            </span>
          </h1>
          <p className="text-gray-400 mt-2">Here are your boards</p>
        </div>
        <button 
          onClick={createBoard}
          className="flex items-center gap-2 px-6 py-2.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white text-sm font-medium rounded-xl hover:from-blue-500 hover:to-purple-500 transition-all shadow-lg shadow-blue-500/20 cursor-pointer"
        >
          <Plus size={18} />
          Create Board
        </button>
      </div>

      {loading ? (
        <div className="text-white text-center py-10">Loading boards...</div>
      ) : boards.length === 0 ? (
        <div className="bg-[#111827]/60 backdrop-blur border border-white/[0.06] rounded-2xl p-12 text-center">
          <h3 className="text-lg font-medium text-gray-300 mb-2">No boards yet</h3>
          <p className="text-sm text-gray-500 mb-6">Create your first board to start managing tasks</p>
          <button onClick={createBoard} className="px-6 py-2.5 bg-gradient-to-r from-blue-600 to-purple-600 text-white text-sm font-medium rounded-xl hover:from-blue-500 hover:to-purple-500 transition-all shadow-lg shadow-blue-500/20 cursor-pointer">
            Create Board
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {boards.map(board => (
            <div 
              key={board.id} 
              onClick={() => navigate(`/boards/${board.id}`)}
              className="bg-[#111827]/80 backdrop-blur border border-white/[0.06] rounded-2xl p-6 hover:border-white/[0.12] transition-all duration-200 group cursor-pointer"
            >
              <h3 className="text-xl font-semibold text-white mb-2">{board.title}</h3>
              <p className="text-gray-400 text-sm line-clamp-2">{board.description || 'No description'}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
