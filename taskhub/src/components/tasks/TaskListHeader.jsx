import React from 'react';
import { LayoutGrid, List, Table as TableIcon, Plus, Search, Filter } from 'lucide-react';
import { useTasks } from '../../context/TaskContext';

export const TaskListHeader = ({ onFilterToggle }) => {
  const { 
    viewMode, 
    setViewMode, 
    filters, 
    updateFilters, 
    setShowCreateModal 
  } = useTasks();

  const handleSearch = (e) => {
    updateFilters({ searchKeyword: e.target.value });
  };

  return (
    <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
      <div className="flex items-center gap-4 flex-1">
        <h1 className="text-2xl font-bold text-gray-900">Tasks</h1>
        
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search tasks..."
            value={filters.searchKeyword}
            onChange={handleSearch}
            className="w-full pl-10 pr-4 py-2 bg-white border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
          />
        </div>
      </div>

      <div className="flex items-center gap-2">
        {/* View Toggles */}
        <div className="flex items-center bg-gray-100 p-1 rounded-lg">
          <button
            onClick={() => setViewMode('list')}
            className={`p-1.5 rounded-md transition-all ${viewMode === 'list' ? 'bg-white shadow-sm text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}
            title="List View"
          >
            <List className="w-4 h-4" />
          </button>
          <button
            onClick={() => setViewMode('board')}
            className={`p-1.5 rounded-md transition-all ${viewMode === 'board' ? 'bg-white shadow-sm text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}
            title="Board View"
          >
            <LayoutGrid className="w-4 h-4" />
          </button>
          <button
            onClick={() => setViewMode('table')}
            className={`p-1.5 rounded-md transition-all ${viewMode === 'table' ? 'bg-white shadow-sm text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}
            title="Table View"
          >
            <TableIcon className="w-4 h-4" />
          </button>
        </div>

        <button
          onClick={onFilterToggle}
          className="flex items-center gap-2 px-3 py-2 bg-white border border-gray-200 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 transition-all lg:hidden"
        >
          <Filter className="w-4 h-4" />
          <span>Filters</span>
        </button>

        <button
          onClick={() => setShowCreateModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold shadow-sm transition-all"
        >
          <Plus className="w-4 h-4" />
          <span>New Task</span>
        </button>
      </div>
    </div>
  );
};
