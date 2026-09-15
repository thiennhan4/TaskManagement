import React, { useState, useEffect, useRef } from 'react';
import { Send, Loader2, MessageSquare } from 'lucide-react';
import * as taskService from '../../services/taskService';
import { CommentCard } from './CommentCard';
import { CommentForm } from './CommentForm';
import { toast } from 'react-hot-toast';
import { useAuth } from '@/context/AuthContext';
import { useNotification } from '@/context/NotificationContext';

const TaskComments = ({ taskId }) => {
  const { user } = useAuth();
  const { hubConnection } = useNotification();
  const [comments, setComments] = useState([]);
  const [loading, setLoading] = useState(false);
  const [typingUsers, setTypingUsers] = useState([]);
  
  const commentsEndRef = useRef(null);
  const typingTimeoutRef = useRef(null);

  const fetchComments = async () => {
    setLoading(true);
    try {
      const result = await taskService.getComments(taskId);
      if (result.success) {
        setComments(result.data);
      }
    } catch (err) {
      toast.error('Failed to load comments');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchComments();
  }, [taskId]);

  useEffect(() => {
    if (!hubConnection) return;

    const taskIdStr = String(taskId);
    hubConnection.invoke('JoinTask', taskIdStr);

    hubConnection.on('CommentAdded', (newComment) => {
      setComments(prev => {
        if (prev.some(c => c.id === newComment.id)) return prev;
        const commentWithOwnership = {
          ...newComment,
          isOwner: newComment.userId === user?.id
        };
        return [...prev, commentWithOwnership];
      });
    });

    hubConnection.on('CommentDeleted', (deletedCommentId) => {
      setComments(prev => prev.filter(c => c.id !== deletedCommentId));
    });

    hubConnection.on('UserTyping', (tid, name, isTyping) => {
      if (tid !== taskIdStr) return;
      setTypingUsers(prev => {
        if (isTyping) {
          if (prev.includes(name)) return prev;
          return [...prev, name];
        } else {
          return prev.filter(n => n !== name);
        }
      });
    });

    return () => {
      hubConnection.invoke('LeaveTask', taskIdStr);
      hubConnection.off('CommentAdded');
      hubConnection.off('CommentDeleted');
      hubConnection.off('UserTyping');
    };
  }, [hubConnection, taskId, user]);

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
      const result = await taskService.addComment(taskId, content);
      if (result.success) {
        toast.success('Comment added');
        // Stop typing immediately when comment is sent
        if (typingTimeoutRef.current) {
          clearTimeout(typingTimeoutRef.current);
        }
        if (hubConnection && user) {
          hubConnection.invoke('StopTyping', String(taskId), user.fullName);
        }
      }
    } catch (err) {
      toast.error('Failed to add comment');
    }
  };

  const handleDeleteComment = async (commentId) => {
    try {
      const result = await taskService.deleteComment(taskId, commentId);
      if (result.success) {
        toast.success('Comment deleted');
        setComments(comments.filter(c => c.id !== commentId));
      }
    } catch (err) {
      toast.error('Failed to delete comment');
    }
  };

  return (
    <div className="flex flex-col h-full animate-in fade-in slide-in-from-bottom-2 duration-300">
      {/* Comment Form */}
      <div className="mb-4">
        <CommentForm onSubmit={handleAddComment} onTyping={handleTyping} />
        {typingUsers.length > 0 && (
          <p className="text-xs text-text-subtle italic mt-2.5 ml-2.5 animate-pulse font-medium">
            {typingUsers.join(', ')} {typingUsers.length === 1 ? 'is' : 'are'} typing...
          </p>
        )}
      </div>

      {/* Comment List */}
      <div className="flex-1 space-y-6">
        {loading && comments.length === 0 ? (
          <div className="flex justify-center py-10">
            <Loader2 className="w-6 h-6 text-primary animate-spin" />
          </div>
        ) : comments.length > 0 ? (
          <div className="space-y-6 max-h-[500px] overflow-y-auto pr-2 custom-scrollbar">
            {comments.map((comment) => (
              <CommentCard
                key={comment.id}
                comment={comment}
                onDelete={() => handleDeleteComment(comment.id)}
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
