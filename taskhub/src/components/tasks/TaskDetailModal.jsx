import React, { useState, useEffect } from 'react';
import { X, Edit2, Loader2 } from 'lucide-react';
import { useTasks } from '../../context/TaskContext';
import * as taskService from '../../services/taskService';
import { TaskDetailTabs } from './TaskDetailTabs';
import { TaskOverview } from './TaskOverview';
import { TaskComments } from './TaskComments';
import { TaskAttachments } from './TaskAttachments';
import { TaskActivity } from './TaskActivity';

export const TaskDetailModal = () => {
  const { showDetailModal, setShowDetailModal, selectedTask, setSelectedTask, setShowEditModal } = useTasks();
  const [activeTab, setActiveTab] = useState('overview');
  const [fullTask, setFullTask] = useState(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (showDetailModal && selectedTask?.id) {
      fetchTaskDetails();
    } else {
      setFullTask(null);
      setActiveTab('overview');
    }
  }, [showDetailModal, selectedTask]);

  const fetchTaskDetails = async () => {
    setLoading(true);
    try {
      const result = await taskService.getTaskById(selectedTask.id);
      if (result.success) {
        setFullTask(result.data);
      }
    } catch (err) {
      console.error('Failed to fetch task details');
    } finally {
      setLoading(false);
    }
  };

  if (!showDetailModal) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-slate-900/60 backdrop-blur-sm" onClick={() => setShowDetailModal(false)} />
      
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-4xl h-[90vh] flex flex-col overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="p-6 border-b border-gray-100 flex items-start justify-between bg-gray-50/50">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-2">
              <span className="text-[10px] font-bold text-blue-600 uppercase tracking-widest bg-blue-50 px-2 py-0.5 rounded">Task Details</span>
              {fullTask?.boardName && (
                <span className="text-[10px] font-bold text-gray-400 uppercase tracking-widest">• {fullTask.boardName}</span>
              )}
            </div>
            <h2 className="text-2xl font-bold text-gray-900 truncate pr-8">
              {fullTask?.title || selectedTask?.title}
            </h2>
          </div>
          
          <div className="flex items-center gap-2">
            <button 
              onClick={() => { setShowDetailModal(false); setShowEditModal(true); }}
              className="p-2.5 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all"
            >
              <Edit2 className="w-5 h-5" />
            </button>
            <button 
              onClick={() => setShowDetailModal(false)} 
              className="p-2.5 text-gray-500 hover:text-red-600 hover:bg-red-50 rounded-xl transition-all"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Tabs */}
        <TaskDetailTabs activeTab={activeTab} onTabChange={setActiveTab} />

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 bg-white">
          {loading ? (
            <div className="h-full flex flex-col items-center justify-center gap-3">
              <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
              <p className="text-gray-500 font-medium">Loading details...</p>
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
