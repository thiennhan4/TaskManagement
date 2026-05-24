import React from 'react';
import { Search, LayoutGrid, List } from 'lucide-react';
import { useLanguage } from '@/context/LanguageContext';

const ProjectFilterBar = ({ 
  searchQuery, 
  setSearchQuery, 
  statusFilter, 
  setStatusFilter,
  viewMode,
  setViewMode
}) => {
  const { t } = useLanguage();
  const statusOptions = ['All', 'Active', 'Planning', 'Completed'];

  return (
    <div className="flex flex-col md:flex-row gap-4 items-center justify-between bg-white p-4 rounded-2xl shadow-sm border border-slate-100">
      <div className="relative w-full md:w-96">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 w-4 h-4" />
        <input
          type="text"
          placeholder={t('projects.filters.searchPlaceholder')}
          className="w-full pl-10 pr-4 py-2 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-primary/20 transition-all text-sm"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
      </div>

      <div className="flex items-center gap-3 w-full md:w-auto">
        <div className="flex items-center gap-2 bg-slate-50 p-1 rounded-xl">
          {statusOptions.map(status => (
            <button
              key={status}
              className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all ${statusFilter === status ? 'bg-white shadow-sm text-primary' : 'text-slate-500 hover:text-slate-700'}`}
              onClick={() => setStatusFilter(status)}
            >
              {t(`projects.status.${status}`)}
            </button>
          ))}
        </div>

        <div className="h-8 w-[1px] bg-slate-200 mx-1 hidden md:block" />

        <div className="flex items-center gap-1 bg-slate-50 p-1 rounded-xl">
          <button
            className={`p-1.5 rounded-lg transition-all ${viewMode === 'grid' ? 'bg-white shadow-sm text-primary' : 'text-slate-400'}`}
            onClick={() => setViewMode('grid')}
          >
            <LayoutGrid size={18} />
          </button>
          <button
            className={`p-1.5 rounded-lg transition-all ${viewMode === 'list' ? 'bg-white shadow-sm text-primary' : 'text-slate-400'}`}
            onClick={() => setViewMode('list')}
          >
            <List size={18} />
          </button>
        </div>
      </div>
    </div>
  );
};

export default ProjectFilterBar;
