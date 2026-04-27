import { Draggable } from '@hello-pangea/dnd';
import { Calendar, AlertCircle } from 'lucide-react';

export default function TaskCard({ task, index }) {
  const getPriorityColor = (priority) => {
    switch (priority?.toLowerCase()) {
      case 'high': return 'text-red-400 bg-red-400/10 border-red-400/20';
      case 'medium': return 'text-yellow-400 bg-yellow-400/10 border-yellow-400/20';
      case 'low': return 'text-blue-400 bg-blue-400/10 border-blue-400/20';
      default: return 'text-gray-400 bg-gray-400/10 border-gray-400/20';
    }
  };

  return (
    <Draggable draggableId={task.id} index={index}>
      {(provided, snapshot) => (
        <div
          ref={provided.innerRef}
          {...provided.draggableProps}
          {...provided.dragHandleProps}
          className={`bg-[#1F2937]/90 border border-white/[0.08] rounded-xl p-4 shadow-sm group hover:border-white/[0.15] transition-all ${
            snapshot.isDragging ? 'shadow-2xl shadow-black/50 rotate-2 border-blue-500/50' : ''
          }`}
        >
          <div className="flex justify-between items-start mb-2">
            <h4 className="text-white font-medium text-sm leading-tight group-hover:text-blue-400 transition-colors">
              {task.title}
            </h4>
          </div>
          
          {task.description && (
            <p className="text-gray-400 text-xs mb-4 line-clamp-2">
              {task.description}
            </p>
          )}

          <div className="flex items-center justify-between mt-4">
            <div className={`text-[10px] px-2 py-1 rounded border font-medium ${getPriorityColor(task.priority)} flex items-center gap-1`}>
              <AlertCircle size={10} />
              {task.priority || 'No Priority'}
            </div>
            
            {task.dueDate && (
              <div className="flex items-center gap-1 text-gray-400 text-xs">
                <Calendar size={12} />
                <span>{new Date(task.dueDate).toLocaleDateString()}</span>
              </div>
            )}
          </div>
        </div>
      )}
    </Draggable>
  );
}
