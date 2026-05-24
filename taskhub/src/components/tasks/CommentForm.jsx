import React, { useState } from 'react';
import { Send, Loader2 } from 'lucide-react';

export const CommentForm = ({ onSubmit }) => {
  const [content, setContent] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!content.trim() || submitting) return;

    setSubmitting(true);
    await onSubmit(content);
    setContent('');
    setSubmitting(false);
  };

  return (
    <form onSubmit={handleSubmit} className="relative group">
      <textarea
        value={content}
        onChange={(e) => setContent(e.target.value)}
        placeholder="Add a comment... (Enter to send)"
        className="w-full px-4 py-4 pr-16 rounded-2xl border border-gray-200 bg-gray-50 text-sm font-medium placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white focus:border-transparent transition-all min-h-[100px] resize-none"
        onKeyDown={(e) => {
          if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSubmit(e);
          }
        }}
      />
      <button
        type="submit"
        disabled={!content.trim() || submitting}
        className={`
          absolute bottom-3 right-3 p-2.5 rounded-xl transition-all shadow-lg
          ${content.trim() ? 'bg-blue-600 text-white shadow-blue-100 hover:scale-105 active:scale-95' : 'bg-gray-100 text-gray-400 shadow-none'}
        `}
      >
        {submitting ? (
          <Loader2 className="w-5 h-5 animate-spin" />
        ) : (
          <Send className="w-5 h-5" />
        )}
      </button>
    </form>
  );
};
