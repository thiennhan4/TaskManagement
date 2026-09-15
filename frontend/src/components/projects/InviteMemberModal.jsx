import React, { useState } from 'react';
import { X, Mail, Loader2, Send } from 'lucide-react';
import toast from 'react-hot-toast';
import Button from '@/components/ui/Button';
import { projectMemberApi } from '@/api/projectMemberApi';
import { useLanguage } from '@/context/LanguageContext';

const InviteMemberModal = ({ isOpen, onClose, projectId, projectName }) => {
  const { t } = useLanguage();
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('Member');
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!email.trim()) return;

    try {
      setIsSubmitting(true);
      await projectMemberApi.inviteMember(projectId, { email: email.trim(), role });
      toast.success(t('projects.toast.inviteSentTo', { email: email.trim() }));
      setEmail('');
      setRole('Member');
      onClose();
    } catch (error) {
      toast.error(error.response?.data?.message || t('projects.toast.inviteError'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4 backdrop-blur-sm animate-in fade-in">
      <div className="flex w-full max-w-md flex-col overflow-hidden rounded-2xl border border-border-subtle bg-surface-0 shadow-premium animate-in zoom-in-95 duration-200">
        <div className="flex items-center justify-between border-b border-border-subtle bg-surface-2 p-4">
          <h2 className="flex items-center gap-2 text-lg font-bold text-text-main">
            <Mail className="text-primary" size={20} />
            {t('projects.invite.modalTitle', { name: projectName })}
          </h2>
          <button
            onClick={onClose}
            className="rounded-lg p-1 text-text-muted transition-colors hover:bg-hover-bg"
          >
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 p-6">
          <div>
            <label className="mb-1.5 block text-sm font-semibold text-text-main">{t('projects.invite.email')}</label>
            <input
              type="email"
              required
              placeholder={t('projects.invite.emailPlaceholder')}
              className="w-full rounded-xl border border-border-subtle bg-surface-2 px-3 py-2 text-sm text-text-main outline-none transition-all focus:ring-2 focus:ring-primary/20 placeholder:text-text-subtle"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-semibold text-text-main">{t('projects.invite.role')}</label>
            <select
              className="w-full rounded-xl border border-border-subtle bg-surface-2 px-3 py-2 text-sm text-text-main outline-none transition-all focus:ring-2 focus:ring-primary/20"
              value={role}
              onChange={(e) => setRole(e.target.value)}
            >
              <option value="Admin">{t('projects.role.Admin')}</option>
              <option value="Member">{t('projects.role.Member')}</option>
              <option value="Guest">{t('projects.role.Guest')}</option>
            </select>
          </div>

          <div className="mt-6 flex justify-end gap-3 border-t border-border-subtle pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              {t('common.cancel')}
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting || !email.trim()}
              leftIcon={isSubmitting ? <Loader2 size={16} className="animate-spin" /> : <Send size={16} />}
            >
              {isSubmitting ? t('projects.invite.sending') : t('projects.invite.send')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default InviteMemberModal;
