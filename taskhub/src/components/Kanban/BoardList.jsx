import { Droppable } from '@hello-pangea/dnd';
import TaskCard from './TaskCard';
import { taskApi } from '../../api/taskApi';
import { Plus } from 'lucide-react';

export default function BoardList({ list, fetchBoardData }) {
  const createTask = async () => {
    const title = prompt('Enter task title:');
    if (!title) return;
    
    try {
      await taskApi.createTask(list.id, { 
        title, 
        description: '', 
        priority: 'Medium',
        dueDate: new Date().toISOString()
      });
      fetchBoardData();
    } catch (err) {
      console.error('Failed to create task', err);
    }
  };

  return (
    <div className="shrink-0 w-80 flex flex-col bg-[#111827]/80 backdrop-blur border border-white/[0.06] rounded-2xl max-h-full">
      {/* Header */}
      <div className="p-4 border-b border-white/[0.06] flex items-center justify-between shrink-0">
        <h3 className="font-semibold text-white">{list.name}</h3>
        <span className="bg-white/10 text-xs text-gray-300 py-1 px-2.5 rounded-full font-medium">
          {list.tasks?.length || 0}
        </span>
      </div>

      {/* Droppable Area */}
      <Droppable droppableId={list.id}>
        {(provided, snapshot) => (
          <div
            ref={provided.innerRef}
            {...provided.droppableProps}
            className={`flex-1 overflow-y-auto p-4 space-y-3 min-h-[150px] transition-colors ${
              snapshot.isDraggingOver ? 'bg-white/[0.02]' : ''
            }`}
          >
            {list.tasks?.map((task, index) => (
              <TaskCard key={task.id} task={task} index={index} />
            ))}
            {provided.placeholder}
          </div>
        )}
      </Droppable>

      {/* Footer */}
      <div className="p-3 border-t border-white/[0.06] shrink-0">
        <button
          onClick={createTask}
          className="w-full flex items-center justify-center gap-2 py-2 rounded-lg text-gray-400 hover:text-white hover:bg-white/5 transition-colors text-sm font-medium cursor-pointer"
        >
          <Plus size={16} />
          Add Task
        </button>
      </div>
    </div>
  );
}
