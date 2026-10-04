import React from 'react';
import { NavLink } from 'react-router-dom';
import { 
  LayoutDashboard, 
  CheckSquare, 
  Settings, 
  FolderKanban,
  ChevronLeft,
  ChevronRight,
  Calendar, Users
} from 'lucide-react';
import BrandLogo from '@/components/common/BrandLogo';
import { useLanguage } from '@/context/LanguageContext';

const Sidebar = ({ isCollapsed, setIsCollapsed, mobile = false, onNavigate }) => {
  const { t } = useLanguage();
  const menuItems = [
    { icon: LayoutDashboard, label: t('nav.dashboard'), path: '/dashboard' },
    { icon: FolderKanban, label: t('nav.projects'), path: '/projects' },
    { icon: Calendar, label: t('nav.calendar'), path: '/calendar' },
    { icon: CheckSquare, label: t('nav.myTasks'), path: '/my-tasks' },
    { icon: Users, label: 'Teams', path: '/teams' },
    { icon: Settings, label: t('nav.settings'), path: '/settings' },
  ];

  return (
    <aside 
      className={`
        flex flex-col bg-bg-sidebar text-text-main transition-all duration-300 motion-reduce:transition-none
        ${mobile ? 'relative min-h-full w-full' : `fixed left-0 top-0 h-dvh overflow-y-auto z-50 ${isCollapsed ? 'w-20' : 'w-64'}`}
      `}
    >
      {/* Sidebar Header */}
      <div className="h-16 flex items-center justify-between px-6 border-b border-border-subtle" onClick={event => { if (event.target.closest('a')) onNavigate?.(); }}>
        {!isCollapsed && (
          <BrandLogo size="sm" tone="light" to="/dashboard" />
        )}
        {!mobile && <button aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          onClick={() => setIsCollapsed(!isCollapsed)}
          className={`p-1.5 rounded-lg bg-hover-bg hover:bg-surface-2 transition-colors ${isCollapsed ? 'mx-auto' : ''}`}
        >
          {isCollapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
        </button>}
      </div>

      {/* Navigation Links */}
      <nav aria-label="Main navigation" className="mt-6 px-3 pb-6 space-y-1">
        {menuItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            aria-label={item.label}
            onClick={onNavigate}
            className={({ isActive }) => `
              sidebar-link 
              ${isActive ? 'sidebar-link-active' : ''}
              ${isCollapsed ? 'justify-center px-0' : ''}
            `}
          >
            <item.icon size={22} className="flex-shrink-0" />
            {!isCollapsed && <span className="font-medium">{item.label}</span>}
            {isCollapsed && (
              <div className="absolute left-full ml-2 px-2 py-1 bg-slate-800 text-white text-xs rounded opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity whitespace-nowrap">
                {item.label}
              </div>
            )}
          </NavLink>
        ))}
      </nav>

      {/* Sidebar Footer - Upgrade Card */}
      {!isCollapsed && (
        <div className="mx-6 mb-6 mt-auto p-4 bg-primary/70 border border-primary-dark/40 rounded-3xl shadow-premium">
          <p className="text-xs font-bold text-text-inverse uppercase tracking-wider mb-1">{t('sidebar.proPlan')}</p>
          <p className="text-sm font-medium text-text-main mb-3">{t('sidebar.proDesc')}</p>
          <button disabled aria-label="Billing unavailable" className="w-full py-2 bg-secondary text-white text-xs font-bold rounded-2xl hover:bg-text-main transition-colors">
            Billing unavailable
          </button>
        </div>
      )}
    </aside>
  );
};

export default Sidebar;
