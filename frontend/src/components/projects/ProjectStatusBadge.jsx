import React from 'react';
import Badge from '../ui/Badge';
import { useLanguage } from '@/context/LanguageContext';

const statusConfig = {
  Planning: { variant: 'neutral' },
  Active: { variant: 'primary' },
  OnHold: { variant: 'warning' },
  Completed: { variant: 'success' },
  Cancelled: { variant: 'danger' },
};

const ProjectStatusBadge = ({ status, className = '' }) => {
  const { t } = useLanguage();
  const config = statusConfig[status] || statusConfig.Planning;
  
  return (
    <Badge variant={config.variant} className={className}>
      {t(`projects.status.${status}`)}
    </Badge>
  );
};

export default ProjectStatusBadge;
