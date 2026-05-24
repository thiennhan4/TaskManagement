import React from 'react';
import { Trash2, Clock } from 'lucide-react';

export const CommentCard = ({ comment, onDelete }) => {
  const { content, userName, createdAt, isOwner } = comment;

  return (
    <div className="flex gap-4 group animate-in slide-in-from-left-2 duration-300">
      <div className="flex-shrink-0 w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center text-sm font-bold text-blue-700 border-2 border-white shadow-sm">
        {userName?.charAt(0).toUpperCase()}
      </div>
      
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-sm font-bold text-gray-900">{userName}</span>
          <span className="text-[10px] font-medium text-gray-400 flex items-center gap-1 bg-gray-50 px-2 py-0.5 rounded">
            <Clock className="w-3 h-3" />
            {new Date(createdAt).toLocaleString()}
          </span>
        </div>
        
        <div className="relative bg-gray-50 rounded-2xl p-4 text-sm text-gray-700 leading-relaxed border border-gray-100 group-hover:bg-white group-hover:shadow-sm transition-all">
          <p className="whitespace-pre-wrap">{content}</p>
          
          {isOwner && (
            <button
              onClick={onDelete}
              className="absolute -right-2 -top-2 p-1.5 bg-white border border-gray-200 text-gray-400 hover:text-red-600 hover:border-red-100 rounded-lg shadow-sm opacity-0 group-hover:opacity-100 transition-all"
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
