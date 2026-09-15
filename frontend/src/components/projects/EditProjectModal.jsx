import React, { useEffect, useState } from 'react';
import { toast } from 'react-hot-toast';
import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import Button from '@/components/ui/Button';
import Textarea from '@/components/ui/Textarea';
import Select from '@/components/ui/Select';
import { useLanguage } from '@/context/LanguageContext';
import { useProjectStore } from '../../stores/useProjectStore';

const COLOR_OPTIONS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#8b5cf6', '#0ea5e9'];

const EditProjectModal = ({ isOpen, onClose, project }) => {
  const { t } = useLanguage();
  const { updateProject } = useProjectStore();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState({});
  const [formData, setFormData] = useState({
    name: '',
    slug: '',
    description: '',
    emoji: '📁',
    color: '#6366f1',
    visibility: 'Private',
  });

  useEffect(() => {
    if (!isOpen || !project) return;
    
    setErrors({});
    setFormData({
      name: project.name || '',
      slug: project.slug || '',
      description: project.description || '',
      emoji: project.emoji || '📁',
      color: project.color || '#6366f1',
      visibility: project.visibility || 'Private',
    });
  }, [isOpen, project]);

  const handleNameChange = (e) => {
    const name = e.target.value;
    setErrors((prev) => ({ ...prev, name: undefined }));
    setFormData((prev) => ({ ...prev, name }));
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

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (isSubmitting) return;
    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      await updateProject(project.id, formData);
      onClose();
    } catch (error) {
      const message = error.response?.data?.message || t('projects.toast.updateError');
      if (message.toLowerCase().includes('slug')) {
        setErrors((prev) => ({ ...prev, slug: message }));
      } else {
        toast.error(message);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!project) return null;

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={t('projects.editProject')} maxWidth="max-w-2xl">
      <form onSubmit={handleSubmit} className="p-8 space-y-6">
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

        <div className="grid grid-cols-1 gap-4">
          <Select
            label={t('projects.fields.visibility')}
            value={formData.visibility}
            onChange={(e) => setFormData((prev) => ({ ...prev, visibility: e.target.value }))}
          >
            <option value="Private">{t('projects.visibility.Private')}</option>
            {project.workspaceId && <option value="TeamOnly">{t('projects.visibility.TeamOnly')}</option>}
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
                  formData.color === color ? 'scale-110 border-text-main' : 'border-transparent hover:scale-105'
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

        <div className="flex justify-end gap-3 pt-2">
          <Button variant="outline" type="button" onClick={handleClose}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {t('common.save')}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

export default EditProjectModal;
