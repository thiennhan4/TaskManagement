import React from 'react';
import { Info, MessageSquare, Paperclip, Activity } from 'lucide-react';

export const TaskDetailTabs = ({ activeTab, onTabChange }) => {
  const tabs = [
    { id: 'overview', label: 'Overview', icon: Info },
    { id: 'comments', label: 'Comments', icon: MessageSquare },
    { id: 'attachments', label: 'Attachments', icon: Paperclip },
    { id: 'activity', label: 'Activity', icon: Activity },
  ];

  return (
    <div className="flex items-center px-6 bg-surface-1/50 border-b border-border-subtle">
      {tabs.map((tab) => {
        const Icon = tab.icon;
        const isActive = activeTab === tab.id;
        
        return (
          <button
            key={tab.id}
            onClick={() => onTabChange(tab.id)}
            className={`
              flex items-center gap-2 px-4 py-4 text-sm font-bold transition-all border-b-2 
              ${isActive 
                ? 'border-primary text-primary' 
                : 'border-transparent text-text-subtle hover:text-text-muted hover:border-border-subtle'}
            `}
          >
            <Icon className="w-4 h-4" />
            <span>{tab.label}</span>
          </button>
        );
      })}
    </div>
  );
};
