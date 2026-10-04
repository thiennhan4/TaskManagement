import { useCallback, useEffect, useRef, useState } from 'react';
import { boardApi } from '@/api/boardApi';
import { projectApi } from '@/api/projectApi';
import { useNotification } from '@/context/NotificationContext';
import { useTaskDetailStore } from '@/stores/useTaskDetailStore';
import { adaptColumns, pageMetadata, upsertColumns, upsertTask, removeTask, updateTaskCount, mergeBoardPage } from '@/utils/boardState';

export function useBoardData({ projectId, boardId, searchQuery }) {
  const { hubConnection, reconnectVersion } = useNotification();
  const [board, setBoard] = useState(null);
  const [lists, setLists] = useState([]);
  const [columnPage, setColumnPage] = useState(null);
  const [capabilities, setCapabilities] = useState({});
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [boardPresence, setBoardPresence] = useState([]);
  const version = useRef(0);
  const pending = useRef(null);
  const partial = columnPage?.page < columnPage?.totalPages || lists.some(list => list.taskPage?.page < list.taskPage?.totalPages);
  const fetchBoardData = useCallback(async () => {
    const request = ++version.current;
    pending.current = null;
    setLoading(true); setLoadingMore(false); setLoadError('');
    try {
      const params = { ...(projectId ? { boardId: boardId || undefined } : {}), ...(searchQuery.trim() ? { searchKeyword: searchQuery.trim() } : {}) };
      const response = projectId ? await projectApi.getProjectKanban(projectId, params) : await boardApi.getBoardById(boardId, params);
      if (request !== version.current) return;
      const data = response.data.data;
      const page = projectId ? data.lists : data.listPage;
      // Header and page metadata never retain a second copy of the card graph.
      const { listPage: _listPage, lists: _lists, ...header } = projectId ? data.board : data;
      setBoard(header); setLists(adaptColumns(page.items)); setColumnPage(pageMetadata(page));
      // Standalone board View is owner/admin-only in PermissionService, exactly
      // like its mutation actions. Project boards expose explicit capabilities.
      const standalone = !projectId && !header.projectId;
      setCapabilities({ canCreateTasks: data.canCreateTasks ?? standalone, canManageColumns: data.canManageColumns ?? standalone });
    } catch (error) {
      if (request === version.current) setLoadError(error.response?.data?.message || 'Failed to load board. Please try again.');
    } finally { if (request === version.current) setLoading(false); }
  }, [projectId, boardId, searchQuery]);
  useEffect(() => {
    const owner = version;
    const timer = window.setTimeout(fetchBoardData, 150);
    return () => { window.clearTimeout(timer); owner.current++; };
  }, [fetchBoardData]);
  const loadMore = async list => {
    if (loading || pending.current) return;
    const request = version.current;
    const marker = {};
    pending.current = marker;
    setLoadingMore(true);
    try {
      const params = { boardId: board.id, listId: list?.id, page: list ? 1 : columnPage.page + 1,
        taskPage: list ? list.taskPage.page + 1 : 1, searchKeyword: searchQuery.trim() || undefined };
      const response = projectId ? await projectApi.getProjectKanban(projectId, params) : await boardApi.getColumns(board.id, params);
      if (request !== version.current) return;
      const page = projectId ? response.data.data.lists : response.data.data;
      setLists(previous => mergeBoardPage(previous, adaptColumns(page.items), list?.id));
      if (!list) setColumnPage(pageMetadata(page));
    } catch (error) {
      if (request === version.current) setLoadError(error.response?.data?.message || 'Failed to load more cards');
    } finally {
      if (pending.current === marker) { pending.current = null; setLoadingMore(false); }
    }
  };
  // Bound automatic event growth to the loaded page capacity. Explicit load-more
  // can expand the current board; changing board/filter drops those pages.
  const needsRefresh = lists.some(list => list.refreshRequired);
  useEffect(() => {
    if (!needsRefresh) return;
    const timer = window.setTimeout(fetchBoardData, 100);
    return () => window.clearTimeout(timer);
  }, [needsRefresh, fetchBoardData]);
  useEffect(() => {
    if (!hubConnection || !board?.id) return;
    let active = true;
    let refreshTimer;
    const refreshPartial = () => {
      if (!partial && !searchQuery && lists.length < 20) return;
      window.clearTimeout(refreshTimer);
      refreshTimer = window.setTimeout(fetchBoardData, 100);
    };
    const taskUpsert = task => {
      if (!active || String(task.boardId) !== String(board.id)) return;
      setLists(previous => upsertTask(previous, task));
      useTaskDetailStore.getState().update(task);
      refreshPartial();
    };
    const columnUpsert = column => {
      if (!active || String(column.boardId) !== String(board.id)) return;
      setLists(previous => upsertColumns(previous, [column]));
      refreshPartial();
    };
    const handlers = {
      TaskCreated: taskUpsert, TaskUpdated: taskUpsert,
      TaskDeleted: id => { if (active) { setLists(previous => removeTask(previous, id)); useTaskDetailStore.getState().remove(id); refreshPartial(); } },
      BoardListCreated: columnUpsert, BoardListUpdated: columnUpsert,
      BoardListDeleted: id => { if (active) { setLists(previous => previous.filter(column => column.id !== id)); refreshPartial(); } },
      TaskCommentsCountUpdated: event => { if (active) setLists(previous => updateTaskCount(previous, event)); },
      UpdateBoardPresence: users => { if (active) setBoardPresence(users); },
    };
    for (const [event, handler] of Object.entries(handlers)) hubConnection.on(event, handler);
    hubConnection.invoke('JoinBoard', String(board.id)).catch(() => {});
    return () => {
      active = false; window.clearTimeout(refreshTimer);
      for (const [event, handler] of Object.entries(handlers)) hubConnection.off(event, handler);
      hubConnection.invoke('LeaveBoard', String(board.id)).catch(() => {});
    };
  }, [hubConnection, board?.id, partial, searchQuery, lists.length, fetchBoardData]);
  useEffect(() => {
    if (hubConnection && board?.id && reconnectVersion > 0) hubConnection.invoke('JoinBoard', String(board.id)).catch(() => {});
  }, [hubConnection, board?.id, reconnectVersion]);
  const visibleColumnPage = columnPage && columnPage.page >= columnPage.totalPages
    ? { ...columnPage, totalItems: lists.length } : columnPage;
  return { board, lists, setLists, columnPage: visibleColumnPage, capabilities, loading, loadingMore, loadError, boardPresence, partial, fetchBoardData, loadMore };
}
