import React, { useEffect, useState } from 'react';
import { Plus, Trash2, User, Users, Mail, UserRoundPlus } from 'lucide-react';
import { toast } from 'react-hot-toast';
import { useNavigate } from 'react-router-dom';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Button from '@/components/ui/Button';
import Textarea from '@/components/ui/Textarea';
import Select from '@/components/ui/Select';
import { useLanguage } from '@/context/LanguageContext';
import { useProjectStore } from '../../stores/useProjectStore';
import teamApi from '../../api/teamApi';

const DEFAULT_COLOR = '#6366f1';
const DEFAULT_INVITE = { email: '', role: 'Member' };
const COLOR_OPTIONS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#8b5cf6', '#0ea5e9'];

const isValidEmail = (value) => /\S+@\S+\.\S+/.test(value);

const CreateProjectModal = ({ isOpen, onClose }) => {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const { createProject, inviteMember } = useProjectStore();
  const [teams, setTeams] = useState([]);
  const [isLoadingTeams, setIsLoadingTeams] = useState(false);
  const [projectType, setProjectType] = useState('personal');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState({});
  const [formData, setFormData] = useState({
    name: '',
    slug: '',
    description: '',
    emoji: '📁',
    color: DEFAULT_COLOR,
    visibility: 'Private',
    workspaceId: '',
  });
  const [inviteList, setInviteList] = useState([DEFAULT_INVITE]);
  const hasTeams = teams.length > 0;

  useEffect(() => {
    if (!isOpen) return;

    setProjectType('personal');
    setErrors({});
    setInviteList([DEFAULT_INVITE]);
    setFormData({
      name: '',
      slug: '',
      description: '',
      emoji: '📁',
      color: DEFAULT_COLOR,
      visibility: 'Private',
      workspaceId: '',
    });
    fetchTeams();
  }, [isOpen]);

  const fetchTeams = async () => {
    setIsLoadingTeams(true);
    try {
      const res = await teamApi.getTeams();
      const teamsData = res.data?.data || [];
      setTeams(teamsData);
      if (teamsData.length > 0) {
        setFormData((prev) => ({ ...prev, workspaceId: teamsData[0].id }));
      }
    } catch {
      toast.error(t('projects.toast.loadTeamsError'));
    } finally {
      setIsLoadingTeams(false);
    }
  };

  const handleNameChange = (e) => {
    const name = e.target.value;
    setErrors((prev) => ({ ...prev, name: undefined, slug: undefined }));
    setFormData((prev) => ({ ...prev, name }));
  };

  const updateInvite = (index, field, value) => {
    setInviteList((prev) => prev.map((item, idx) => (idx === index ? { ...item, [field]: value } : item)));
  };

  const addInviteRow = () => {
    setInviteList((prev) => [...prev, DEFAULT_INVITE]);
  };

  const removeInviteRow = (index) => {
    setInviteList((prev) => prev.filter((_, idx) => idx !== index));
  };

  const handleClose = () => {
    if (isSubmitting) return;
    onClose();
  };

  const validateForm = () => {
    const nextErrors = {};

    if (!formData.name.trim()) {
      nextErrors.name = t('projects.validation.nameRequired');
    }

    if (!formData.slug.trim()) {
      nextErrors.slug = t('projects.validation.slugRequired');
    }

    if (projectType === 'team' && !formData.workspaceId) {
      nextErrors.workspaceId = t('projects.validation.workspaceRequired');
    }

    if (projectType === 'team') {
      inviteList.forEach((invite, index) => {
        if (invite.email.trim() && !isValidEmail(invite.email.trim())) {
          nextErrors[`invite-${index}`] = t('projects.validation.invalidEmail');
        }
      });
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (isSubmitting) return;
    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      const payload = {
        ...formData,
        workspaceId: projectType === 'team' ? formData.workspaceId : null,
        visibility: projectType === 'personal' && formData.visibility === 'TeamOnly' ? 'Private' : formData.visibility,
      };

      const createdProject = await createProject(payload, {
        silentErrorToast: true,
      });
      const validInvites = projectType === 'team'
        ? inviteList.filter((invite) => invite.email.trim())
        : [];

      if (projectType === 'team' && validInvites.length > 0) {
        const results = await Promise.allSettled(
          validInvites.map((invite) =>
            inviteMember(createdProject.id, {
              email: invite.email.trim(),
              role: invite.role,
            }),
          ),
        );

        const failedCount = results.filter((result) => result.status === 'rejected').length;
        if (failedCount === 0) {
          toast.success(t('projects.toast.invitesSentSuccess'));
        } else {
          toast.error(t('projects.toast.invitesPartialError', { count: failedCount }));
        }
      }

      onClose();
    } catch (error) {
      const message = error.response?.data?.message || t('projects.toast.createError');
      if (message.toLowerCase().includes('slug')) {
        setErrors((prev) => ({ ...prev, slug: message }));
      } else if (message.toLowerCase().includes('workspace')) {
        setErrors((prev) => ({ ...prev, workspaceId: message }));
      } else {
        toast.error(message);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={t('projects.createModal.title')} maxWidth="max-w-3xl">
      <form onSubmit={handleSubmit} className="p-8 space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <button
            type="button"
            className={`flex items-center gap-3 rounded-2xl border-2 px-5 py-4 text-left transition-all ${
              projectType === 'personal'
                ? 'border-primary bg-primary/10 text-primary'
                : 'border-slate-200 text-slate-600 hover:border-slate-300'
            }`}
            onClick={() => setProjectType('personal')}
          >
            <div className={`rounded-full p-2 ${projectType === 'personal' ? 'bg-primary/15' : 'bg-slate-100'}`}>
              <User className="w-5 h-5" />
            </div>
            <div>
              <div className="font-bold">{t('projects.type.personal')}</div>
              <div className="text-xs opacity-80">{t('projects.type.personalHint')}</div>
            </div>
          </button>

          <button
            type="button"
            className={`flex items-center gap-3 rounded-2xl border-2 px-5 py-4 text-left transition-all ${
              projectType === 'team'
                ? 'border-primary bg-primary/10 text-primary'
                : 'border-slate-200 text-slate-600 hover:border-slate-300'
            }`}
            onClick={() => setProjectType('team')}
          >
            <div className={`rounded-full p-2 ${projectType === 'team' ? 'bg-primary/15' : 'bg-slate-100'}`}>
              <Users className="w-5 h-5" />
            </div>
            <div>
              <div className="font-bold">{t('projects.type.team')}</div>
              <div className="text-xs opacity-80">{t('projects.type.teamHint')}</div>
            </div>
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <Input
            label={t('projects.fields.emoji')}
            value={formData.emoji}
            onChange={(e) => setFormData((prev) => ({ ...prev, emoji: e.target.value }))}
            className="md:col-span-1"
          />
          <Input
            label={t('projects.fields.name')}
            placeholder={t('projects.fields.namePlaceholder')}
            value={formData.name}
            onChange={handleNameChange}
            error={errors.name}
            className="md:col-span-3"
          />
        </div>

        <Input
          label={t('projects.fields.slug')}
          placeholder={t('projects.fields.slugPlaceholder')}
          value={formData.slug}
          onChange={(e) => {
            setErrors((prev) => ({ ...prev, slug: undefined }));
            setFormData((prev) => ({ ...prev, slug: e.target.value }));
          }}
          error={errors.slug}
        />
        <p className="text-xs text-text-muted -mt-3">
          {t('projects.fields.slugHelp')}
        </p>

        <Textarea
          label={t('projects.fields.description')}
          placeholder={t('projects.fields.descriptionPlaceholder')}
          value={formData.description}
          onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))}
        />

        <div className={`grid gap-4 ${projectType === 'team' ? 'md:grid-cols-2' : 'grid-cols-1'}`}>
          {projectType === 'team' && (
            <div className="space-y-2">
              <Select
                label={t('projects.fields.workspace')}
                value={formData.workspaceId}
                onChange={(e) => {
                  setErrors((prev) => ({ ...prev, workspaceId: undefined }));
                  setFormData((prev) => ({ ...prev, workspaceId: e.target.value }));
                }}
                disabled={isLoadingTeams || !hasTeams}
                error={errors.workspaceId}
              >
                <option value="">{hasTeams ? t('projects.fields.workspacePlaceholder') : t('projects.workspace.emptyOption')}</option>
                {teams.map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.name}
                  </option>
                ))}
              </Select>
              <p className="text-xs text-text-muted">
                {t('projects.workspace.help')}
              </p>
              {!hasTeams && !isLoadingTeams && (
                <div className="rounded-xl border border-amber-300/40 bg-amber-50/70 p-3 text-sm text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-200">
                  <p>{t('projects.workspace.emptyState')}</p>
                  <Button
                    type="button"
                    variant="outline"
                    className="mt-3"
                    onClick={() => {
                      onClose();
                      navigate('/teams');
                    }}
                  >
                    {t('projects.workspace.createTeam')}
                  </Button>
                </div>
              )}
            </div>
          )}

          <Select
            label={t('projects.fields.visibility')}
            value={formData.visibility}
            onChange={(e) => setFormData((prev) => ({ ...prev, visibility: e.target.value }))}
          >
            <option value="Private">{t('projects.visibility.Private')}</option>
            {projectType === 'team' && <option value="TeamOnly">{t('projects.visibility.TeamOnly')}</option>}
            <option value="Public">{t('projects.visibility.Public')}</option>
          </Select>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-bold text-text-main ml-1">{t('projects.fields.color')}</label>
          <div className="flex flex-wrap gap-3">
            {COLOR_OPTIONS.map((color) => (
              <button
                key={color}
                type="button"
                className={`h-9 w-9 rounded-full border-2 transition-transform ${
                  formData.color === color ? 'scale-110 border-slate-900' : 'border-transparent hover:scale-105'
                }`}
                style={{ backgroundColor: color }}
                onClick={() => setFormData((prev) => ({ ...prev, color }))}
                aria-label={color}
              />
            ))}
            <input
              type="color"
              value={formData.color}
              onChange={(e) => setFormData((prev) => ({ ...prev, color: e.target.value }))}
              className="h-9 w-9 cursor-pointer rounded border-none bg-transparent p-0"
            />
          </div>
        </div>

        {projectType === 'team' && hasTeams && (
          <div className="rounded-2xl border border-border-subtle bg-slate-50/70 p-5 space-y-4 dark:bg-slate-800/60">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h3 className="flex items-center gap-2 text-sm font-bold text-text-main">
                  <UserRoundPlus size={18} className="text-primary" />
                  {t('projects.invite.title')}
                </h3>
                <p className="mt-1 text-sm text-text-muted">{t('projects.invite.subtitle')}</p>
              </div>
              <Button type="button" variant="outline" onClick={addInviteRow} leftIcon={<Plus size={16} />}>
                {t('projects.invite.addRow')}
              </Button>
            </div>

            <div className="space-y-3">
              {inviteList.map((invite, index) => (
                <div key={`${index}-${invite.email}`} className="grid grid-cols-1 md:grid-cols-[1fr_180px_auto] gap-3 items-start">
                  <Input
                    type="email"
                    label={index === 0 ? t('projects.invite.email') : undefined}
                    placeholder={t('projects.invite.emailPlaceholder')}
                    value={invite.email}
                    onChange={(e) => {
                      setErrors((prev) => ({ ...prev, [`invite-${index}`]: undefined }));
                      updateInvite(index, 'email', e.target.value);
                    }}
                    error={errors[`invite-${index}`]}
                  />
                  <Select
                    label={index === 0 ? t('projects.invite.role') : undefined}
                    value={invite.role}
                    onChange={(e) => updateInvite(index, 'role', e.target.value)}
                  >
                    <option value="Admin">{t('projects.role.Admin')}</option>
                    <option value="Member">{t('projects.role.Member')}</option>
                    <option value="Guest">{t('projects.role.Guest')}</option>
                  </Select>
                  <div className="md:pt-8">
                    {inviteList.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeInviteRow(index)}
                        className="rounded-xl p-3 text-rose-500 transition-colors hover:bg-rose-50 dark:hover:bg-rose-500/10"
                        aria-label={t('projects.invite.removeRow')}
                        title={t('projects.invite.removeRow')}
                      >
                        <Trash2 size={18} />
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="outline" type="button" onClick={handleClose}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" isLoading={isSubmitting} disabled={projectType === 'team' && !hasTeams}>
            {projectType === 'team' ? t('projects.createModal.submitTeam') : t('projects.createModal.submit')}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

export default CreateProjectModal;
