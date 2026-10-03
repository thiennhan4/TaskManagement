import React, { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import Card from '@/components/ui/Card';
import ProjectStatusBadge from './ProjectStatusBadge';
import ProjectVisibilityIcon from './ProjectVisibilityIcon';
import { Users, Layout, Clock, UserPlus, MoreVertical, Edit2, Trash2 } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import InviteMemberModal from './InviteMemberModal';
import { useLanguage } from '@/context/LanguageContext';

const ProjectCard = ({ project, onEdit, onDelete }) => {
  const { t } = useLanguage();
  const [isInviteOpen, setIsInviteOpen] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  useEffect(() => {
    const handler = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
    };
    if (menuOpen) document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [menuOpen]);

  return (
    <>
      <Card 
        className="hover:shadow-lg transition-all cursor-pointer group border-l-4 relative"
        style={{ borderLeftColor: project.color || '#6366f1' }}
      >
        <div className="flex flex-wrap gap-2 justify-between items-start mb-4">
          <div className="min-w-0 flex items-center gap-3">
            <span className="text-2xl">{project.emoji || '📁'}</span>
            <div className="min-w-0">
              <h3 className="font-bold text-lg transition-colors break-words">
                <Link to={`/projects/${project.id}`} className="inline-block min-h-10 hover:underline">{project.name}</Link>
              </h3>
              <p className="break-all text-xs text-text-muted">/{project.slug}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <ProjectStatusBadge status={project.status} />

            {/* 3-dot menu */}
            <div className="relative" ref={menuRef}>
              <button
                type="button" aria-label={`Actions for ${project.name}`} aria-expanded={menuOpen}
                onClick={(e) => { e.stopPropagation(); setMenuOpen(o => !o); }}
                className="min-h-10 min-w-10 p-1.5 rounded-lg text-text-subtle hover:text-text-main hover:bg-hover-bg transition-colors"
              >
                <MoreVertical size={16} />
              </button>

              {menuOpen && (
                <div className="absolute right-0 top-full mt-1 w-44 bg-surface-0 border border-border-subtle rounded-xl shadow-lg z-20 overflow-hidden">
                  <button
                    onClick={(e) => { e.stopPropagation(); setMenuOpen(false); onEdit?.(project); }}
                    className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-text-main hover:bg-hover-bg transition-colors"
                  >
                    <Edit2 size={14} className="text-primary" />
                    {t('projects.editProject')}
                  </button>
                  <div className="h-px bg-border-subtle mx-2" />
                  <button
                    onClick={(e) => { e.stopPropagation(); setMenuOpen(false); onDelete?.(project); }}
                    className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-red-500 hover:bg-red-500/10 transition-colors"
                  >
                    <Trash2 size={14} />
                    {t('projects.deleteProject')}
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        <p className="text-sm text-muted-foreground line-clamp-2 mb-6 h-10">
          {project.description || t('projects.card.noDescription')}
        </p>

        <div className="flex flex-wrap gap-3 items-center justify-between pt-4 border-t text-xs text-text-muted">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-1">
              <Users className="w-3.5 h-3.5" />
              <span>{project.memberCount}</span>
            </div>
            <div className="flex items-center gap-1">
              <Layout className="w-3.5 h-3.5" />
              <span>{project.boardCount}</span>
            </div>
            <ProjectVisibilityIcon visibility={project.visibility} className="w-3.5 h-3.5" />
          </div>
          
          <div className="flex items-center gap-2">
            {project.workspaceId && (
              <button 
                onClick={(e) => {
                  e.stopPropagation();
                  setIsInviteOpen(true);
                }}
                className="p-1.5 hover:bg-primary/10 hover:text-primary rounded-full transition-colors"
                title={t('projects.invite.openButton')}
              >
                <UserPlus size={16} />
              </button>
            )}
            <div className="flex items-center gap-1">
              <Clock className="w-3.5 h-3.5" />
              <span>{formatDistanceToNow(new Date(project.createdAt), { addSuffix: true })}</span>
            </div>
          </div>
        </div>
      </Card>
      
      <InviteMemberModal 
        isOpen={isInviteOpen} 
        onClose={() => setIsInviteOpen(false)} 
        projectId={project.id} 
        projectName={project.name} 
      />
    </>
  );
};

export default ProjectCard;
