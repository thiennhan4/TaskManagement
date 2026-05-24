import React, { useState, useEffect } from 'react';
import { useProjectStore } from '../../stores/useProjectStore';
import { Plus, Briefcase, Archive, Trash2, Loader2 } from 'lucide-react';
import Button from '@/components/ui/Button';
import ProjectCard from '@/components/projects/ProjectCard';
import ProjectFilterBar from '@/components/projects/ProjectFilterBar';
import CreateProjectModal from '@/components/projects/CreateProjectModal';
import EditProjectModal from '@/components/projects/EditProjectModal';
import { useNavigate } from 'react-router-dom';
import { useLanguage } from '@/context/LanguageContext';

const ProjectsPage = ({ isArchivedView = false }) => {
  const navigate = useNavigate();
  const { t } = useLanguage();
  const { projects, isLoading, fetchProjects, deleteProject } = useProjectStore();
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [viewMode, setViewMode] = useState('grid');
  const [hasLoaded, setHasLoaded] = useState(false);

  // Edit/Delete state
  const [editingProject, setEditingProject] = useState(null);
  const [deletingProject, setDeletingProject] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  useEffect(() => {
    if (hasLoaded) return;
    fetchProjects();
    setHasLoaded(true);
  }, [fetchProjects, hasLoaded]);

  const handleDeleteProject = async () => {
    if (!deletingProject) return;
    setDeleteLoading(true);
    try {
      await deleteProject(deletingProject.id);
      setDeletingProject(null);
    } catch (error) {
      // toast is handled in store
    } finally {
      setDeleteLoading(false);
    }
  };

  const filteredProjects = projects.filter(project => {
    const matchesSearch = project.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
                         project.slug.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesStatus = statusFilter === 'All' || project.status === statusFilter;
    return matchesSearch && matchesStatus && project.isArchived === isArchivedView;
  });

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-slate-900 tracking-tight flex items-center gap-3">
            <Briefcase className="text-primary w-8 h-8" />
            {isArchivedView ? t('projects.archivedTitle') : t('projects.title')}
          </h1>
          <p className="text-slate-500 mt-1 font-medium">
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
        setSearchQuery={setSearchQuery}
        statusFilter={statusFilter}
        setStatusFilter={setStatusFilter}
        viewMode={viewMode}
        setViewMode={setViewMode}
      />

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3, 4, 5, 6].map(i => (
            <div key={i} className="h-48 rounded-2xl bg-slate-100 animate-pulse" />
          ))}
        </div>
      ) : filteredProjects.length === 0 ? (
        <div className="text-center py-20 bg-white rounded-3xl border border-dashed border-slate-300">
          <div className="w-20 h-20 bg-slate-50 rounded-full flex items-center justify-center mx-auto mb-6">
            <Briefcase className="w-10 h-10 text-slate-300" />
          </div>
          <h3 className="text-xl font-bold text-slate-900">{t('projects.empty.title')}</h3>
          <p className="text-slate-500 mb-8 max-w-md mx-auto">
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
        onClose={() => setIsCreateModalOpen(false)} 
      />

      <EditProjectModal
        isOpen={!!editingProject}
        onClose={() => setEditingProject(null)}
        project={editingProject}
      />

      {/* Delete Confirm Dialog */}
      {deletingProject && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !deleteLoading && setDeletingProject(null)} />
          <div className="relative bg-white dark:bg-slate-800 rounded-2xl shadow-2xl w-full max-w-md p-6 animate-in fade-in zoom-in duration-200">
            <div className="flex items-center gap-4 mb-4">
              <div className="w-12 h-12 rounded-xl bg-red-100 flex items-center justify-center shrink-0">
                <Trash2 size={22} className="text-red-500" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-text-main">{t('projects.deleteProject')}</h3>
                <p className="text-sm text-text-muted mt-0.5">{t('projects.deleteWarning')}</p>
              </div>
            </div>
            <p className="text-sm text-text-muted bg-slate-50 dark:bg-slate-700/50 rounded-xl p-4 mb-6">
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
