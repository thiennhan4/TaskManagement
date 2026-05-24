import React from 'react';
import { ClipboardList, Plus } from 'lucide-react';
import { useTasks } from '../../context/TaskContext';

export const EmptyState = () => {
  const { setShowCreateModal, filters } = useTasks();
  
  const isFiltering = filters.status || filters.priority || filters.searchKeyword;

  return (
    <div className="flex-1 flex flex-col items-center justify-center min-h-[400px] bg-white rounded-2xl border-2 border-dashed border-gray-100 p-12 text-center animate-in fade-in zoom-in duration-500">
      <div className="w-20 h-20 bg-blue-50 rounded-full flex items-center justify-center mb-6">
        <ClipboardList className="w-10 h-10 text-blue-600 opacity-40" />
      </div>
      
      <h3 className="text-xl font-bold text-gray-900 mb-2">
        {isFiltering ? 'No tasks match your filters' : 'No tasks here yet'}
      </h3>
      
      <p className="text-gray-500 max-w-sm mb-8 font-medium">
        {isFiltering 
          ? 'Try adjusting your filters or search keywords to find what you are looking for.' 
          : 'Get started by creating your first task and stay organized with your team.'}
      </p>

      {isFiltering ? (
        <button
          onClick={() => window.location.reload()}
          className="px-6 py-3 bg-gray-900 text-white font-bold rounded-xl hover:bg-gray-800 transition-all shadow-lg shadow-gray-100"
        >
          Reset Filters
        </button>
      ) : (
        <button
          onClick={() => setShowCreateModal(true)}
          className="flex items-center gap-2 px-6 py-3 bg-blue-600 text-white font-bold rounded-xl hover:bg-blue-700 transition-all shadow-lg shadow-blue-100"
        >
          <Plus className="w-5 h-5" />
          <span>Create First Task</span>
        </button>
      )}
    </div>
  );
};
