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
      case 'Created': return { bg: 'bg-blue-100', text: 'text-blue-600', dot: 'bg-blue-600' };
      case 'StatusChanged': return { bg: 'bg-yellow-100', text: 'text-yellow-600', dot: 'bg-yellow-600' };
      case 'Assigned': return { bg: 'bg-green-100', text: 'text-green-600', dot: 'bg-green-600' };
      case 'Commented': return { bg: 'bg-gray-100', text: 'text-gray-600', dot: 'bg-gray-600' };
      case 'AttachmentAdded': return { bg: 'bg-purple-100', text: 'text-purple-600', dot: 'bg-purple-600' };
      default: return { bg: 'bg-gray-100', text: 'text-gray-600', dot: 'bg-gray-600' };
    }
  };

  if (loading && logs.length === 0) {
    return (
      <div className="flex justify-center py-10">
        <Loader2 className="w-6 h-6 text-blue-600 animate-spin" />
      </div>
    );
  }

  return (
    <div className="animate-in fade-in slide-in-from-bottom-2 duration-300">
      <h4 className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-8 flex items-center gap-2">
        <Activity className="w-3.5 h-3.5" />
        Activity Timeline
      </h4>

      {logs.length > 0 ? (
        <div className="relative space-y-0">
          {/* Vertical Timeline Line */}
          <div className="absolute left-[19px] top-2 bottom-2 w-0.5 bg-gray-100" />

          {logs.map((log, index) => {
            const styles = getActionStyles(log.action);
            return (
              <div key={log.id} className="relative flex gap-6 pb-10 last:pb-0 group">
                {/* Dot */}
                <div className={`relative z-10 w-10 h-10 rounded-full ${styles.bg} flex items-center justify-center border-4 border-white shadow-sm transition-transform group-hover:scale-110`}>
                  <div className={`w-2.5 h-2.5 rounded-full ${styles.dot}`} />
                </div>

                {/* Content */}
                <div className="flex-1 pt-1.5">
                  <div className="flex items-center justify-between mb-1">
                    <p className="text-sm font-bold text-gray-900">
                      {log.userName} <span className="font-normal text-gray-500">{log.actionDescription}</span>
                    </p>
                    <span className="text-[10px] font-bold text-gray-400 flex items-center gap-1 bg-gray-50 px-2 py-0.5 rounded uppercase tracking-wider">
                      <Clock className="w-3 h-3" />
                      {new Date(log.timestamp).toLocaleDateString()}
                    </span>
                  </div>
                  
                  {(log.oldValue || log.newValue) && (
                    <div className="mt-2 flex items-center gap-2 text-xs font-medium">
                      {log.oldValue && (
                        <span className="px-2 py-1 bg-red-50 text-red-600 rounded line-through opacity-60">{log.oldValue}</span>
                      )}
                      {log.oldValue && log.newValue && <span className="text-gray-300">→</span>}
                      {log.newValue && (
                        <span className="px-2 py-1 bg-green-50 text-green-600 rounded">{log.newValue}</span>
                      )}
                    </div>
                  )}
                  
                  <p className="text-[10px] text-gray-400 mt-2 font-medium">
                    {new Date(log.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="py-12 text-center bg-gray-50 rounded-2xl border border-gray-100">
          <Activity className="w-10 h-10 mx-auto mb-3 text-gray-200" />
          <p className="text-sm font-medium text-gray-400">No activity recorded yet</p>
        </div>
      )}
    </div>
  );
};
