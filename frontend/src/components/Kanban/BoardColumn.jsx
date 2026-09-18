import React, { useState } from 'react';
import { Droppable } from '@hello-pangea/dnd';
import { Plus, MoreVertical } from 'lucide-react';
import TaskCard from '../tasks/TaskCard';
import Button from '../ui/Button';
import TaskFormModal from '../tasks/TaskFormModal';
import { taskApi } from '@/api/taskApi';
import toast from 'react-hot-toast';

const BoardColumn = ({ column, tasks, fetchBoardData, onTaskClick }) => {
  const [showTaskForm, setShowTaskForm] = useState(false);

  const handleCreateTask = async (formData) => {
    try {
      await taskApi.createTask(column.id, formData);
      toast.success('Task created successfully');
      setShowTaskForm(false);
      fetchBoardData();
    } catch (err) {
      toast.error('Failed to create task');
      console.error(err);
    }
  };

  const handleToggleStatus = async (task) => {
    const newStatus = task.status === 'Done' ? 'Todo' : 'Done';
    try {
      await taskApi.updateTask(task.id, { ...task, status: newStatus });
      toast.success(`Task marked as ${newStatus}`);
      fetchBoardData();
    } catch (err) {
      toast.error('Failed to update task status');
      console.error(err);
    }
  };

  return (
    <div className="flex flex-col w-[320px] min-w-[320px] h-full">
      {/* Column Header */}
      <div className="flex items-center justify-between mb-4 px-2">
        <div className="flex items-center gap-2">
          <h3 className="text-sm font-bold text-text-main uppercase tracking-wider">
            {column.name}
          </h3>
          <span className="bg-surface-2 text-text-muted text-[10px] font-bold px-1.5 py-0.5 rounded-md">
            {tasks?.length || 0}
          </span>
        </div>
        <div className="flex items-center gap-1">
          <Button 
            variant="ghost" 
            size="icon" 
            className="h-8 w-8"
            onClick={() => setShowTaskForm(true)}
          >
            <Plus size={16} />
          </Button>
          <Button variant="ghost" size="icon" className="h-8 w-8">
            <MoreVertical size={16} />
          </Button>
        </div>
      </div>

      {/* Droppable Area */}
      <Droppable droppableId={String(column.id)}>
        {(provided, snapshot) => (
          <div
            ref={provided.innerRef}
            {...provided.droppableProps}
            className={`
              flex-1 rounded-2xl p-2 transition-colors duration-200 overflow-y-auto custom-scrollbar
              ${snapshot.isDraggingOver ? 'bg-surface-2' : 'bg-surface-1/50'}
            `}
            style={{ minHeight: '150px' }}
          >
            {tasks?.map((task, index) => (
              <TaskCard 
                key={task.id} 
                task={task} 
                index={index} 
                onClick={onTaskClick}
                onToggleStatus={handleToggleStatus}
              />
            ))}
            {provided.placeholder}
            
            {/* Add Task Button at bottom of column */}
            <button 
              onClick={() => setShowTaskForm(true)}
              className="w-full py-3 mt-2 border-2 border-dashed border-border-subtle rounded-xl text-text-subtle text-xs font-bold hover:border-primary hover:text-primary hover:bg-surface-0 transition-all flex items-center justify-center gap-2"
            >
              <Plus size={14} /> Add Task
            </button>
          </div>
        )}
      </Droppable>

      <TaskFormModal
        isOpen={showTaskForm}
        onClose={() => setShowTaskForm(false)}
        onSubmit={handleCreateTask}
        listId={column.id}
      />
    </div>
  );
};

export default BoardColumn;
