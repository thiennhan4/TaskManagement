import React from 'react';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';
import { useTasks } from '../../context/TaskContext';
import { TaskCard } from './TaskCard';
import { Plus, MoreHorizontal } from 'lucide-react';
import * as taskService from '../../services/taskService';
import { toast } from 'react-hot-toast';

const COLUMNS = [
  { id: 'Todo', title: 'To Do', color: 'bg-blue-500' },
  { id: 'InProgress', title: 'In Progress', color: 'bg-yellow-500' },
  { id: 'Review', title: 'Review', color: 'bg-purple-500' },
  { id: 'Done', title: 'Done', color: 'bg-green-500' }
];

export const TaskKanbanBoard = () => {
  const { tasks, setTasks, openDetail, setShowEditModal, setShowCreateModal } = useTasks();

  const onDragEnd = async (result) => {
    const { destination, source, draggableId } = result;

    if (!destination) return;
    if (destination.droppableId === source.droppableId && destination.index === source.index) return;

    // Optimistic Update
    const updatedTasks = [...tasks];
    const taskIndex = updatedTasks.findIndex(t => t.id === draggableId);
    if (taskIndex === -1) return;

    const oldStatus = updatedTasks[taskIndex].status;
    const newStatus = destination.droppableId;
    
    updatedTasks[taskIndex] = { ...updatedTasks[taskIndex], status: newStatus };
    setTasks(updatedTasks);

    try {
      const apiResult = await taskService.changeStatus(draggableId, newStatus);
      if (!apiResult.success) {
        throw new Error();
      }
      toast.success(`Moved to ${newStatus}`);
    } catch (err) {
      // Revert on error
      const revertedTasks = [...tasks];
      revertedTasks[taskIndex] = { ...revertedTasks[taskIndex], status: oldStatus };
      setTasks(revertedTasks);
      toast.error('Failed to update status');
    }
  };

  return (
    <DragDropContext onDragEnd={onDragEnd}>
      <div className="flex gap-6 overflow-x-auto pb-6 min-h-[calc(100vh-250px)] custom-scrollbar">
        {COLUMNS.map((column) => {
          const columnTasks = tasks.filter(t => t.status === column.id);

          return (
            <div key={column.id} className="flex-shrink-0 w-80 flex flex-col">
              {/* Column Header */}
              <div className="flex items-center justify-between mb-4 px-1">
                <div className="flex items-center gap-2">
                  <div className={`w-2 h-2 rounded-full ${column.color}`} />
                  <h3 className="font-bold text-text-main">{column.title}</h3>
                  <span className="px-2 py-0.5 bg-surface-2 text-text-muted text-[10px] font-bold rounded-full">
                    {columnTasks.length}
                  </span>
                </div>
                <button className="text-text-subtle hover:text-text-main p-1 rounded-md hover:bg-hover-bg transition-all">
                  <MoreHorizontal className="w-4 h-4" />
                </button>
              </div>

              {/* Droppable Area */}
              <Droppable droppableId={column.id}>
                {(provided, snapshot) => (
                  <div
                    {...provided.droppableProps}
                    ref={provided.innerRef}
                    className={`
                      flex-1 flex flex-col gap-4 p-2 rounded-2xl transition-all duration-300
                      ${snapshot.isDraggingOver ? 'bg-primary/10 ring-2 ring-primary/20 ring-inset' : 'bg-transparent'}
                    `}
                  >
                    {columnTasks.map((task, index) => (
                      <Draggable key={task.id} draggableId={task.id} index={index}>
                        {(provided, snapshot) => (
                          <div
                            ref={provided.innerRef}
                            {...provided.draggableProps}
                            {...provided.dragHandleProps}
                            className={`${snapshot.isDragging ? 'z-[1000]' : ''}`}
                          >
                            <TaskCard 
                              task={task} 
                              onClick={() => openDetail(task)}
                              onEdit={() => setShowEditModal(true)}
                              onDelete={() => {}} 
                            />
                          </div>
                        )}
                      </Draggable>
                    ))}
                    {provided.placeholder}
                    
                    <button
                      onClick={() => setShowCreateModal(true)}
                      className="w-full py-3 flex items-center justify-center gap-2 text-sm font-bold text-text-subtle border-2 border-dashed border-border-subtle rounded-xl hover:border-primary/40 hover:text-primary hover:bg-surface-0 transition-all group"
                    >
                      <Plus className="w-4 h-4 group-hover:scale-110 transition-transform" />
                      <span>Add Task</span>
                    </button>
                  </div>
                )}
              </Droppable>
            </div>
          );
        })}
      </div>
    </DragDropContext>
  );
};
