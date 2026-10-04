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
        <h1 className="text-2xl font-bold text-text-main">Tasks</h1>
        
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-subtle" />
          <input
            type="text"
            placeholder="Search tasks..."
            value={filters.searchKeyword}
            onChange={handleSearch}
            className="w-full pl-10 pr-4 py-2 bg-surface-0 border border-border-subtle text-text-main placeholder:text-text-muted rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
          />
        </div>
      </div>

      <div className="flex items-center gap-2">
        {/* View Toggles */}
        <div className="flex items-center bg-surface-2 p-1 rounded-lg">
          <button
            onClick={() => setViewMode('list')}
            className={`p-1.5 rounded-md transition-all ${viewMode === 'list' ? 'bg-surface-0 shadow-sm text-primary font-bold' : 'text-text-muted hover:text-text-main'}`}
            title="List View"
          >
            <List className="w-4 h-4" />
          </button>
          <button
            onClick={() => setViewMode('board')}
            className={`p-1.5 rounded-md transition-all ${viewMode === 'board' ? 'bg-surface-0 shadow-sm text-primary font-bold' : 'text-text-muted hover:text-text-main'}`}
            title="Board View"
          >
            <LayoutGrid className="w-4 h-4" />
          </button>
          <button
            onClick={() => setViewMode('table')}
            className={`p-1.5 rounded-md transition-all ${viewMode === 'table' ? 'bg-surface-0 shadow-sm text-primary font-bold' : 'text-text-muted hover:text-text-main'}`}
            title="Table View"
          >
            <TableIcon className="w-4 h-4" />
          </button>
        </div>

        <button
          onClick={onFilterToggle}
          className="flex items-center gap-2 px-3 py-2 bg-surface-0 border border-border-subtle rounded-lg text-sm font-medium text-text-main hover:bg-hover-bg transition-all lg:hidden"
        >
          <Filter className="w-4 h-4" />
          <span>Filters</span>
        </button>

        <button
          onClick={() => setShowCreateModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-primary hover:bg-primary-dark text-white rounded-lg text-sm font-semibold shadow-lg shadow-primary/20 transition-all"
        >
          <Plus className="w-4 h-4" />
          <span>New Task</span>
        </button>
      </div>
    </div>
  );
};
