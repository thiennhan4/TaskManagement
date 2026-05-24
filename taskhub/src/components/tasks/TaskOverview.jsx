import React, { useState } from 'react';
import { Calendar, User, Flag, CheckCircle2, Clock, Trash2, UserPlus, Loader2 } from 'lucide-react';
import { StatusBadge } from './StatusBadge';
import { PriorityBadge } from './PriorityBadge';
import { StatusWorkflow } from './StatusWorkflow';
import { inviteToTask } from '../../services/taskService';
import toast from 'react-hot-toast';

export const TaskOverview = ({ task }) => {
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviting, setInviting] = useState(false);

  if (!task) return null;

  const handleInvite = async (e) => {
    e.preventDefault();
    if (!inviteEmail) return;
    setInviting(true);
    try {
      await inviteToTask(task.id, inviteEmail);
      toast.success(`Invitation sent to ${inviteEmail}`);
      setInviteEmail('');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to send invitation');
    } finally {
      setInviting(false);
    }
  };

  return (
    <div className="space-y-8 animate-in fade-in slide-in-from-bottom-2 duration-300">
      {/* Description Section */}
      <section>
        <h4 className="text-sm font-bold text-gray-400 uppercase tracking-wider mb-3">Description</h4>
        {task.description ? (
          <div className="bg-gray-50 rounded-xl p-4 text-gray-700 leading-relaxed whitespace-pre-wrap border border-gray-100">
            {task.description}
          </div>
        ) : (
          <p className="text-gray-400 italic">No description provided.</p>
        )}
      </section>

      {/* Workflow Section */}
      <section>
        <h4 className="text-sm font-bold text-gray-400 uppercase tracking-wider mb-4">Status Workflow</h4>
        <StatusWorkflow currentStatus={task.status} taskId={task.id} />
      </section>

      {/* Metadata Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 pt-4">
        <div className="flex items-start gap-3">
          <div className="p-2 bg-blue-50 rounded-lg">
            <User className="w-5 h-5 text-blue-600" />
          </div>
          <div>
            <p className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-1">Assigned To</p>
            <div className="flex items-center gap-2">
              <p className="text-sm font-bold text-gray-900">{task.assignedToName || 'Unassigned'}</p>
            </div>
            {/* Invite Form - only show for team tasks */}
            {(task.workspaceId || task.teamId) ? (
              <form onSubmit={handleInvite} className="mt-2 flex items-center gap-2">
                <input
                  type="email"
                  placeholder="Invite via email..."
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  className="text-xs px-2 py-1.5 border border-gray-200 rounded-lg focus:outline-none focus:border-blue-500 w-36"
                  required
                />
                <button
                  type="submit"
                  disabled={inviting || !inviteEmail}
                  className="p-1.5 bg-blue-50 text-blue-600 rounded-lg hover:bg-blue-100 disabled:opacity-50 transition-colors"
                  title="Invite Member"
                >
                  {inviting ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserPlus className="w-4 h-4" />}
                </button>
              </form>
            ) : (
              <p className="text-xs text-gray-400 mt-1 italic">Personal task — invite disabled</p>
            )}
          </div>
        </div>

        <div className="flex items-start gap-3">
          <div className="p-2 bg-purple-50 rounded-lg">
            <Calendar className="w-5 h-5 text-purple-600" />
          </div>
          <div>
            <p className="text-xs font-bold text-gray-400 uppercase tracking-wider">Due Date</p>
            <p className={`text-sm font-bold mt-0.5 ${task.isOverdue ? 'text-red-600' : 'text-gray-900'}`}>
              {task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'No deadline'}
            </p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <div className="p-2 bg-yellow-50 rounded-lg">
            <Flag className="w-5 h-5 text-yellow-600" />
          </div>
          <div>
            <p className="text-xs font-bold text-gray-400 uppercase tracking-wider">Priority</p>
            <div className="mt-0.5">
              <PriorityBadge priority={task.priority} />
            </div>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <div className="p-2 bg-green-50 rounded-lg">
            <CheckCircle2 className="w-5 h-5 text-green-600" />
          </div>
          <div>
            <p className="text-xs font-bold text-gray-400 uppercase tracking-wider">Status</p>
            <div className="mt-0.5">
              <StatusBadge status={task.status} />
            </div>
          </div>
        </div>
      </div>

      {/* Creation Info */}
      <div className="pt-8 border-t border-gray-100 flex items-center justify-between text-xs text-gray-400 font-medium">
        <div className="flex items-center gap-2">
          <Clock className="w-3.5 h-3.5" />
          <span>Created by {task.createdByUserName} • {new Date(task.createdAt).toLocaleString()}</span>
        </div>
        {task.updatedAt && (
          <span>Last updated • {new Date(task.updatedAt).toLocaleString()}</span>
        )}
      </div>
    </div>
  );
};
