import React from 'react';
import { X, Filter, ChevronDown } from 'lucide-react';
import { useTasks } from '../../context/TaskContext';

const STATUS_OPTIONS = [
  { value: '', label: 'All Status' },
  { value: 'Todo', label: 'To Do' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Review', label: 'Review' },
  { value: 'Done', label: 'Done' }
];

const PRIORITY_OPTIONS = [
  { value: '', label: 'All Priority' },
  { value: 'Critical', label: 'Critical' },
  { value: 'High', label: 'High' },
  { value: 'Medium', label: 'Medium' },
  { value: 'Low', label: 'Low' }
];

export const TaskFilters = ({ isOpen, onClose }) => {
  const { filters, updateFilters } = useTasks();

  const handleFilterChange = (key, value) => {
    updateFilters({ [key]: value });
  };

  return (
    <aside className={`
      fixed inset-y-0 right-0 z-50 w-72 bg-surface-0 shadow-xl transform transition-transform duration-300 ease-in-out lg:static lg:transform-none lg:z-0 lg:w-64 lg:shadow-none lg:border-r lg:border-border-subtle p-6
      ${isOpen ? 'translate-x-0' : 'translate-x-full lg:translate-x-0'}
    `}>
      <div className="flex items-center justify-between mb-8 lg:hidden">
        <h2 className="text-lg font-bold flex items-center gap-2 text-text-main">
          <Filter className="w-5 h-5 text-primary" />
          Filters
        </h2>
        <button onClick={onClose} className="p-2 hover:bg-hover-bg rounded-lg text-text-muted">
          <X className="w-5 h-5" />
        </button>
      </div>

      <div className="hidden lg:flex items-center gap-2 mb-6">
        <Filter className="w-5 h-5 text-primary" />
        <h2 className="font-bold text-text-main">Filter By</h2>
      </div>

      <div className="space-y-8">
        {/* Status Filter */}
        <div>
          <label className="block text-xs font-bold text-text-subtle uppercase tracking-wider mb-3">Status</label>
          <div className="space-y-1.5">
            {STATUS_OPTIONS.map(option => (
              <button
                key={option.value}
                onClick={() => handleFilterChange('status', option.value)}
                className={`w-full text-left px-3 py-2 rounded-lg text-sm font-medium transition-all ${
                  filters.status === option.value 
                    ? 'bg-primary/10 text-primary font-bold' 
                    : 'text-text-muted hover:bg-hover-bg'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>

        {/* Priority Filter */}
        <div>
          <label className="block text-xs font-bold text-text-subtle uppercase tracking-wider mb-3">Priority</label>
          <div className="space-y-1.5">
            {PRIORITY_OPTIONS.map(option => (
              <button
                key={option.value}
                onClick={() => handleFilterChange('priority', option.value)}
                className={`w-full text-left px-3 py-2 rounded-lg text-sm font-medium transition-all ${
                  filters.priority === option.value 
                    ? 'bg-primary/10 text-primary font-bold' 
                    : 'text-text-muted hover:bg-hover-bg'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>

        {/* Extra Filters */}
        <div className="pt-4 border-t border-border-subtle">
          <div className="flex items-center justify-between mb-4">
            <span className="text-sm font-medium text-text-main">Show Overdue</span>
            <button
              onClick={() => handleFilterChange('isOverdue', !filters.isOverdue)}
              className={`relative inline-flex h-5 w-10 items-center rounded-full transition-colors ${
                filters.isOverdue ? 'bg-primary' : 'bg-surface-3'
              }`}
            >
              <span className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white transition-transform ${
                filters.isOverdue ? 'translate-x-5.5' : 'translate-x-1'
              }`} />
            </button>
          </div>
        </div>

        {/* Sort By */}
        <div>
          <label className="block text-xs font-bold text-text-subtle uppercase tracking-wider mb-3">Sort By</label>
          <select
            value={filters.sortBy}
            onChange={(e) => handleFilterChange('sortBy', e.target.value)}
            className="w-full px-3 py-2 bg-surface-2 border border-border-subtle text-text-main rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
          >
            <option value="CreatedAt">Created Date</option>
            <option value="DueDate">Due Date</option>
            <option value="Priority">Priority</option>
            <option value="Title">Alphabetical</option>
          </select>
        </div>
      </div>

      <div className="mt-10 lg:hidden">
        <button
          onClick={onClose}
          className="w-full py-3 bg-primary text-white rounded-xl font-bold shadow-lg shadow-primary/20 hover:bg-primary-dark transition-all"
        >
          Show Results
        </button>
      </div>
    </aside>
  );
};
