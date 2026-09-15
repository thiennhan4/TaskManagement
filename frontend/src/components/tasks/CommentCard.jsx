import React from 'react';
import { Trash2, Clock } from 'lucide-react';

export const CommentCard = ({ comment, onDelete }) => {
  const { content, userName, createdAt, isOwner } = comment;

  return (
    <div className="flex gap-4 group animate-in slide-in-from-left-2 duration-300">
      <div className="flex-shrink-0 w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center text-sm font-bold text-primary border-2 border-surface-0 shadow-sm">
        {userName?.charAt(0).toUpperCase()}
      </div>
      
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-sm font-bold text-text-main">{userName}</span>
          <span className="text-[10px] font-medium text-text-subtle flex items-center gap-1 bg-surface-1 px-2 py-0.5 rounded">
            <Clock className="w-3 h-3" />
            {new Date(createdAt).toLocaleString()}
          </span>
        </div>
        
        <div className="relative bg-surface-1 rounded-2xl p-4 text-sm text-text-muted leading-relaxed border border-border-subtle group-hover:bg-surface-0 group-hover:shadow-sm transition-all">
          <p className="whitespace-pre-wrap">{content}</p>
          
          {isOwner && (
            <button
              onClick={onDelete}
              className="absolute -right-2 -top-2 p-1.5 bg-surface-0 border border-border-subtle text-text-subtle hover:text-red-600 hover:border-red-100 rounded-lg shadow-sm opacity-0 group-hover:opacity-100 transition-all"
              title="Delete Comment"
            >
              <Trash2 className="w-3.5 h-3.5" />
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
