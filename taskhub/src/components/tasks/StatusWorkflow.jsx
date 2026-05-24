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
      <div className="absolute left-0 top-1/2 -translate-y-1/2 w-full h-0.5 bg-gray-100 z-0" />
      
      {/* Progress Line */}
      <div 
        className="absolute left-0 top-1/2 -translate-y-1/2 h-0.5 bg-blue-600 transition-all duration-500 z-0" 
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
                ${isCompleted ? 'bg-blue-600 text-white shadow-lg shadow-blue-100' : ''}
                ${isActive ? 'bg-white border-4 border-blue-600 text-blue-600 shadow-xl scale-110' : ''}
                ${isFuture ? 'bg-white border-2 border-gray-200 text-gray-300 hover:border-blue-300 hover:text-blue-300' : ''}
                ${isLoading ? 'animate-pulse' : ''}
              `}
            >
              {isLoading ? (
                <Loader2 className="w-5 h-5 animate-spin" />
              ) : isCompleted ? (
                <Check className="w-5 h-5" />
              ) : (
                <Circle className={`w-5 h-5 ${isActive ? 'fill-blue-600' : ''}`} />
              )}
            </button>
            <span className={`
              absolute -bottom-7 text-[10px] font-bold uppercase tracking-wider whitespace-nowrap
              ${isActive ? 'text-blue-600' : isCompleted ? 'text-gray-900' : 'text-gray-400'}
            `}>
              {stage === 'InProgress' ? 'In Progress' : stage}
            </span>
          </div>
        );
      })}
    </div>
  );
};
