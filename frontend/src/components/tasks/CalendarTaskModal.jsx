import React, { useState, useEffect } from 'react';
import { 
  X, 
  Calendar, 
  Flag, 
  User, 
  Briefcase, 
  Loader2, 
  AlignLeft,
  ChevronDown
} from 'lucide-react';
import { useLanguage } from '@/context/LanguageContext';
import Modal from '@/components/ui/Modal';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Textarea from '@/components/ui/Textarea';
import Select from '@/components/ui/Select';
import { toast } from 'react-hot-toast';
import api from '@/api/axiosInstance';
import * as taskService from '@/services/taskService';
import teamApi from '@/api/teamApi';

const formatDateTimeLocal = (date) => {
  if (!date) return '';
  const d = new Date(date);
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 16);
};

const CalendarTaskModal = ({ isOpen, onClose, initialDate, onSuccess }) => {
  const { t } = useLanguage();
  const [loading, setLoading] = useState(false);
  const [boards, setBoards] = useState([]);
  const [lists, setLists] = useState([]);
  const [members, setMembers] = useState([]);
  
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    priority: 'Medium',
    status: 'Todo',
    dueDate: initialDate ? formatDateTimeLocal(initialDate) : '',
    startDate: initialDate ? formatDateTimeLocal(initialDate) : '',
    boardId: '',
    listId: '',
    assignedToId: ''
  });

  useEffect(() => {
    if (isOpen) {
      fetchBoards();
      if (initialDate) {
        setFormData(prev => ({ 
          ...prev, 
          dueDate: formatDateTimeLocal(initialDate),
          startDate: formatDateTimeLocal(initialDate)
        }));
      }
    }
  }, [isOpen, initialDate]);

  const fetchBoards = async () => {
    try {
      const response = await api.get('/boards');
      if (response.data.success) {
        setBoards(response.data.data);
      }
    } catch (err) {
      console.error('Failed to fetch boards');
    }
  };

  const handleBoardChange = async (boardId) => {
    setFormData({ ...formData, boardId, listId: '', assignedToId: '' });
    try {
      // Fetch lists for the board
      const listRes = await api.get(`/boardlists/board/${boardId}`);
      if (listRes.data.success) {
        setLists(listRes.data.data);
      }
      
      // Fetch members for the board (via team)
      const board = boards.find(b => b.id === boardId);
      if (board && board.teamId) {
        const teamRes = await teamApi.getTeam(board.teamId);
        setMembers(teamRes.data.data.members || []);
      }
    } catch (err) {
      console.error('Failed to fetch board details');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.title || !formData.listId) {
      toast.error(t('dashboard.boardNameRequired'));
      return;
    }

    setLoading(true);
    try {
      const result = await taskService.createTask(formData.listId, {
        title: formData.title,
        description: formData.description,
        priority: formData.priority,
        dueDate: formData.dueDate || null,
        startDate: formData.startDate || null,
        assignedToId: formData.assignedToId || null
      });

      if (result.success) {
        toast.success(t('board.taskCreated'));
        onSuccess();
        onClose();
        setFormData({
          title: '',
          description: '',
          priority: 'Medium',
          status: 'Todo',
          dueDate: '',
          startDate: '',
          boardId: '',
          listId: '',
          assignedToId: ''
        });
      }
    } catch (err) {
      toast.error(err.response?.data?.message || t('board.taskUpdateError'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={t('calendar.addTask')} maxWidth="max-w-2xl">
      <form onSubmit={handleSubmit} className="p-8 space-y-6">
        <Input 
          label={t('common.fullName').replace('Họ và tên', 'Tiêu đề').replace('Full Name', 'Task Title')}
          placeholder={t('nav.searchPlaceholder')}
          value={formData.title}
          onChange={(e) => setFormData({...formData, title: e.target.value})}
          autoFocus
        />

        <Textarea 
          label={t('taskModal.description')}
          placeholder={t('taskModal.description')}
          value={formData.description}
          onChange={(e) => setFormData({...formData, description: e.target.value})}
        />

        <div className="grid grid-cols-2 gap-6">
          <div className="space-y-1.5 text-sm font-bold text-text-main">
            <label className="ml-1 flex items-center gap-2">
              <Calendar size={14} className="text-primary" /> Start Date
            </label>
            <input 
              type="datetime-local"
              className="w-full px-4 py-2.5 rounded-xl border border-border-subtle bg-surface-2 text-text-main focus:border-primary focus:ring-4 focus:ring-primary/10 transition-all outline-none"
              value={formData.startDate}
              onChange={(e) => setFormData({...formData, startDate: e.target.value})}
            />
          </div>
          <div className="space-y-1.5 text-sm font-bold text-text-main">
            <label className="ml-1 flex items-center gap-2">
              <Calendar size={14} className="text-red-500" /> {t('taskModal.dueDate')}
            </label>
            <input 
              type="datetime-local"
              className="w-full px-4 py-2.5 rounded-xl border border-border-subtle bg-surface-2 text-text-main focus:border-primary focus:ring-4 focus:ring-primary/10 transition-all outline-none"
              value={formData.dueDate}
              onChange={(e) => setFormData({...formData, dueDate: e.target.value})}
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6">
          <Select 
            label={t('board.filter').replace('Lọc', 'Mức độ ưu tiên').replace('Filter', 'Priority')}
            value={formData.priority}
            onChange={(e) => setFormData({...formData, priority: e.target.value})}
          >
            <option value="Low">{t('task.priority.Low')}</option>
            <option value="Medium">{t('task.priority.Medium')}</option>
            <option value="High">{t('task.priority.High')}</option>
            <option value="Critical">Critical</option>
          </Select>

          <Select 
            label={t('nav.projects')}
            value={formData.boardId}
            onChange={(e) => handleBoardChange(e.target.value)}
          >
            <option value="">{t('nav.searchPlaceholder').split(',')[1]?.trim() || 'Select Board'}</option>
            {boards.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
          </Select>
        </div>

        <div className="grid grid-cols-2 gap-6">
          <Select 
            label={t('board.columnAdded').replace('Đã thêm cột!', 'Cột').replace('Column added!', 'List / Column')}
            disabled={!formData.boardId}
            value={formData.listId}
            onChange={(e) => setFormData({...formData, listId: e.target.value})}
          >
            <option value="">Select List</option>
            {lists.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}
          </Select>

          <Select 
            label={t('taskModal.assignedTo')}
            disabled={!formData.boardId}
            value={formData.assignedToId}
            onChange={(e) => setFormData({...formData, assignedToId: e.target.value})}
          >
            <option value="">{t('taskModal.unassigned')}</option>
            {members.map(m => (
              <option key={m.userId} value={m.userId}>{m.userFullName}</option>
            ))}
          </Select>
        </div>

        <div className="flex justify-end gap-3 pt-4 border-t border-border-subtle transition-colors">
          <Button variant="outline" type="button" onClick={onClose}>{t('common.cancel')}</Button>
          <Button type="submit" disabled={loading} loading={loading}>
            {t('calendar.addTask')}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

export default CalendarTaskModal;
