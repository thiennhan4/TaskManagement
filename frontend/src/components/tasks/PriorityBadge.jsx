import React from 'react';
import PropTypes from 'prop-types';
import { AlertCircle, ArrowUpCircle, ArrowRightCircle, ArrowDownCircle } from 'lucide-react';

const priorityConfig = {
  Critical: { color: 'text-red-500', bg: 'bg-red-50 dark:bg-red-900/20', icon: AlertCircle, label: 'Critical' },
  High: { color: 'text-orange-500', bg: 'bg-orange-50 dark:bg-orange-900/20', icon: ArrowUpCircle, label: 'High' },
  Medium: { color: 'text-yellow-500', bg: 'bg-yellow-50 dark:bg-yellow-900/20', icon: ArrowRightCircle, label: 'Medium' },
  Low: { color: 'text-green-500', bg: 'bg-green-50 dark:bg-green-900/20', icon: ArrowDownCircle, label: 'Low' }
};

export const PriorityBadge = ({ priority, showLabel = true }) => {
  const config = priorityConfig[priority] || priorityConfig.Medium;
  const Icon = config.icon;
  
  return (
    <div className={`flex items-center gap-1.5 px-2 py-0.5 rounded text-xs font-medium transition-colors ${config.color} ${showLabel ? config.bg : ''}`}>
      <Icon className="w-4 h-4" />
      {showLabel && <span>{config.label}</span>}
    </div>
  );
};

PriorityBadge.propTypes = {
  priority: PropTypes.oneOf(['Low', 'Medium', 'High', 'Critical']).isRequired,
  showLabel: PropTypes.bool
};
