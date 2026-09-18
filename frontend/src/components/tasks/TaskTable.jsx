import React from 'react';
import { useTasks } from '../../context/TaskContext';
import { StatusBadge } from './StatusBadge';
import { PriorityBadge } from './PriorityBadge';
import { User, Calendar, MessageSquare, Paperclip, MoreVertical } from 'lucide-react';

export const TaskTable = () => {
  const { tasks, openDetail } = useTasks();

  return (
    <div className="bg-surface-0 rounded-2xl border border-border-subtle overflow-hidden shadow-sm animate-in fade-in duration-300">
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-surface-2/50 border-b border-border-subtle">
              <th className="px-6 py-4 text-xs font-bold text-text-subtle uppercase tracking-wider">Task</th>
              <th className="px-6 py-4 text-xs font-bold text-text-subtle uppercase tracking-wider">Status</th>
              <th className="px-6 py-4 text-xs font-bold text-text-subtle uppercase tracking-wider">Priority</th>
              <th className="px-6 py-4 text-xs font-bold text-text-subtle uppercase tracking-wider">Assignee</th>
              <th className="px-6 py-4 text-xs font-bold text-text-subtle uppercase tracking-wider">Due Date</th>
              <th className="px-6 py-4 text-xs font-bold text-text-subtle uppercase tracking-wider"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {tasks.map((task) => (
              <tr 
                key={task.id}
                onClick={() => openDetail(task)}
                className="hover:bg-hover-bg transition-colors cursor-pointer group"
              >
                <td className="px-6 py-4">
                  <div className="flex flex-col">
                    <span className="text-sm font-bold text-text-main group-hover:text-primary transition-colors">{task.title}</span>
                    <div className="flex items-center gap-3 mt-1 text-text-subtle">
                      <div className="flex items-center gap-1 text-[10px] font-medium">
                        <MessageSquare className="w-3 h-3" />
                        <span>{task.commentsCount || 0}</span>
                      </div>
                      <div className="flex items-center gap-1 text-[10px] font-medium">
                        <Paperclip className="w-3 h-3" />
                        <span>{task.attachmentsCount || 0}</span>
                      </div>
                    </div>
                  </div>
                </td>
                <td className="px-6 py-4">
                  <StatusBadge status={task.status} />
                </td>
                <td className="px-6 py-4">
                  <PriorityBadge priority={task.priority} />
                </td>
                <td className="px-6 py-4">
                  {task.assignedToName ? (
                    <div className="flex items-center gap-2">
                      <div className="w-6 h-6 rounded-full bg-primary/10 flex items-center justify-center text-[10px] font-bold text-primary">
                        {task.assignedToName.charAt(0)}
                      </div>
                      <span className="text-sm font-medium text-text-main">{task.assignedToName}</span>
                    </div>
                  ) : (
                    <span className="text-xs text-text-subtle italic">Unassigned</span>
                  )}
                </td>
                <td className="px-6 py-4">
                  <div className={`flex items-center gap-2 text-xs font-medium ${task.isOverdue ? 'text-rose-500' : 'text-text-muted'}`}>
                    <Calendar className="w-3.5 h-3.5" />
                    <span>{task.dueDate ? new Date(task.dueDate).toLocaleDateString() : '—'}</span>
                  </div>
                </td>
                <td className="px-6 py-4 text-right">
                  <button className="p-2 text-text-subtle hover:text-text-main rounded-lg hover:bg-hover-bg transition-all opacity-0 group-hover:opacity-100">
                    <MoreVertical className="w-4 h-4" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
