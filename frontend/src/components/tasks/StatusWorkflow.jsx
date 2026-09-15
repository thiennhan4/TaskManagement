import React, { useState } from 'react';
import { Check, Circle, Loader2 } from 'lucide-react';
import { useTasks } from '../../context/TaskContext';
import * as taskService from '../../services/taskService';
import { toast } from 'react-hot-toast';

const STAGES = ['Todo', 'InProgress', 'Review', 'Done'];

export const StatusWorkflow = ({ currentStatus, taskId }) => {
  const { refreshTasks } = useTasks();
  const [loadingStatus, setLoadingStatus] = useState(null);
  const currentIndex = STAGES.indexOf(currentStatus);

  const handleStatusChange = async (newStatus) => {
    if (newStatus === currentStatus) return;
    
    setLoadingStatus(newStatus);
    try {
      const result = await taskService.changeStatus(taskId, newStatus);
      if (result.success) {
        toast.success(`Status updated to ${newStatus}`);
        refreshTasks();
      }
    } catch (err) {
      toast.error('Failed to update status');
    } finally {
      setLoadingStatus(null);
    }
  };

  return (
    <div className="relative flex items-center justify-between w-full max-w-lg mx-auto py-8">
      {/* Background Line */}
      <div className="absolute left-0 top-1/2 -translate-y-1/2 w-full h-0.5 bg-surface-2 z-0" />
      
      {/* Progress Line */}
      <div 
        className="absolute left-0 top-1/2 -translate-y-1/2 h-0.5 bg-primary transition-all duration-500 z-0" 
        style={{ width: `${(currentIndex / (STAGES.length - 1)) * 100}%` }}
      />

      {STAGES.map((stage, index) => {
        const isCompleted = index < currentIndex;
        const isActive = index === currentIndex;
        const isFuture = index > currentIndex;
        const isLoading = loadingStatus === stage;

        return (
          <div key={stage} className="relative z-10 flex flex-col items-center">
            <button
              disabled={isLoading}
              onClick={() => handleStatusChange(stage)}
              className={`
                w-10 h-10 rounded-full flex items-center justify-center transition-all duration-300
                ${isCompleted ? 'bg-primary text-white shadow-lg shadow-primary/20' : ''}
                ${isActive ? 'bg-surface-0 border-4 border-primary text-primary shadow-xl scale-110' : ''}
                ${isFuture ? 'bg-surface-0 border-2 border-border-subtle text-text-subtle hover:border-primary/40 hover:text-primary/60' : ''}
                ${isLoading ? 'animate-pulse' : ''}
              `}
            >
              {isLoading ? (
                <Loader2 className="w-5 h-5 animate-spin" />
              ) : isCompleted ? (
                <Check className="w-5 h-5" />
              ) : (
                <Circle className={`w-5 h-5 ${isActive ? 'fill-primary' : ''}`} />
              )}
            </button>
            <span className={`
              absolute -bottom-7 text-[10px] font-bold uppercase tracking-wider whitespace-nowrap
              ${isActive ? 'text-primary' : isCompleted ? 'text-text-main' : 'text-text-subtle'}
            `}>
              {stage === 'InProgress' ? 'In Progress' : stage}
            </span>
          </div>
        );
      })}
    </div>
  );
};
