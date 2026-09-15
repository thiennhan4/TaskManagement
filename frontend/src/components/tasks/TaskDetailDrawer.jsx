import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  X,
  Calendar,
  Flag,
  User,
  Layout,
  CheckCircle2,
  Clock,
  MoreHorizontal,
  ChevronRight,
  MessageSquare,
  Paperclip
} from 'lucide-react';
import { format } from 'date-fns';
import { useLanguage } from '@/context/LanguageContext';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';
import { PriorityBadge } from './PriorityBadge';
import { StatusBadge } from './StatusBadge';

const TaskDetailDrawer = ({ isOpen, onClose, task }) => {
  const { t } = useLanguage();
  if (!task) return null;

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 bg-slate-900/20 backdrop-blur-[2px] z-[110]"
          />

          {/* Drawer */}
          <motion.div
            initial={{ x: '100%' }}
            animate={{ x: 0 }}
            exit={{ x: '100%' }}
            transition={{ type: 'spring', damping: 25, stiffness: 200 }}
            className="fixed right-0 top-0 h-full w-full max-w-lg bg-surface-0 shadow-2xl z-[120] overflow-hidden flex flex-col transition-colors"
          >
            {/* Header */}
            <div className="flex items-center justify-between p-6 border-b border-border-subtle">
              <div className="flex items-center gap-2">
                <Badge variant="outline" className="text-[10px] font-black uppercase tracking-widest">
                  {task.projectName || 'General'}
                </Badge>
                <ChevronRight size={14} className="text-text-subtle" />
                <span className="text-xs font-bold text-text-muted">{task.boardName}</span>
              </div>
              <div className="flex items-center gap-2">
                <button className="p-2 hover:bg-hover-bg rounded-xl text-text-muted hover:text-text-main transition-colors">
                  <MoreHorizontal size={20} />
                </button>
                <button
                  onClick={onClose}
                  className="p-2 hover:bg-hover-bg rounded-xl text-text-muted hover:text-text-main transition-colors"
                >
                  <X size={20} />
                </button>
              </div>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-8 custom-scrollbar">
              <div className="space-y-8">
                {/* Title & Status */}
                <div className="space-y-4">
                  <div className="flex items-center gap-3">
                    <StatusBadge status={task.status} />
                    <PriorityBadge priority={task.priority} />
                  </div>
                  <h1 className="text-2xl font-black text-text-main leading-tight">
                    {task.title}
                  </h1>
                </div>

                {/* Metadata Grid */}
                <div className="grid grid-cols-2 gap-y-6 gap-x-4 bg-surface-2/50 p-6 rounded-3xl border border-border-subtle transition-colors">
                  <div className="space-y-1.5">
                    <label className="text-[10px] font-black uppercase tracking-widest text-text-subtle flex items-center gap-1.5">
                      <User size={12} /> {t('taskModal.assignedTo')}
                    </label>
                    <div className="flex items-center gap-2">
                      <div className="w-6 h-6 rounded-full bg-primary/10 flex items-center justify-center text-[10px] font-bold text-primary border border-primary/20">
                        {task.assignedToName?.charAt(0) || 'U'}
                      </div>
                      <span className="text-sm font-bold text-text-main">
                        {task.assignedToName || t('taskModal.unassigned')}
                      </span>
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="text-[10px] font-black uppercase tracking-widest text-text-subtle flex items-center gap-1.5">
                      <Calendar size={12} /> {t('taskModal.dueDate')}
                    </label>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-bold text-text-main">
                        {task.dueDate ? format(new Date(task.dueDate), 'MMM dd, yyyy') : t('taskModal.noDeadline')}
                      </span>
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="text-[10px] font-black uppercase tracking-widest text-text-subtle flex items-center gap-1.5">
                      <Clock size={12} /> {t('dashboard.createdAt').replace(' {date}', '')}
                    </label>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-bold text-text-main">
                        {task.createdAt ? format(new Date(task.createdAt), 'MMM dd, yyyy') : 'Recently'}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Description */}
                <div className="space-y-3">
                  <label className="text-[10px] font-black uppercase tracking-widest text-text-subtle">
                    {t('taskModal.description')}
                  </label>
                  <p className="text-sm text-text-muted leading-relaxed font-medium">
                    {task.description || t('taskModal.noDescription')}
                  </p>
                </div>

                {/* Quick Actions / Tabs Placeholder */}
                <div className="flex gap-4 border-b border-border-subtle pb-2">
                  <button className="text-xs font-bold text-primary border-b-2 border-primary pb-2 flex items-center gap-2">
                    <MessageSquare size={14} /> {t('taskModal.discussion')}
                  </button>
                  <button className="text-xs font-bold text-text-muted hover:text-text-main pb-2 flex items-center gap-2">
                    <Paperclip size={14} /> Attachments
                  </button>
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-6 border-t border-border-subtle bg-surface-1/50 flex items-center justify-between transition-colors">
              <Button variant="outline" size="sm" onClick={onClose}>{t('common.cancel')}</Button>
              <div className="flex items-center gap-2">
                <Button size="sm" variant="primary">{t('task.markDone')}</Button>
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
};

export default TaskDetailDrawer;
