import React from 'react';
import { Link } from 'react-router-dom';
import {
  ArrowRight,
  Users,
  Zap,
  Shield, 
  MousePointer2, 
  Sparkles,
  BarChart3,
  Globe
} from 'lucide-react';
import Button from '@/components/ui/Button';
import WorkflowSection from '@/components/landing/WorkflowSection';
import BrandLogo from '@/components/common/BrandLogo';
import LanguageSwitcher from '@/components/common/LanguageSwitcher';
import { useLanguage } from '@/context/LanguageContext';

export default function LandingPage() {
  const { t } = useLanguage();

  return (
    <div className="min-h-screen bg-bg-main font-sans selection:bg-primary selection:text-white transition-colors duration-300">
      {/* Navigation */}
      <nav className="fixed top-0 w-full z-50 bg-bg-main/80 backdrop-blur-md border-b border-border-subtle px-6 py-4">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <BrandLogo />
          
          <div className="hidden md:flex items-center gap-8 text-sm font-bold text-slate-500">
            <a href="#features" className="hover:text-primary transition-colors">{t('nav.features')}</a>
            <a href="#workflow" className="hover:text-primary transition-colors">{t('nav.workflow')}</a>
            <a href="#pricing" className="hover:text-primary transition-colors">{t('nav.pricing')}</a>
          </div>

          <div className="flex items-center gap-3">
            <LanguageSwitcher compact className="hidden sm:inline-flex" />
            <Link to="/login">
              <Button variant="ghost" size="sm">{t('nav.signIn')}</Button>
            </Link>
            <Link to="/register">
              <Button size="sm">{t('nav.getStarted')}</Button>
            </Link>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="relative pt-32 pb-20 px-6 overflow-hidden">
        {/* Background Gradients */}
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full h-full -z-10 opacity-30">
          <div className="absolute top-20 left-1/4 w-96 h-96 bg-primary/40 rounded-full blur-[120px]"></div>
          <div className="absolute bottom-20 right-1/4 w-96 h-96 bg-secondary/40 rounded-full blur-[120px]"></div>
        </div>

        <div className="max-w-5xl mx-auto text-center">
          <h1 className="text-5xl md:text-7xl font-black text-text-main leading-[1.1] tracking-tight mb-8">
            {t('landing.heroTitleStart')} <span className="text-transparent bg-clip-text bg-gradient-to-r from-primary to-secondary">{t('landing.heroTitleHighlight')}</span>
          </h1>
          
          <p className="text-lg md:text-xl text-text-muted max-w-2xl mx-auto mb-10 font-medium leading-relaxed">
            {t('landing.heroDescription')}
          </p>

          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link to="/register" className="w-full sm:w-auto">
              <Button size="lg" className="w-full sm:px-10 h-14 text-lg" rightIcon={<ArrowRight size={20} />}>
                {t('landing.startFree')}
              </Button>
            </Link>
            <Link to="/login" className="w-full sm:w-auto">
              <Button variant="outline" size="lg" className="w-full h-14 text-lg">
                {t('landing.viewDemo')}
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* Feature Section */}
      <section id="features" className="py-24 bg-bg-main">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-16">
            <h2 className="text-sm font-black text-primary uppercase tracking-widest mb-3">{t('landing.capabilities')}</h2>
            <h3 className="text-3xl md:text-4xl font-black text-text-main tracking-tight">{t('landing.featuresTitle')}</h3>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <FeatureCard 
              icon={<MousePointer2 className="text-primary" />}
              title={t('landing.feature.kanban.title')}
              description={t('landing.feature.kanban.desc')}
            />
            <FeatureCard 
              icon={<Zap className="text-amber-500" />}
              title={t('landing.feature.fast.title')}
              description={t('landing.feature.fast.desc')}
            />
            <FeatureCard 
              icon={<Users className="text-blue-500" />}
              title={t('landing.feature.team.title')}
              description={t('landing.feature.team.desc')}
            />
            <FeatureCard 
              icon={<Shield className="text-emerald-500" />}
              title={t('landing.feature.security.title')}
              description={t('landing.feature.security.desc')}
            />
            <FeatureCard 
              icon={<BarChart3 className="text-rose-500" />}
              title={t('landing.feature.analytics.title')}
              description={t('landing.feature.analytics.desc')}
            />
            <FeatureCard 
              icon={<Globe className="text-indigo-500" />}
              title={t('landing.feature.anywhere.title')}
              description={t('landing.feature.anywhere.desc')}
            />
          </div>
        </div>
      </section>

      <WorkflowSection />

      {/* Footer Section */}
      <footer className="bg-bg-main py-16 px-6 border-t border-border-subtle">
        <div className="max-w-7xl mx-auto">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-12 mb-12">
            <div className="col-span-1 md:col-span-1">
              <BrandLogo size="sm" className="mb-6" />
              <p className="text-sm text-text-muted leading-relaxed font-medium">
                {t('landing.footer.desc')}
              </p>
            </div>
            <div>
              <h4 className="font-bold text-text-main mb-6">{t('landing.footer.product')}</h4>
              <ul className="space-y-4 text-sm text-text-muted font-medium">
                <li><a href="#" className="hover:text-primary transition-colors">{t('nav.features')}</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">{t('landing.footer.integrations')}</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">{t('landing.footer.enterprise')}</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">{t('landing.footer.solutions')}</a></li>
              </ul>
            </div>
            <div>
              <h4 className="font-bold text-text-main mb-6">{t('landing.footer.resources')}</h4>
              <ul className="space-y-4 text-sm text-text-muted font-medium">
                <li><a href="#" className="hover:text-primary transition-colors">{t('landing.footer.docs')}</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">{t('landing.footer.api')}</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">{t('landing.footer.guides')}</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">{t('landing.footer.support')}</a></li>
              </ul>
            </div>
            <div>
              <h4 className="font-bold text-text-main mb-6">{t('landing.footer.connect')}</h4>
              <ul className="space-y-4 text-sm text-text-muted font-medium">
                <li><a href="#" className="hover:text-primary transition-colors">Twitter</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">Discord</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">LinkedIn</a></li>
                <li><a href="#" className="hover:text-primary transition-colors">GitHub</a></li>
              </ul>
            </div>
          </div>
          <div className="pt-8 border-t border-border-subtle flex flex-col md:flex-row items-center justify-between gap-4">
            <p className="text-xs text-text-muted font-bold uppercase tracking-widest">&copy; 2026 {t('landing.footer.rights')}</p>
            <div className="flex gap-6 text-xs font-bold text-text-muted uppercase tracking-widest">
              <a href="#" className="hover:text-text-main transition-colors">{t('landing.footer.privacy')}</a>
              <a href="#" className="hover:text-text-main transition-colors">{t('landing.footer.terms')}</a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}

function Badge({ children, variant = 'primary', className = '' }) {
  const styles = variant === 'primary' ? 'bg-primary/10 text-primary' : 'bg-slate-100 text-slate-500';
  return (
    <span className={`px-2 py-0.5 rounded-full text-[10px] font-black uppercase tracking-wider ${styles} ${className}`}>
      {children}
    </span>
  );
}

function FeatureCard({ icon, title, description }) {
  return (
    <div className="p-8 bg-bg-card rounded-3xl border border-border-subtle shadow-sm hover:shadow-xl transition-all duration-300 group">
      <div className="w-12 h-12 bg-bg-main rounded-2xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform duration-300">
        {icon}
      </div>
      <h4 className="text-lg font-black text-text-main mb-3">{title}</h4>
      <p className="text-sm text-text-muted font-medium leading-relaxed">{description}</p>
    </div>
  );
}

function LogoPlaceholder({ name }) {
  return (
    <div className="flex items-center gap-2 font-black text-xl tracking-widest text-slate-900">
      <Sparkles size={20} className="text-slate-900" />
      <span>{name}</span>
    </div>
  );
}
