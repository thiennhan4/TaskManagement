import { useState } from 'react';
import { useTaskDetail } from '@/hooks/useTaskDetail';
import { Calendar, AlertCircle, User, Edit2, Trash2, BarChart2, Clock, MessageSquare, Tag } from 'lucide-react';
import Modal from '@/components/ui/Modal';
import TaskComments from './TaskComments';
import TimeTrackingWidget from '@/components/timetracking/TimeTrackingWidget';
import { useAuth } from '@/context/authState';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import { useLanguage } from '@/context/LanguageContext';

const PRIORITY_VARIANTS = {
  High: 'danger',
  Medium: 'warning',
  Low: 'success',
};

export default function TaskModal({ isOpen, taskId, onClose, onEdit, onDelete, readOnly = false, canEditTask, canDeleteTask }) {
  const { task, error, isLoading } = useTaskDetail(isOpen ? taskId : null);
  const { user } = useAuth();
  const { t } = useLanguage();
  const [confirmDelete, setConfirmDelete] = useState(false);

  if (!isOpen) return null;
  if (!task) return <Modal isOpen={isOpen} onClose={onClose} title={t('taskModal.title')}><p role={error ? 'alert' : 'status'}>{error || (isLoading ? 'Loading task…' : 'Task unavailable')}</p></Modal>;

  const isOwner = user?.id === task.ownerId;
  const showEdit = !readOnly && (canEditTask ?? isOwner);
  const showDelete = !readOnly && (canDeleteTask ?? isOwner);
  const isOverdue = task.dueDate && new Date(task.dueDate) < new Date() && task.status !== 'Done';

  const handleDelete = () => {
    if (confirmDelete) {
      onDelete && onDelete(task.id);
      onClose();
    } else {
      setConfirmDelete(true);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={() => { setConfirmDelete(false); onClose(); }} title={t('taskModal.title')} maxWidth="max-w-2xl">
      <div className="flex flex-col h-full">
        <div className="p-8 space-y-8">
          {/* Header Area */}
          <div className="flex flex-col gap-4">
            <div className="flex items-center gap-2">
              <Badge variant={PRIORITY_VARIANTS[task.priority] || 'neutral'}>
                {t(`task.priority.${task.priority || 'Normal'}`)}
              </Badge>
              <Badge variant={task.status === 'Done' ? 'success' : 'primary'}>
                {t(`task.status.${task.status || 'Todo'}`)}
              </Badge>
              {task.label && (
                <Badge variant="neutral" className="bg-surface-2 border-none text-text-muted">
                  <Tag size={10} className="mr-1" /> {task.label}
                </Badge>
              )}
            </div>
            <h1 className={`text-2xl font-black text-text-main tracking-tight ${task.status === 'Done' ? 'line-through text-text-subtle' : ''}`}>
              {task.title}
            </h1>
          </div>

          {/* Description */}
          <div>
            <h3 className="text-xs font-bold text-text-subtle uppercase tracking-[0.2em] mb-3">{t('taskModal.description')}</h3>
            <div className="p-5 bg-surface-1 rounded-2xl border border-border-subtle">
              <p className="text-sm text-text-main font-medium leading-relaxed whitespace-pre-wrap">
                {task.description || t('taskModal.noDescription')}
              </p>
            </div>
          </div>

          {/* Meta Info Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <MetaItem 
              icon={<User size={16} />} 
              label={t('taskModal.assignedTo')} 
              value={task.assignedToName || t('taskModal.unassigned')} 
              isHighlighted={!!task.assignedToName}
            />
            <MetaItem 
              icon={<Clock size={16} />} 
              label={t('taskModal.dueDate')} 
              value={task.dueDate ? new Date(task.dueDate).toLocaleDateString() : t('taskModal.noDeadline')} 
              isDanger={isOverdue}
            />
          </div>

          {/* Time Tracking */}
          {!readOnly && <TimeTrackingWidget taskId={task.id} />}

          {/* Progress Bar */}
          {task.progress !== undefined && (
            <div className="space-y-2">
              <div className="flex justify-between items-center text-xs font-bold uppercase tracking-widest text-text-subtle">
                <span>{t('taskModal.progress')}</span>
                <span className="text-text-main">{task.progress}%</span>
              </div>
              <div className="w-full h-2 bg-surface-2 rounded-full overflow-hidden">
                <div 
                  className="h-full bg-primary transition-all duration-500"
                  style={{ width: `${task.progress}%` }}
                />
              </div>
            </div>
          )}

          {/* Action Buttons */}
          {(showEdit || showDelete) && (
            <div className="flex gap-3 pt-6 border-t border-border-subtle">
              {showEdit && <Button
                variant="outline" 
                className="flex-1" 
                leftIcon={<Edit2 size={16} />}
                onClick={() => { onEdit && onEdit(task); onClose(); }}
              >
                {t('taskModal.editTask')}
              </Button>}
              {showDelete && <Button
                variant={confirmDelete ? 'danger' : 'ghost'} 
                className={`flex-1 ${!confirmDelete ? 'text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-500/10' : ''}`}
                leftIcon={<Trash2 size={16} />}
                onClick={handleDelete}
              >
                {confirmDelete ? t('taskModal.confirmDelete') : t('taskModal.deleteTask')}
              </Button>}
            </div>
          )}
        </div>

        {/* Comments Section */}
        <div className="bg-surface-1/50 border-t border-border-subtle p-8">
          <div className="flex items-center gap-2 mb-6">
            <MessageSquare size={18} className="text-text-subtle" />
            <h3 className="text-sm font-bold text-text-main uppercase tracking-widest">{t('taskModal.discussion')}</h3>
          </div>
          <TaskComments taskId={task.id} readOnly={readOnly} />
        </div>
      </div>
    </Modal>
  );
}

function MetaItem({ icon, label, value, isHighlighted, isDanger }) {
  return (
    <div className="p-4 bg-surface-0 rounded-2xl border border-border-subtle flex items-center gap-4">
      <div className={`p-2 rounded-xl ${isDanger ? 'bg-rose-50 dark:bg-rose-500/10 text-rose-500' : 'bg-surface-1 text-text-subtle'}`}>
        {icon}
      </div>
      <div>
        <p className="text-[10px] font-bold text-text-subtle uppercase tracking-widest">{label}</p>
        <p className={`text-sm font-bold ${isDanger ? 'text-rose-500' : isHighlighted ? 'text-primary' : 'text-text-main'}`}>
          {value}
        </p>
      </div>
    </div>
  );
}
