import React, { useEffect, useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
import { useLanguage } from '@/context/LanguageContext';
import Modal from '@/components/ui/Modal';
import { useMediaQuery } from '@/hooks/useMediaQuery';

const AppLayout = () => {
  const { t } = useLanguage();
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const desktop = useMediaQuery('(min-width: 1024px)');
  useEffect(() => {
    const media = window.matchMedia?.('(min-width: 1024px)');
    const resize = event => { if (event.matches) setIsMobileMenuOpen(false); };
    media?.addEventListener('change', resize);
    return () => media?.removeEventListener('change', resize);
  }, []);

  return (
    <div className="min-h-screen bg-bg-main flex">
      {/* Sidebar for Desktop */}
      <div className="hidden lg:block transition-all duration-300">
        <Sidebar 
          isCollapsed={isSidebarCollapsed} 
          setIsCollapsed={setIsSidebarCollapsed} 
        />
      </div>

      <Modal isOpen={isMobileMenuOpen && !desktop} onClose={() => setIsMobileMenuOpen(false)} title="Navigation" placement="left" maxWidth="max-w-72">
        <Sidebar mobile isCollapsed={false} setIsCollapsed={() => setIsMobileMenuOpen(false)} onNavigate={() => setIsMobileMenuOpen(false)} />
      </Modal>

      {/* Main Content Area */}
      <div className={`
        min-w-0 flex-1 flex flex-col transition-all duration-300 motion-reduce:transition-none
        ${!isSidebarCollapsed ? 'lg:ml-64' : 'lg:ml-20'}
      `}>
        <Topbar onMenuClick={() => setIsMobileMenuOpen(true)} />
        
        <main id="main-content" className="min-w-0 flex-1 p-3 sm:p-6 lg:p-8 max-w-[1600px] mx-auto w-full">
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
