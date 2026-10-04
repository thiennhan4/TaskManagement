import React from 'react';
import { Lock, Globe, Users } from 'lucide-react';

const ProjectVisibilityIcon = ({ visibility, className = 'w-4 h-4' }) => {
  switch (visibility) {
    case 'Private':
      return <Lock className={`${className} text-rose-500`} />;
    case 'TeamOnly':
      return <Users className={`${className} text-blue-500`} />;
    case 'Public':
      return <Globe className={`${className} text-emerald-500`} />;
    default:
      return <Lock className={className} />;
  }
};

export default ProjectVisibilityIcon;
