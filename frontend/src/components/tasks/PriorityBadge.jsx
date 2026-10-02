import { TASK_PRIORITY as priorityConfig } from '@/constants/taskStatus';
import React from 'react';
import PropTypes from 'prop-types';
import { AlertCircle, ArrowUpCircle, ArrowRightCircle, ArrowDownCircle } from 'lucide-react';



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
