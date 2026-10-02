import React, { useCallback, useState, useEffect } from 'react';
import { useProjectStore } from '../../stores/useProjectStore';
import { Plus, Briefcase, Archive, Trash2, Loader2 } from 'lucide-react';
import Button from '@/components/ui/Button';
import PageControls from '@/components/ui/PageControls';
import ProjectCard from '@/components/projects/ProjectCard';
import ProjectFilterBar from '@/components/projects/ProjectFilterBar';
import CreateProjectModal from '@/components/projects/CreateProjectModal';
import EditProjectModal from '@/components/projects/EditProjectModal';
import { useNavigate } from 'react-router-dom';
import { useLanguage } from '@/context/LanguageContext';

const ProjectsPage = ({ isArchivedView = false }) => {
  const navigate = useNavigate();
  const { t } = useLanguage();
  const { projects, projectPage, error, isLoading, fetchProjects, deleteProject } = useProjectStore();
  const [page, setPage] = useState(1);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [viewMode, setViewMode] = useState('grid');


  // Edit/Delete state
  const [editingProject, setEditingProject] = useState(null);
  const [deletingProject, setDeletingProject] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  const refreshPage = useCallback(() => fetchProjects({ page, search: searchQuery, status: statusFilter === 'All' ? undefined : statusFilter, archivedOnly: isArchivedView }),
    [fetchProjects, page, searchQuery, statusFilter, isArchivedView]);
  useEffect(() => { refreshPage(); }, [refreshPage]);

  const handleDeleteProject = async () => {
    if (!deletingProject) return;
    setDeleteLoading(true);
    try {
      await deleteProject(deletingProject.id);
      if (projects.length === 1 && page > 1) setPage(page - 1);
      else await refreshPage();
      setDeletingProject(null);
    } catch {
      // toast is handled in store
    } finally {
      setDeleteLoading(false);
    }
  };

  const filteredProjects = projects;

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-text-main tracking-tight flex items-center gap-3">
            <Briefcase className="text-primary w-8 h-8" />
            {isArchivedView ? t('projects.archivedTitle') : t('projects.title')}
          </h1>
          <p className="text-text-muted mt-1 font-medium">
            {isArchivedView ? t('projects.archivedSubtitle') : t('projects.subtitle')}
          </p>
        </div>
        <div className="flex gap-3">
          <Button 
            variant="outline" 
            leftIcon={<Archive size={18} />}
            onClick={() => navigate(isArchivedView ? '/projects' : '/projects/archived')}
          >
            {isArchivedView ? t('projects.backToActive') : t('projects.archivedButton')}
          </Button>
          <Button 
            leftIcon={<Plus size={18} />} 
            onClick={() => setIsCreateModalOpen(true)}
            disabled={isArchivedView}
          >
            {t('projects.newProject')}
          </Button>
        </div>
      </div>

      <ProjectFilterBar 
        searchQuery={searchQuery}
        setSearchQuery={value => { setPage(1); setSearchQuery(value); }}
        statusFilter={statusFilter}
        setStatusFilter={value => { setPage(1); setStatusFilter(value); }}
        viewMode={viewMode}
        setViewMode={setViewMode}
      />

      <PageControls page={projectPage} loading={isLoading} onPage={setPage} />
      {error ? <p role="alert">{error}</p> : isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3, 4, 5, 6].map(i => (
            <div key={i} className="h-48 rounded-2xl bg-surface-2 animate-pulse" />
          ))}
        </div>
      ) : filteredProjects.length === 0 ? (
        <div className="text-center py-20 bg-surface-0 rounded-3xl border border-dashed border-border-subtle">
          <div className="w-20 h-20 bg-surface-2 rounded-full flex items-center justify-center mx-auto mb-6">
            <Briefcase className="w-10 h-10 text-text-subtle" />
          </div>
          <h3 className="text-xl font-bold text-text-main">{t('projects.empty.title')}</h3>
          <p className="text-text-muted mb-8 max-w-md mx-auto">
            {searchQuery || statusFilter !== 'All' 
              ? t('projects.empty.filtered') 
              : isArchivedView
                ? t('projects.empty.archived')
                : t('projects.empty.default')}
          </p>
          {!(searchQuery || statusFilter !== 'All' || isArchivedView) && (
            <Button onClick={() => setIsCreateModalOpen(true)}>{t('projects.createProject')}</Button>
          )}
        </div>
      ) : (
        <div className={viewMode === 'grid' 
          ? "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6" 
          : "flex flex-col gap-4"
        }>
          {filteredProjects.map(project => (
            <ProjectCard 
              key={project.id} 
              project={project} 
              onEdit={setEditingProject}
              onDelete={setDeletingProject}
            />
          ))}
        </div>
      )}

      <CreateProjectModal 
        isOpen={isCreateModalOpen} 
        onClose={() => { setIsCreateModalOpen(false); refreshPage(); }}
      />

      <EditProjectModal
        isOpen={!!editingProject}
        onClose={() => { setEditingProject(null); refreshPage(); }}
        project={editingProject}
      />

      {/* Delete Confirm Dialog */}
      {deletingProject && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !deleteLoading && setDeletingProject(null)} />
          <div className="relative bg-surface-0 rounded-2xl border border-border-subtle shadow-2xl w-full max-w-md p-6 animate-in fade-in zoom-in duration-200">
            <div className="flex items-center gap-4 mb-4">
              <div className="w-12 h-12 rounded-xl bg-red-500/10 flex items-center justify-center shrink-0">
                <Trash2 size={22} className="text-red-500" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-text-main">{t('projects.deleteProject')}</h3>
                <p className="text-sm text-text-muted mt-0.5">{t('projects.deleteWarning')}</p>
              </div>
            </div>
            <p className="text-sm text-text-muted bg-surface-2 rounded-xl p-4 mb-6">
              {t('projects.deleteConfirm', { name: deletingProject.name })}
            </p>
            <div className="flex gap-3">
              <Button variant="ghost" className="flex-1" onClick={() => setDeletingProject(null)} disabled={deleteLoading}>
                {t('common.cancel')}
              </Button>
              <button
                onClick={handleDeleteProject}
                disabled={deleteLoading}
                className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-red-500 hover:bg-red-600 text-white font-bold rounded-xl transition-colors disabled:opacity-70"
              >
                {deleteLoading ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                {deleteLoading ? (t('common.delete') + '...') : t('projects.deleteProject')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProjectsPage;
