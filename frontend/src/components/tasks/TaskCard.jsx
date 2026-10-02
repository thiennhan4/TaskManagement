import { PriorityBadge } from '@/components/tasks/PriorityBadge';
import { StatusBadge } from '@/components/tasks/StatusBadge';
import React from 'react';
import { Draggable } from '@hello-pangea/dnd';
import { MoreHorizontal, Clock, MessageSquare, CheckCircle2, Circle } from 'lucide-react';
import Badge from '@/components/ui/Badge';
import { useLanguage } from '@/context/LanguageContext';



const TaskCard = ({ task, index, onClick, onToggleStatus, readOnly = false }) => {
  const { t } = useLanguage();
  const isOverdue = task.dueDate && new Date(task.dueDate) < new Date() && task.status !== 'Done';

  const handleToggle = (e) => {
    e.stopPropagation();
    if (onToggleStatus) onToggleStatus(task);
  };

  return (
    <Draggable draggableId={String(task.id)} index={index} isDragDisabled={readOnly}>
      {(provided, snapshot) => (
        <div
          ref={provided.innerRef}
          {...provided.draggableProps}
          {...provided.dragHandleProps}
          onClick={() => onClick && onClick(task)}
          className={`
            premium-card p-4 mb-3 group cursor-grab active:cursor-grabbing select-none
            ${snapshot.isDragging ? 'shadow-2xl ring-2 ring-primary/20 scale-[1.02] rotate-1' : ''}
            ${task.status === 'Done' ? 'opacity-75' : ''}
          `}
        >
          {/* Header: Label & Actions */}
          <div className="flex items-center justify-between mb-3">
            <div className="flex gap-2 items-center flex-wrap">
              {!readOnly && <button
                onClick={handleToggle}
                className="transition-transform active:scale-90"
                title={task.status === 'Done' ? t('task.markTodo') : t('task.markDone')}
              >
                {task.status === 'Done' ? (
                  <CheckCircle2 className="w-5 h-5 text-emerald-500" />
                ) : (
                  <Circle className="w-5 h-5 text-text-subtle hover:text-primary transition-colors" />
                )}
              </button>}
              {task.label && (
                <span className="px-2 py-0.5 rounded-md bg-surface-2 text-[10px] font-bold text-text-muted uppercase tracking-wider">
                  {task.label}
                </span>
              )}
<PriorityBadge priority={task.priority} />
<StatusBadge status={task.status} />
            </div>

          </div>

          {/* Body: Title & Description */}
          <h4 className={`text-sm font-bold text-text-main mb-1 leading-snug ${task.status === 'Done' ? 'line-through decoration-text-subtle text-text-subtle' : ''}`}>
            {task.title}
          </h4>
          {task.description && (
            <p className="text-xs text-text-muted line-clamp-2 mb-4 leading-relaxed">
              {task.description}
            </p>
          )}

          {/* Footer: Metadata & Assignee */}
          <div className="flex items-center justify-between pt-3 border-t border-border-subtle">
            <div className="flex items-center gap-3 text-text-muted">
              {task.dueDate && (
                <div className={`flex items-center gap-1 text-[10px] font-semibold ${isOverdue ? 'text-rose-500' : ''}`}>
                  <Clock size={12} />
                  {new Date(task.dueDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
                </div>
              )}
              {task.commentsCount > 0 && (
                <div className="flex items-center gap-1 text-[10px] font-semibold">
                  <MessageSquare size={12} />
                  {task.commentsCount}
                </div>
              )}
            </div>

            {task.assignedToName && (
              <div 
                className="w-6 h-6 rounded-full bg-primary/10 border border-surface-0 flex items-center justify-center text-[10px] font-bold text-primary ring-2 ring-surface-1"
                title={t('task.assignedTo', { name: task.assignedToName })}
              >
                {task.assignedToName.charAt(0).toUpperCase()}
              </div>
            )}
          </div>
        </div>
      )}
    </Draggable>
  );
};

export default TaskCard;
