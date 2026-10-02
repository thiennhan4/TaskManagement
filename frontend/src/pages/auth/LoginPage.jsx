import { safeReturnLocation } from '@/utils/returnLocation';
import { Link, useNavigate, useSearchParams, useLocation } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from '@/context/authState';
import { GoogleLogin } from "@react-oauth/google";
import { ArrowRight, ShieldCheck } from 'lucide-react';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import BrandLogo from '@/components/common/BrandLogo';
import LanguageSwitcher from '@/components/common/LanguageSwitcher';
import { useLanguage } from '@/context/LanguageContext';

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const { login, googleLogin } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const redirectTo = safeReturnLocation(location.state?.from || searchParams.get('redirect'));

  const handleGoogleSuccess = async (tokenResponse) => {
    try {
      setIsLoading(true);
      await googleLogin(tokenResponse.credential);
      navigate(redirectTo);
    } catch (err) {
      setError(err.message || t('auth.login.errorGoogle'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);
    try {
      await login(email, password);
      navigate(redirectTo);
    } catch (err) {
      setError(err.message || t('auth.login.errorInvalid'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex bg-surface-1 font-sans">
      {/* Left Panel: Branding & Marketing */}
      <div className="hidden lg:flex flex-col justify-between w-[40%] bg-bg-sidebar p-12 text-text-main relative overflow-hidden">
        {/* Animated Background Orbs */}
        <div className="absolute top-0 right-0 w-64 h-64 bg-primary/20 rounded-full blur-[100px] -mr-32 -mt-32"></div>
        <div className="absolute bottom-0 left-0 w-64 h-64 bg-secondary/20 rounded-full blur-[100px] -ml-32 -mb-32"></div>

        <div className="relative z-10">
          <div className="mb-16 flex items-center justify-between">
            <BrandLogo tone="light" />
            <LanguageSwitcher compact />
          </div>

          <h2 className="text-4xl font-black leading-tight tracking-tight mb-6">
            {t('auth.login.marketingTitle')} <br />
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-primary to-secondary">{t('auth.login.marketingHighlight')}</span>
          </h2>
          <p className="text-text-muted text-lg font-medium leading-relaxed max-w-md">
            {t('auth.login.marketingDesc')}
          </p>
        </div>

        <div className="relative z-10 space-y-6">
          <div className="flex items-center gap-4 p-4 bg-white/5 rounded-2xl border border-white/10 backdrop-blur-sm">
            <div className="w-10 h-10 rounded-xl bg-primary/20 flex items-center justify-center text-primary">
              <ShieldCheck size={20} />
            </div>
            <div>
              <p className="font-bold text-sm">{t('auth.login.securityTitle')}</p>
              <p className="text-xs text-text-muted font-medium">{t('auth.login.securityDesc')}</p>
            </div>
          </div>
          <p className="text-xs text-text-subtle font-bold uppercase tracking-widest">&copy; 2026 {t('auth.platform')}</p>
        </div>
      </div>

      {/* Right Panel: Login Form */}
      <div className="flex-1 flex flex-col justify-center px-6 py-12 md:px-24">
        <div className="max-w-md w-full mx-auto">
          <div className="lg:hidden flex justify-center mb-8">
            <div className="flex flex-col items-center gap-4">
              <BrandLogo />
              <LanguageSwitcher compact />
            </div>
          </div>

          <h1 className="text-3xl font-black text-text-main tracking-tight mb-2">{t('auth.login.title')}</h1>
          <p className="text-text-muted font-medium mb-8">{t('auth.login.subtitle')}</p>

          {/* Social Auth */}
          <div className="mb-8">
            <GoogleLogin
              onSuccess={handleGoogleSuccess}
              onError={() => setError(t('auth.login.errorGoogle'))}
              useOneTap
              width="100%"
              theme="outline"
              shape="pill"
            />
          </div>

          <div className="relative mb-8 text-center">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-border-subtle"></div>
            </div>
            <span className="relative px-4 bg-surface-1 text-xs font-bold text-text-subtle uppercase tracking-widest">
              {t('common.orWithEmail')}
            </span>
          </div>

          {/* Login Form */}
          <form className="space-y-6" onSubmit={handleSubmit}>
            {error && (
              <div className="p-4 bg-rose-50 dark:bg-rose-950/30 border border-rose-100 dark:border-rose-900/50 rounded-xl text-sm font-bold text-rose-500 animate-shake">
                {error}
              </div>
            )}
            
            <Input 
              label={t('common.email')}
              type="email"
              placeholder={t('auth.placeholder.email')}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
            />

            <div className="space-y-1">
              <div className="flex items-center justify-between">
                <label htmlFor="login-password" className="text-sm font-bold text-text-main ml-1">{t('common.password')}</label>
                <Link to="/forgot-password" size="xs" className="text-xs font-bold text-primary hover:underline">
                  {t('common.forgot')}
                </Link>
              </div>
              <input id="login-password"
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
                className="w-full px-4 py-2.5 rounded-xl border border-border-subtle bg-surface-0 text-text-main placeholder:text-text-muted focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all outline-none"
              />
            </div>

            <Button 
              type="submit" 
              className="w-full h-12 text-base" 
              isLoading={isLoading}
              rightIcon={!isLoading && <ArrowRight size={18} />}
            >
              {t('auth.login.submit')}
            </Button>
          </form>

          <p className="text-center mt-8 text-sm font-medium text-text-muted">
            {t('auth.login.noAccount')}{' '}
            <Link to="/register" className="text-primary font-bold hover:underline">
              {t('auth.login.createAccount')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
