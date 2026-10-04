import React from 'react';
import { ClipboardList, Plus } from 'lucide-react';
import { useTasks } from '../../context/TaskContext';

export const EmptyState = () => {
  const { setShowCreateModal, filters } = useTasks();
  
  const isFiltering = filters.status || filters.priority || filters.searchKeyword;

  return (
    <div className="flex-1 flex flex-col items-center justify-center min-h-[400px] bg-surface-0 rounded-2xl border-2 border-dashed border-border-subtle p-12 text-center animate-in fade-in zoom-in duration-500">
      <div className="w-20 h-20 bg-primary/10 rounded-full flex items-center justify-center mb-6">
        <ClipboardList className="w-10 h-10 text-primary opacity-40" />
      </div>
      
      <h3 className="text-xl font-bold text-text-main mb-2">
        {isFiltering ? 'No tasks match your filters' : 'No tasks here yet'}
      </h3>
      
      <p className="text-text-muted max-w-sm mb-8 font-medium">
        {isFiltering 
          ? 'Try adjusting your filters or search keywords to find what you are looking for.' 
          : 'Get started by creating your first task and stay organized with your team.'}
      </p>

      {isFiltering ? (
        <button
          onClick={() => window.location.reload()}
          className="px-6 py-3 bg-text-main text-text-inverse font-bold rounded-xl hover:opacity-90 transition-all shadow-lg"
        >
          Reset Filters
        </button>
      ) : (
        <button
          onClick={() => setShowCreateModal(true)}
          className="flex items-center gap-2 px-6 py-3 bg-primary text-white font-bold rounded-xl hover:bg-primary-dark transition-all shadow-lg shadow-primary/20"
        >
          <Plus className="w-5 h-5" />
          <span>Create First Task</span>
        </button>
      )}
    </div>
  );
};
