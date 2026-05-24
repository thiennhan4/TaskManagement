import { useState } from 'react';
import { Droppable } from '@hello-pangea/dnd';
import { Plus } from 'lucide-react';
import TaskCard from '@/components/tasks/TaskCard';
import TaskFormModal from '@/components/tasks/TaskFormModal';
import { taskApi } from '@/api/taskApi';
import toast from 'react-hot-toast';

export default function KanbanColumn({ list, fetchBoardData, onTaskClick }) {
  const [showForm, setShowForm] = useState(false);

  const handleCreateTask = async (formData) => {
    await taskApi.createTask(list.id, formData);
    toast.success('Task created!');
    fetchBoardData();
  };

  return (
    <div className="shrink-0 w-80 flex flex-col bg-white border-[3px] border-[#1a1a2e] shadow-[8px_8px_0px_#1a1a2e] max-h-full">
      {/* Header */}
      <div className="p-4 border-b-[3px] border-[#1a1a2e] bg-[#f7c948] flex items-center justify-between shrink-0">
        <h3 className="font-black uppercase text-[#1a1a2e] tracking-widest">{list.name}</h3>
        <span className="bg-white border-[2px] border-[#1a1a2e] text-[#1a1a2e] text-xs py-1 px-3 font-black uppercase shadow-[2px_2px_0px_#1a1a2e]">
          {list.tasks?.length || 0}
        </span>
      </div>

      {/* Droppable Area */}
      <Droppable droppableId={String(list.id)}>
        {(provided, snapshot) => (
          <div
            ref={provided.innerRef}
            {...provided.droppableProps}
            className={`flex-1 overflow-y-auto p-4 space-y-4 min-h-[120px] transition-colors ${
              snapshot.isDraggingOver ? 'bg-[#4ecdc4]/10' : ''
            }`}
          >
            {list.tasks?.map((task, index) => (
              <TaskCard key={task.id} task={task} index={index} onClick={onTaskClick} />
            ))}
            {provided.placeholder}
          </div>
        )}
      </Droppable>

      {/* Footer — Add Task button */}
      <div className="p-3 border-t-[3px] border-[#1a1a2e] shrink-0 bg-[#e8f4f8]">
        <button
          onClick={() => setShowForm(true)}
          className="w-full flex items-center justify-center gap-2 py-3 bg-white border-[3px] border-[#1a1a2e] text-[#1a1a2e] font-black uppercase text-xs tracking-widest hover:bg-[#ff6b6b] hover:text-white hover:-translate-y-1 hover:shadow-[4px_4px_0px_#1a1a2e] transition-all cursor-pointer"
        >
          <Plus size={18} strokeWidth={3} />
          ADD TASK
        </button>
      </div>

      {/* Task Create Modal */}
      <TaskFormModal
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onSubmit={handleCreateTask}
        listId={list.id}
      />
    </div>
  );
}
