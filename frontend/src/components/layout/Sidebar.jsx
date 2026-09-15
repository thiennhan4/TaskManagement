import React from 'react';
import { NavLink } from 'react-router-dom';
import { 
  LayoutDashboard, 
  CheckSquare, 
  Settings, 
  FolderKanban,
  ChevronLeft,
  ChevronRight,
  Calendar
} from 'lucide-react';
import BrandLogo from '@/components/common/BrandLogo';
import { useLanguage } from '@/context/LanguageContext';

const Sidebar = ({ isCollapsed, setIsCollapsed }) => {
  const { t } = useLanguage();
  const menuItems = [
    { icon: LayoutDashboard, label: t('nav.dashboard'), path: '/dashboard' },
    { icon: FolderKanban, label: t('nav.projects'), path: '/projects' },
    { icon: Calendar, label: t('nav.calendar'), path: '/calendar' },
    { icon: CheckSquare, label: t('nav.myTasks'), path: '/my-tasks' },
    { icon: Settings, label: t('nav.settings'), path: '/settings' },
  ];

  return (
    <aside 
      className={`
        fixed left-0 top-0 h-full bg-bg-sidebar text-white transition-all duration-300 z-50
        ${isCollapsed ? 'w-20' : 'w-64'}
      `}
    >
      {/* Sidebar Header */}
      <div className="h-16 flex items-center justify-between px-6 border-b border-white/10">
        {!isCollapsed && (
          <BrandLogo size="sm" tone="dark" />
        )}
        <button 
          onClick={() => setIsCollapsed(!isCollapsed)}
          className={`p-1.5 rounded-lg bg-white/5 hover:bg-white/10 transition-colors ${isCollapsed ? 'mx-auto' : ''}`}
        >
          {isCollapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
        </button>
      </div>

      {/* Navigation Links */}
      <nav className="mt-6 px-3 space-y-1">
        {menuItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
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
        <div className="absolute bottom-6 left-6 right-6 p-4 bg-primary/20 border border-primary/30 rounded-2xl">
          <p className="text-xs font-bold text-primary-light uppercase tracking-wider mb-1">{t('sidebar.proPlan')}</p>
          <p className="text-sm font-medium text-white/90 mb-3">{t('sidebar.proDesc')}</p>
          <button className="w-full py-2 bg-primary text-white text-xs font-bold rounded-xl hover:bg-primary-dark transition-colors">
            {t('sidebar.upgrade')}
          </button>
        </div>
      )}
    </aside>
  );
};

export default Sidebar;
