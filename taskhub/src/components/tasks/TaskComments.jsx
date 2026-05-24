import React, { useState, useEffect, useRef } from 'react';
import { Send, Loader2, MessageSquare } from 'lucide-react';
import * as taskService from '../../services/taskService';
import { CommentCard } from './CommentCard';
import { CommentForm } from './CommentForm';
import { toast } from 'react-hot-toast';

const TaskComments = ({ taskId }) => {
  const [comments, setComments] = useState([]);
  const [loading, setLoading] = useState(false);
  const commentsEndRef = useRef(null);

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

  const scrollToBottom = () => {
    commentsEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [comments]);

  const handleAddComment = async (content) => {
    try {
      const result = await taskService.addComment(taskId, content);
      if (result.success) {
        toast.success('Comment added');
        fetchComments();
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
      <div className="mb-8">
        <CommentForm onSubmit={handleAddComment} />
      </div>

      {/* Comment List */}
      <div className="flex-1 space-y-6">
        {loading && comments.length === 0 ? (
          <div className="flex justify-center py-10">
            <Loader2 className="w-6 h-6 text-blue-600 animate-spin" />
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
          <div className="flex flex-col items-center justify-center py-12 text-gray-400 bg-gray-50 rounded-2xl border-2 border-dashed border-gray-100">
            <MessageSquare className="w-12 h-12 mb-3 opacity-20" />
            <p className="text-sm font-medium">No comments yet. Be the first!</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default TaskComments;
