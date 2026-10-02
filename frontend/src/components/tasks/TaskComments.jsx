import PageControls from '@/components/ui/PageControls';
import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Send, Loader2, MessageSquare } from 'lucide-react';
import commentApi from '@/api/commentApi';
import { CommentCard } from '@/components/tasks/CommentCard';
import { CommentForm } from './CommentForm';
import { toast } from 'react-hot-toast';
import { useAuth } from '@/context/AuthContext';
import { useNotification } from '@/context/NotificationContext';

const TaskComments = ({ taskId, readOnly = false }) => {
  const { user } = useAuth();
  const { hubConnection, reconnectVersion } = useNotification();
  const [pageInfo, setPageInfo] = useState(null);
  const [comments, setComments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [typingUsers, setTypingUsers] = useState([]);
  
  const commentsEndRef = useRef(null);
  const typingTimeoutRef = useRef(null);

  const fetchComments = useCallback(async (page = 1) => {
    setLoading(true);
    try {
      const { data: result } = await commentApi.getComments(taskId, page);
      if (!result.success || !Array.isArray(result.data?.items)) throw new Error(result.message || 'Invalid comments response');
      if (result.success) {
        setError('');
        setComments(result.data.items);
        setPageInfo(result.data);
      }
    } catch (error) {
      setError(error.response?.data?.message || 'Failed to load comments');
      toast.error('Failed to load comments');
    } finally {
      setLoading(false);
    }
  }, [taskId]);

  useEffect(() => {
    const timer = window.setTimeout(() => { fetchComments(); }, 0);
    return () => window.clearTimeout(timer);
  }, [fetchComments]);

  useEffect(() => {
    if (!hubConnection) return;

    const taskIdStr = String(taskId);
    hubConnection.invoke('JoinTask', taskIdStr).catch(() => {});

    const handleCommentAdded = () => fetchComments(pageInfo?.page ?? 1);
    const handleCommentDeleted = () => fetchComments(pageInfo?.page ?? 1);

    const handleUserTyping = (tid, name, isTyping) => {
      if (tid !== taskIdStr) return;
      setTypingUsers(prev => {
        if (isTyping) {
          if (prev.includes(name)) return prev;
          return [...prev, name];
        } else {
          return prev.filter(n => n !== name);
        }
      });
    };
    hubConnection.on('CommentAdded', handleCommentAdded);
    hubConnection.on('CommentDeleted', handleCommentDeleted);
    hubConnection.on('UserTyping', handleUserTyping);

    return () => {
      hubConnection.invoke('LeaveTask', taskIdStr).catch(() => {});
      hubConnection.off('CommentAdded', handleCommentAdded);
      hubConnection.off('CommentDeleted', handleCommentDeleted);
      hubConnection.off('UserTyping', handleUserTyping);
    };
  }, [hubConnection, taskId, fetchComments, pageInfo?.page]);

  useEffect(() => {
    if (hubConnection && reconnectVersion > 0) {
      hubConnection.invoke('JoinTask', String(taskId)).catch(() => {});
    }
  }, [hubConnection, taskId, reconnectVersion]);

  const scrollToBottom = () => {
    commentsEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [comments]);

  useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, []);

  const handleTyping = () => {
    if (!hubConnection || !user) return;
    
    hubConnection.invoke('StartTyping', String(taskId), user.fullName);

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    typingTimeoutRef.current = setTimeout(() => {
      hubConnection.invoke('StopTyping', String(taskId), user.fullName);
    }, 2000);
  };

  const handleAddComment = async (content) => {
    try {
      const { data: result } = await commentApi.addComment(taskId, { content });
      if (result.success) {
        toast.success('Comment added');
        await fetchComments(pageInfo?.totalPages || 1);
        // Stop typing immediately when comment is sent
        if (typingTimeoutRef.current) {
          clearTimeout(typingTimeoutRef.current);
        }
        if (hubConnection && user) {
          hubConnection.invoke('StopTyping', String(taskId), user.fullName);
        }
      }
    } catch {
      toast.error('Failed to add comment');
    }
  };

  const handleDeleteComment = async (commentId) => {
    try {
      const { data: result } = await commentApi.deleteComment(commentId);
      if (result.success) {
        toast.success('Comment deleted');
        await fetchComments(comments.length === 1 ? Math.max(1, (pageInfo?.page ?? 1) - 1) : pageInfo?.page ?? 1);
      }
    } catch {
      toast.error('Failed to delete comment');
    }
  };

  return (
    <div className="flex flex-col h-full animate-in fade-in slide-in-from-bottom-2 duration-300">
      <PageControls page={pageInfo} loading={loading} onPage={fetchComments} />
      {/* Comment Form */}
      {!readOnly && <div className="mb-4">
        <CommentForm onSubmit={handleAddComment} onTyping={handleTyping} />
        {typingUsers.length > 0 && (
          <p className="text-xs text-text-subtle italic mt-2.5 ml-2.5 animate-pulse font-medium">
            {typingUsers.join(', ')} {typingUsers.length === 1 ? 'is' : 'are'} typing...
          </p>
        )}
      </div>}

      {/* Comment List */}
      <div className="flex-1 space-y-6">
        {error ? <p role="alert">{error}</p> : loading && comments.length === 0 ? (
          <div className="flex justify-center py-10">
            <Loader2 className="w-6 h-6 text-primary animate-spin" />
          </div>
        ) : comments.length > 0 ? (
          <div className="space-y-6 max-h-[500px] overflow-y-auto pr-2 custom-scrollbar">
            {comments.map((comment) => (
              <CommentCard
                key={comment.id}
                comment={comment}
                onDelete={readOnly ? undefined : () => handleDeleteComment(comment.id)}
              />
            ))}
            <div ref={commentsEndRef} />
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center py-12 text-text-muted bg-surface-1 rounded-2xl border-2 border-dashed border-border-subtle">
            <MessageSquare className="w-12 h-12 mb-3 opacity-20" />
            <p className="text-sm font-medium">No comments yet. Be the first!</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default TaskComments;
