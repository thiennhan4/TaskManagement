import { useShallow } from 'zustand/react/shallow';
import React, { useState, useEffect, useRef } from 'react';
import useTimeTrackingStore from '@/stores/useTimeTrackingStore';
import { Play, Square, Clock, Plus } from 'lucide-react';
import Button from '@/components/ui/Button';
import toast from 'react-hot-toast';

export default function TimeTrackingWidget({ taskId }) {
  const { runningTimer, startTimer, stopTimer, fetchRunningTimer, fetchTaskEntries, taskEntries, taskEntriesPage, entriesError, isLoading } = useTimeTrackingStore(useShallow(state => ({ runningTimer: state.runningTimer, startTimer: state.startTimer, stopTimer: state.stopTimer, fetchRunningTimer: state.fetchRunningTimer, fetchTaskEntries: state.fetchTaskEntries, taskEntries: state.taskEntries, taskEntriesPage: state.taskEntriesPage, entriesError: state.entriesError, isLoading: state.isLoading })));
  const [description, setDescription] = useState('');
  const taskVersion = useRef(0);
  
  const isRunningForThisTask = runningTimer?.taskId === taskId;

  useEffect(() => {
    const lifecycle = taskVersion;
    fetchRunningTimer();
    if (taskId) {
      fetchTaskEntries(taskId);
    }
    return () => { lifecycle.current++; };
  }, [fetchRunningTimer, fetchTaskEntries, taskId]);

  const handleStart = async () => {
    const version = taskVersion.current;
    try {
      await startTimer(taskId, description, true);
      if (version === taskVersion.current) setDescription('');
    } catch (err) {
      if (version === taskVersion.current) toast.error(err.response?.data?.message || 'Unable to start timer');
    }
  };

  const handleStop = async () => {
    const version = taskVersion.current;
    try {
      await stopTimer(description);
      if (version === taskVersion.current) fetchTaskEntries(taskId);
    } catch (err) {
      if (version === taskVersion.current) toast.error(err.response?.data?.message || 'Unable to stop timer');
    }
  };

  const formatDuration = (seconds) => {
    if (!seconds) return '0h 0m';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    return `${h}h ${m}m`;
  };

  const totalSeconds = taskEntries.reduce((acc, entry) => acc + entry.durationSeconds, 0);

  return (
    <div className="bg-surface-1 rounded-xl p-4 border border-border-subtle">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-bold flex items-center gap-2 text-text-main">
          <Clock size={16} className="text-primary" />
          Time Tracking
        </h3>
        <span className="text-xs font-semibold text-text-muted bg-surface-2 px-2 py-1 rounded-lg">
          Page total: {formatDuration(totalSeconds)}
        </span>
      </div>

      <div className="flex items-center gap-2 mb-4">
        <input 
          type="text" 
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="What are you working on?"
          className="flex-1 text-sm px-3 py-2 bg-surface-0 border border-border-subtle rounded-lg text-text-main placeholder:text-text-muted focus:outline-none focus:border-primary transition-colors"
          disabled={isRunningForThisTask}
        />
        {isRunningForThisTask ? (
          <Button variant="danger" size="sm" onClick={handleStop} className="gap-1 px-3">
            <Square size={14} fill="currentColor" />
            Stop
          </Button>
        ) : (
          <Button variant="primary" size="sm" onClick={handleStart} className="gap-1 px-3" disabled={!!runningTimer}>
            <Play size={14} fill="currentColor" />
            Start
          </Button>
        )}
      </div>

      {runningTimer && !isRunningForThisTask && (
        <div className="text-xs text-amber-600 dark:text-amber-400 mb-4 flex items-center gap-1 bg-amber-50 dark:bg-amber-900/20 p-2 rounded-lg">
          <Clock size={12} />
          You have another timer running. Stop it first.
        </div>
      )}

      {entriesError && <p role="alert" className="text-sm text-text-main">{entriesError}</p>}
      {taskEntriesPage?.totalPages > 1 && <div className="flex items-center gap-2">
        <Button disabled={isLoading || taskEntriesPage.page <= 1} onClick={() => fetchTaskEntries(taskId, taskEntriesPage.page - 1)}>Previous</Button>
        <span className="text-sm text-text-muted">{taskEntriesPage.page} / {taskEntriesPage.totalPages}</span>
        <Button disabled={isLoading || taskEntriesPage.page >= taskEntriesPage.totalPages} onClick={() => fetchTaskEntries(taskId, taskEntriesPage.page + 1)}>Next</Button>
      </div>}
      {taskEntries.length > 0 && (
        <div className="space-y-2 mt-4 max-h-40 overflow-y-auto pr-1">
          {taskEntries.map(entry => (
            <div key={entry.id} className="flex justify-between items-start text-xs border-t border-border-subtle pt-2 first:border-0 first:pt-0">
              <div>
                <span className="font-semibold text-text-main">{entry.userName}</span>
                <p className="text-text-muted mt-0.5">{entry.description || 'No description'}</p>
              </div>
              <div className="text-right text-text-main">
                <span className="font-bold">{formatDuration(entry.durationSeconds)}</span>
                <p className="text-[10px] text-text-subtle mt-0.5">{new Date(entry.startTime).toLocaleDateString()}</p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
