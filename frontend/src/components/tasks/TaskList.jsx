import React, { useState } from 'react';
import { useTasks } from '../../context/TaskContext';
import { TaskListHeader } from './TaskListHeader';
import { TaskFilters } from './TaskFilters';
import { TaskCard } from './TaskCard';
import { TaskTable } from './TaskTable';
import { TaskKanbanBoard } from './TaskKanbanBoard';
import { EmptyState } from './EmptyState';
import { CreateTaskModal } from './CreateTaskModal';
import { EditTaskModal } from './EditTaskModal';
import { TaskDetailModal } from './TaskDetailModal';

export const TaskList = () => {
  const {
    tasks,
    loading,
    error,
    viewMode,
    filters,
    handlePageChange,
    totalCount,
    openDetail,
    setShowEditModal
  } = useTasks();

  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  const renderContent = () => {
    if (loading && tasks.length === 0) {
      return (
        <div className="flex-1 flex items-center justify-center min-h-[400px]">
          <div className="flex flex-col items-center gap-4">
            <div className="w-10 h-10 border-4 border-primary border-t-transparent rounded-full animate-spin"></div>
            <p className="text-text-muted font-medium">Loading tasks...</p>
          </div>
        </div>
      );
    }

    if (tasks.length === 0) {
      return <EmptyState />;
    }

    switch (viewMode) {
      case 'board':
        return <TaskKanbanBoard />;
      case 'table':
        return <TaskTable />;
      default:
        return (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
            {tasks.map(task => (
              <TaskCard 
                key={task.id} 
                task={task} 
                onClick={() => openDetail(task)}
                onEdit={() => setShowEditModal(true)}
                onDelete={() => {}} // Handle delete logic
              />
            ))}
          </div>
        );
    }
  };

  return (
    <div className="flex flex-col h-full bg-surface-1">
      <div className="flex flex-1 overflow-hidden">
        {/* Filter Sidebar */}
        <TaskFilters 
          isOpen={isSidebarOpen} 
          onClose={() => setIsSidebarOpen(false)} 
        />

        {/* Main Content Area */}
        <main className="flex-1 flex flex-col min-w-0 overflow-hidden">
          <div className="flex-1 overflow-y-auto px-6 py-8">
            <div className="max-w-7xl mx-auto">
              <TaskListHeader 
                onFilterToggle={() => setIsSidebarOpen(true)} 
              />

              {error && (
                <div className="mb-6 p-4 bg-rose-50 dark:bg-rose-950/30 border border-rose-100 dark:border-rose-900/50 rounded-xl text-rose-500 text-sm font-medium">
                  {error}
                </div>
              )}

              {renderContent()}

              {/* Pagination */}
              {viewMode !== 'board' && totalCount > filters.pageSize && (
                <div className="mt-10 flex items-center justify-between border-t border-border-subtle pt-6">
                  <p className="text-sm text-text-muted">
                    Showing <span className="font-semibold text-text-main">{tasks.length}</span> of <span className="font-semibold text-text-main">{totalCount}</span> tasks
                  </p>
                  <div className="flex gap-2">
                    <button
                      disabled={filters.page === 1}
                      onClick={() => handlePageChange(filters.page - 1)}
                      className="px-4 py-2 bg-surface-0 border border-border-subtle text-text-main rounded-lg text-sm font-medium hover:bg-hover-bg disabled:opacity-50 disabled:bg-surface-2 disabled:cursor-not-allowed transition-all"
                    >
                      Previous
                    </button>
                    <button
                      disabled={filters.page * filters.pageSize >= totalCount}
                      onClick={() => handlePageChange(filters.page + 1)}
                      className="px-4 py-2 bg-surface-0 border border-border-subtle text-text-main rounded-lg text-sm font-medium hover:bg-hover-bg disabled:opacity-50 disabled:bg-surface-2 disabled:cursor-not-allowed transition-all"
                    >
                      Next
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </main>
      </div>

      {/* Modals */}
      <CreateTaskModal />
      <EditTaskModal />
      <TaskDetailModal />
    </div>
  );
};
