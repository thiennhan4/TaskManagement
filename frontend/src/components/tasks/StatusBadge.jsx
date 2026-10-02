import { TASK_STATUS as statusConfig } from '@/constants/taskStatus';
import React from 'react';
import PropTypes from 'prop-types';



export const StatusBadge = ({ status }) => {
  const config = statusConfig[status] || statusConfig.Todo;
  
  return (
    <span className={`px-2.5 py-0.5 rounded-full text-xs font-medium transition-colors ${config.bg} ${config.text}`}>
      {config.label}
    </span>
  );
};

StatusBadge.propTypes = {
  status: PropTypes.oneOf(['Todo', 'InProgress', 'Review', 'Done']).isRequired
};
