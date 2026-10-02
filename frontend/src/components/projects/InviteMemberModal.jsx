import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Select from '@/components/ui/Select';
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
    if (isSubmitting || !email.trim()) return;

    try {
      setIsSubmitting(true);
      await projectMemberApi.inviteMember(projectId, { email: email.trim(), role });
      toast.success('Invitation created for ' + email.trim() + '. Email delivery is not confirmed.');
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
    <Modal isOpen={isOpen} title={t('projects.invite.modalTitle', { name: projectName })} onClose={onClose} closeDisabled={isSubmitting} maxWidth="max-w-md">
        <form onSubmit={handleSubmit} className="space-y-4 p-6">
          <div>
            <Input label={t('projects.invite.email')} disabled={isSubmitting}
              type="email"
              required
              placeholder={t('projects.invite.emailPlaceholder')}
              className="w-full rounded-xl border border-border-subtle bg-surface-2 px-3 py-2 text-sm text-text-main outline-none transition-all focus:ring-2 focus:ring-primary/20 placeholder:text-text-subtle"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div>
            <Select label={t('projects.invite.role')} disabled={isSubmitting}
              className="w-full rounded-xl border border-border-subtle bg-surface-2 px-3 py-2 text-sm text-text-main outline-none transition-all focus:ring-2 focus:ring-primary/20"
              value={role}
              onChange={(e) => setRole(e.target.value)}
            >
              <option value="Admin">{t('projects.role.Admin')}</option>
              <option value="Member">{t('projects.role.Member')}</option>
              <option value="Guest">{t('projects.role.Guest')}</option>
            </Select>
          </div>

          <div className="mt-6 flex justify-end gap-3 border-t border-border-subtle pt-4">
            <Button type="button" variant="ghost" disabled={isSubmitting} onClick={onClose}>
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
    </Modal>
  );
};

export default InviteMemberModal;
