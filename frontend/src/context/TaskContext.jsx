import React, { useState, useCallback, useEffect, useRef } from 'react';
import { taskApi } from '@/api/taskApi';
import { toast } from 'react-hot-toast';

import TaskContext from '@/context/taskState';



export const TaskProvider = ({ children }) => {
  const [tasks, setTasks] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const requestVersion = useRef(0);
  
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

  const fetchTasks = useCallback(() => {
    const version = ++requestVersion.current;
    return taskApi.getTasks(filters).then(({ data: result }) => {
      if (version !== requestVersion.current) return;
      setError(null);
      if (result.success) {
        setTasks(result.data.items);
        setTotalCount(result.data.totalItems);
      } else {
        setError(result.message);
      }
    }).catch(err => {
      if (version !== requestVersion.current) return;
      setError(err.message || 'Failed to fetch tasks');
      toast.error('Could not load tasks');
    }).finally(() => {
      if (version === requestVersion.current) setLoading(false);
    });
  }, [filters]);

  useEffect(() => {
    fetchTasks();
    return () => { requestVersion.current++; };
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
