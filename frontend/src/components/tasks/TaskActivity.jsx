import React, { useState, useEffect } from 'react';
import { Activity, Clock, Loader2 } from 'lucide-react';
import * as taskService from '../../services/taskService';
import { toast } from 'react-hot-toast';

export const TaskActivity = ({ taskId }) => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const result = await taskService.getActivityLogs(taskId);
      if (result.success) {
        setLogs(result.data);
      }
    } catch (err) {
      toast.error('Failed to load activity logs');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs();
  }, [taskId]);

  const getActionStyles = (action) => {
    switch (action) {
      case 'Created': return { bg: 'bg-primary/10', text: 'text-primary', dot: 'bg-primary' };
      case 'StatusChanged': return { bg: 'bg-amber-500/10', text: 'text-amber-500', dot: 'bg-amber-500' };
      case 'Assigned': return { bg: 'bg-emerald-500/10', text: 'text-emerald-500', dot: 'bg-emerald-500' };
      case 'Commented': return { bg: 'bg-surface-2', text: 'text-text-muted', dot: 'bg-text-muted' };
      case 'AttachmentAdded': return { bg: 'bg-purple-500/10', text: 'text-purple-500', dot: 'bg-purple-500' };
      default: return { bg: 'bg-surface-2', text: 'text-text-muted', dot: 'bg-text-muted' };
    }
  };

  if (loading && logs.length === 0) {
    return (
      <div className="flex justify-center py-10">
        <Loader2 className="w-6 h-6 text-primary animate-spin" />
      </div>
    );
  }

  return (
    <div className="animate-in fade-in slide-in-from-bottom-2 duration-300">
      <h4 className="text-xs font-bold text-text-subtle uppercase tracking-wider mb-8 flex items-center gap-2">
        <Activity className="w-3.5 h-3.5" />
        Activity Timeline
      </h4>

      {logs.length > 0 ? (
        <div className="relative space-y-0">
          {/* Vertical Timeline Line */}
          <div className="absolute left-[19px] top-2 bottom-2 w-0.5 bg-border-subtle" />

          {logs.map((log, index) => {
            const styles = getActionStyles(log.action);
            return (
              <div key={log.id} className="relative flex gap-6 pb-10 last:pb-0 group">
                {/* Dot */}
                <div className={`relative z-10 w-10 h-10 rounded-full ${styles.bg} flex items-center justify-center border-4 border-surface-0 shadow-sm transition-transform group-hover:scale-110`}>
                  <div className={`w-2.5 h-2.5 rounded-full ${styles.dot}`} />
                </div>

                {/* Content */}
                <div className="flex-1 pt-1.5">
                  <div className="flex items-center justify-between mb-1">
                    <p className="text-sm font-bold text-text-main">
                      {log.userName} <span className="font-normal text-text-muted">{log.actionDescription}</span>
                    </p>
                    <span className="text-[10px] font-bold text-text-subtle flex items-center gap-1 bg-surface-2 px-2 py-0.5 rounded uppercase tracking-wider">
                      <Clock className="w-3 h-3" />
                      {new Date(log.timestamp).toLocaleDateString()}
                    </span>
                  </div>
                  
                  {(log.oldValue || log.newValue) && (
                    <div className="mt-2 flex items-center gap-2 text-xs font-medium">
                      {log.oldValue && (
                        <span className="px-2 py-1 bg-rose-50 dark:bg-rose-950/30 text-rose-500 rounded line-through opacity-60">{log.oldValue}</span>
                      )}
                      {log.oldValue && log.newValue && <span className="text-text-subtle">→</span>}
                      {log.newValue && (
                        <span className="px-2 py-1 bg-emerald-50 dark:bg-emerald-950/30 text-emerald-500 rounded">{log.newValue}</span>
                      )}
                    </div>
                  )}
                  
                  <p className="text-[10px] text-text-subtle mt-2 font-medium">
                    {new Date(log.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="py-12 text-center bg-surface-2/50 rounded-2xl border border-border-subtle">
          <Activity className="w-10 h-10 mx-auto mb-3 text-text-subtle opacity-40" />
          <p className="text-sm font-medium text-text-subtle">No activity recorded yet</p>
        </div>
      )}
    </div>
  );
};
