import React from 'react';
import PropTypes from 'prop-types';

const statusConfig = {
  Todo: { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-400', label: 'To Do' },
  InProgress: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-400', label: 'In Progress' },
  Review: { bg: 'bg-purple-100 dark:bg-purple-900/30', text: 'text-purple-800 dark:text-purple-400', label: 'Review' },
  Done: { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-800 dark:text-green-400', label: 'Done' }
};

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
