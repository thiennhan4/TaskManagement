import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
import { useLanguage } from '@/context/LanguageContext';

const AppLayout = () => {
  const { t } = useLanguage();
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  return (
    <div className="min-h-screen bg-bg-main flex">
      {/* Sidebar for Desktop */}
      <div className="hidden lg:block transition-all duration-300">
        <Sidebar 
          isCollapsed={isSidebarCollapsed} 
          setIsCollapsed={setIsSidebarCollapsed} 
        />
      </div>

      {/* Mobile Sidebar Overlay */}
      {isMobileMenuOpen && (
        <div 
          className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm z-[60] lg:hidden"
          onClick={() => setIsMobileMenuOpen(false)}
        />
      )}

      {/* Mobile Sidebar */}
      <div className={`
        fixed inset-y-0 left-0 z-[70] lg:hidden transition-transform duration-300
        ${isMobileMenuOpen ? 'translate-x-0' : '-translate-x-full'}
      `}>
        <Sidebar 
          isCollapsed={false} 
          setIsCollapsed={() => setIsMobileMenuOpen(false)} 
        />
      </div>

      {/* Main Content Area */}
      <div className={`
        flex-1 flex flex-col transition-all duration-300
        ${!isSidebarCollapsed ? 'lg:ml-64' : 'lg:ml-20'}
      `}>
        <Topbar onMenuClick={() => setIsMobileMenuOpen(true)} />
        
        <main className="flex-1 p-6 lg:p-8 max-w-[1600px] mx-auto w-full">
          <Outlet />
        </main>

        <footer className="py-6 px-8 text-center text-text-muted text-xs font-medium border-t border-border-subtle">
          &copy; {new Date().getFullYear()} TaskHub. {t('layout.footer')}
        </footer>
      </div>
    </div>
  );
};

export default AppLayout;
