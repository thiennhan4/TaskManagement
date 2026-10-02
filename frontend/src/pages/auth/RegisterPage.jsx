import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { useAuth } from '@/context/authState';
import { GoogleLogin } from "@react-oauth/google";
import { Rocket, Users, Zap, ShieldCheck } from 'lucide-react';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import BrandLogo from '@/components/common/BrandLogo';
import LanguageSwitcher from '@/components/common/LanguageSwitcher';
import { useLanguage } from '@/context/LanguageContext';

export default function RegisterPage() {
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const { register, googleLogin } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();

  const handleGoogleSuccess = async (tokenResponse) => {
    try {
      setIsLoading(true);
      await googleLogin(tokenResponse.credential);
      navigate("/dashboard");
    } catch (err) {
      setError(err.message || t('auth.register.errorGoogle'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    if (password.length < 6) {
      setError(t('auth.register.errorPassword'));
      return;
    }
    setIsLoading(true);
    try {
      await register(fullName, email, password);
      navigate("/dashboard");
    } catch (err) {
      setError(err.message || t('auth.register.errorCreate'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex bg-surface-1 font-sans">
      {/* Left Panel: Value Prop */}
      <div className="hidden lg:flex flex-col justify-between w-[40%] bg-bg-sidebar p-12 text-white relative overflow-hidden">
        <div className="absolute top-0 left-0 w-full h-full opacity-10 pointer-events-none">
          <div className="absolute top-12 left-12 w-64 h-64 border border-white rounded-full"></div>
          <div className="absolute top-24 left-24 w-64 h-64 border border-white rounded-full"></div>
          <div className="absolute top-36 left-36 w-64 h-64 border border-white rounded-full"></div>
        </div>

        <div className="relative z-10">
          <div className="mb-16 flex items-center justify-between">
            <BrandLogo tone="dark" />
            <LanguageSwitcher tone="dark" compact />
          </div>

          <h2 className="text-4xl font-black leading-tight tracking-tight mb-8">
            {t('auth.register.marketingTitle')} <br />
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-emerald-400 to-cyan-400">{t('auth.register.marketingHighlight')}</span> {t('auth.register.marketingSuffix')}
          </h2>
          
          <div className="space-y-8">
            <FeatureItem 
              icon={<Zap className="text-amber-400" />} 
              title={t('auth.register.featureSetup.title')}
              desc={t('auth.register.featureSetup.desc')}
            />
            <FeatureItem 
              icon={<Users className="text-blue-400" />} 
              title={t('auth.register.featureCollaborate.title')}
              desc={t('auth.register.featureCollaborate.desc')}
            />
            <FeatureItem 
              icon={<ShieldCheck className="text-emerald-400" />} 
              title={t('auth.register.featureSecure.title')}
              desc={t('auth.register.featureSecure.desc')}
            />
          </div>
        </div>

        <div className="relative z-10">
          <p className="text-xs text-slate-500 font-bold uppercase tracking-widest">&copy; 2026 {t('auth.platform')}</p>
        </div>
      </div>

      {/* Right Panel: Register Form */}
      <div className="flex-1 flex flex-col justify-center px-6 py-12 md:px-24 bg-surface-1">
        <div className="max-w-md w-full mx-auto">
          <div className="lg:hidden flex justify-center mb-8">
            <div className="flex flex-col items-center gap-4">
              <BrandLogo />
              <LanguageSwitcher compact />
            </div>
          </div>

          <h1 className="text-3xl font-black text-text-main tracking-tight mb-2">{t('auth.register.title')}</h1>
          <p className="text-text-muted font-medium mb-8">{t('auth.register.subtitle')}</p>

          {/* Social Auth */}
          <div className="mb-8">
            <GoogleLogin
              onSuccess={handleGoogleSuccess}
              onError={() => setError(t('auth.register.errorGoogle'))}
              width="100%"
              theme="outline"
              shape="pill"
              text="signup_with"
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

          {/* Register Form */}
          <form className="space-y-5" onSubmit={handleSubmit}>
            {error && (
              <div className="p-4 bg-rose-50 dark:bg-rose-950/30 border border-rose-100 dark:border-rose-900/50 rounded-xl text-sm font-bold text-rose-500">
                {error}
              </div>
            )}
            
            <Input 
              label={t('common.fullName')}
              placeholder={t('auth.placeholder.fullName')}
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              required
            />

            <Input 
              label={t('common.email')}
              type="email"
              placeholder={t('auth.placeholder.registerEmail')}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />

            <Input 
              label={t('common.password')}
              type="password"
              placeholder={t('auth.placeholder.passwordMin')}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />

            <div className="pt-2">
              <Button 
                type="submit" 
                className="w-full h-12 text-base" 
                isLoading={isLoading}
                rightIcon={!isLoading && <Rocket size={18} />}
              >
                {t('auth.register.submit')}
              </Button>
            </div>
          </form>

          <p className="text-center mt-8 text-sm font-medium text-text-muted">
            {t('auth.register.haveAccount')}{' '}
            <Link to="/login" className="text-primary font-bold hover:underline">
              {t('auth.register.signIn')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

function FeatureItem({ icon, title, desc }) {
  return (
    <div className="flex gap-4">
      <div className="mt-1">{icon}</div>
      <div>
        <p className="font-bold text-lg leading-tight mb-1">{title}</p>
        <p className="text-sm text-slate-400 font-medium leading-relaxed">{desc}</p>
      </div>
    </div>
  );
}
