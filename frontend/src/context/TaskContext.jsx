import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import * as taskService from '../services/taskService';
import { toast } from 'react-hot-toast';

const TaskContext = createContext();

export const useTasks = () => {
  const context = useContext(TaskContext);
  if (!context) {
    throw new Error('useTasks must be used within a TaskProvider');
  }
  return context;
};

export const TaskProvider = ({ children }) => {
  const [tasks, setTasks] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  
  const [filters, setFilters] = useState({
    status: '',
    priority: '',
    boardId: '',
    listId: '',
    assignedToUserId: '',
    searchKeyword: '',
    isOverdue: false,
    sortBy: 'CreatedAt',
    sortOrder: 'desc',
    page: 1,
    pageSize: 10
  });

  const [viewMode, setViewMode] = useState('list'); // 'list' | 'board' | 'table'
  const [selectedTask, setSelectedTask] = useState(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDetailModal, setShowDetailModal] = useState(false);

  const fetchTasks = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await taskService.getTasks(filters);
      if (result.success) {
        setTasks(result.data.tasks);
        setTotalCount(result.data.totalCount);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch tasks');
      toast.error('Could not load tasks');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchTasks();
  }, [fetchTasks]);

  const updateFilters = (newFilters) => {
    setFilters(prev => ({ ...prev, ...newFilters, page: 1 }));
  };

  const handlePageChange = (newPage) => {
    setFilters(prev => ({ ...prev, page: newPage }));
  };

  const refreshTasks = () => fetchTasks();

  const openDetail = (task) => {
    setSelectedTask(task);
    setShowDetailModal(true);
  };

  const value = {
    tasks,
    totalCount,
    loading,
    error,
    filters,
    viewMode,
    selectedTask,
    showCreateModal,
    showEditModal,
    showDetailModal,
    setTasks,
    setFilters,
    updateFilters,
    handlePageChange,
    setViewMode,
    setSelectedTask,
    setShowCreateModal,
    setShowEditModal,
    setShowDetailModal,
    refreshTasks,
    openDetail
  };

  return (
    <TaskContext.Provider value={value}>
      {children}
    </TaskContext.Provider>
  );
};
