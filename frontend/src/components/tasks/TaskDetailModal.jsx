import React, { useCallback, useState, useEffect } from 'react';
import { X, Edit2, Loader2 } from 'lucide-react';
import { useTasks } from '@/context/TaskContext';
import * as taskService from '../../services/taskService';
import { TaskDetailTabs } from './TaskDetailTabs';
import { TaskOverview } from './TaskOverview';
import { TaskComments } from './TaskComments';
import { TaskAttachments } from './TaskAttachments';
import { TaskActivity } from './TaskActivity';

export const TaskDetailModal = () => {
  const { showDetailModal, setShowDetailModal, selectedTask, setShowEditModal } = useTasks();
  const [activeTab, setActiveTab] = useState('overview');
  const [fullTask, setFullTask] = useState(null);
  const [loading, setLoading] = useState(false);

  const fetchTaskDetails = useCallback(() => {
    return taskService.getTaskById(selectedTask?.id).then(result => {
      if (result.success) {
        setFullTask(result.data);
      }
    }).catch(() => {
      console.error('Failed to fetch task details');
    }).finally(() => {
      setLoading(false);
    });
  }, [selectedTask?.id]);

  useEffect(() => {
    if (showDetailModal && selectedTask?.id) {
      fetchTaskDetails();
    }
  }, [showDetailModal, selectedTask?.id, fetchTaskDetails]);



  if (!showDetailModal) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-slate-900/60 backdrop-blur-sm" onClick={() => setShowDetailModal(false)} />
      
      <div className="relative bg-surface-0 rounded-2xl shadow-2xl w-full max-w-4xl h-[90vh] flex flex-col overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="p-6 border-b border-border-subtle flex items-start justify-between bg-surface-1/50">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-2">
              <span className="text-[10px] font-bold text-primary uppercase tracking-widest bg-primary/10 px-2 py-0.5 rounded">Task Details</span>
              {fullTask?.boardName && (
                <span className="text-[10px] font-bold text-text-subtle uppercase tracking-widest">• {fullTask.boardName}</span>
              )}
            </div>
            <h2 className="text-2xl font-bold text-text-main truncate pr-8">
              {fullTask?.title || selectedTask?.title}
            </h2>
          </div>
          
          <div className="flex items-center gap-2">
            <button 
              onClick={() => { setShowDetailModal(false); setShowEditModal(true); }}
              className="p-2.5 text-text-muted hover:text-primary hover:bg-primary/10 rounded-xl transition-all"
            >
              <Edit2 className="w-5 h-5" />
            </button>
            <button 
              onClick={() => setShowDetailModal(false)} 
              className="p-2.5 text-text-muted hover:text-red-600 hover:bg-red-50 rounded-xl transition-all"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Tabs */}
        <TaskDetailTabs activeTab={activeTab} onTabChange={setActiveTab} />

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 bg-surface-0">
          {loading ? (
            <div className="h-full flex flex-col items-center justify-center gap-3">
              <Loader2 className="w-8 h-8 text-primary animate-spin" />
              <p className="text-text-muted font-medium">Loading details...</p>
            </div>
          ) : (
            <div className="max-w-3xl mx-auto h-full">
              {activeTab === 'overview' && <TaskOverview task={fullTask || selectedTask} />}
              {activeTab === 'comments' && <TaskComments taskId={selectedTask.id} />}
              {activeTab === 'attachments' && <TaskAttachments taskId={selectedTask.id} />}
              {activeTab === 'activity' && <TaskActivity taskId={selectedTask.id} />}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
