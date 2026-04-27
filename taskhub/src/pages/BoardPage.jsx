import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { DragDropContext } from '@hello-pangea/dnd';
import { boardApi } from '../api/boardApi';
import { listApi } from '../api/listApi';
import { taskApi } from '../api/taskApi';
import BoardList from '../components/Kanban/BoardList';
import { Plus, ArrowLeft } from 'lucide-react';

export default function BoardPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [board, setBoard] = useState(null);
  const [lists, setLists] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchBoardData();
  }, [id]);

  const fetchBoardData = async () => {
    try {
      setLoading(true);
      const boardRes = await boardApi.getBoardById(id);
      setBoard(boardRes.data.data);

      const listsRes = await listApi.getListsByBoard(id);
      const boardLists = listsRes.data.data || [];

      // Fetch tasks for each list
      const listsWithTasks = await Promise.all(
        boardLists.map(async (list) => {
          const tasksRes = await taskApi.getTasksByList(list.id);
          return { ...list, tasks: tasksRes.data.data || [] };
        })
      );
      
      // Sort lists if there's a position property, else keep order
      setLists(listsWithTasks.sort((a, b) => (a.position || 0) - (b.position || 0)));
    } catch (err) {
      console.error('Failed to fetch board data', err);
    } finally {
      setLoading(false);
    }
  };

  const createList = async () => {
    const name = prompt('Enter list name:');
    if (!name) return;
    
    try {
      const position = lists.length;
      const res = await listApi.createList({ name, boardId: id, position });
      setLists([...lists, { ...res.data.data, tasks: [] }]);
    } catch (err) {
      console.error('Failed to create list', err);
    }
  };

  const onDragEnd = async (result) => {
    const { destination, source, draggableId } = result;

    if (!destination) return;

    if (
      destination.droppableId === source.droppableId &&
      destination.index === source.index
    ) {
      return;
    }

    const sourceListIndex = lists.findIndex(l => l.id === source.droppableId);
    const destListIndex = lists.findIndex(l => l.id === destination.droppableId);
    
    const newLists = [...lists];
    const sourceList = newLists[sourceListIndex];
    const destList = newLists[destListIndex];
    
    const [movedTask] = sourceList.tasks.splice(source.index, 1);
    destList.tasks.splice(destination.index, 0, movedTask);
    
    setLists(newLists);

    try {
      await taskApi.moveTask(draggableId, { 
        listId: destination.droppableId, 
        position: destination.index 
      });
    } catch (err) {
      console.error('Failed to save task movement', err);
      fetchBoardData(); // Revert on failure
    }
  };

  if (loading) {
    return <div className="text-white p-8 text-center">Loading board...</div>;
  }

  if (!board) {
    return <div className="text-white p-8 text-center">Board not found.</div>;
  }

  return (
    <div className="h-[calc(100vh-4rem)] flex flex-col bg-[#080C14] animate-fade-in">
      <div className="flex items-center px-6 py-4 border-b border-white/[0.06] shrink-0">
        <button 
          onClick={() => navigate('/dashboard')}
          className="mr-4 p-2 rounded-lg text-gray-400 hover:text-white hover:bg-white/5 transition-colors"
        >
          <ArrowLeft size={20} />
        </button>
        <h1 className="text-2xl font-bold text-white">{board.title}</h1>
      </div>

      <div className="flex-1 overflow-x-auto overflow-y-hidden p-6 flex gap-6 items-start">
        <DragDropContext onDragEnd={onDragEnd}>
          {lists.map((list) => (
            <BoardList key={list.id} list={list} fetchBoardData={fetchBoardData} />
          ))}
        </DragDropContext>
        
        <button 
          onClick={createList}
          className="shrink-0 w-80 flex items-center justify-center gap-2 p-4 rounded-xl bg-[#111827]/80 backdrop-blur border border-white/[0.06] text-gray-400 hover:text-white hover:border-white/[0.12] transition-colors cursor-pointer"
        >
          <Plus size={20} />
          Add List
        </button>
      </div>
    </div>
  );
}
