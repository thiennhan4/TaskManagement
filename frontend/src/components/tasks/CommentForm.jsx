import React, { useState } from 'react';
import { Send, Loader2 } from 'lucide-react';

export const CommentForm = ({ onSubmit, onTyping }) => {
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

  const handleChange = (e) => {
    setContent(e.target.value);
    if (onTyping) {
      onTyping();
    }
  };

  return (
    <form onSubmit={handleSubmit} className="relative group">
      <textarea
        value={content}
        onChange={handleChange}
        placeholder="Add a comment... (Enter to send)"
        className="w-full px-4 py-4 pr-16 rounded-2xl border border-border-subtle bg-surface-1 text-text-main text-sm font-medium placeholder:text-text-subtle focus:outline-none focus:ring-2 focus:ring-primary/30 focus:bg-surface-0 focus:border-primary transition-all min-h-[100px] resize-none"
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
          ${content.trim() ? 'bg-primary text-white shadow-primary/20 hover:scale-105 active:scale-95' : 'bg-surface-2 text-text-subtle shadow-none'}
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
