import TeamActionModal from '@/components/teams/TeamActionModal';
import { useCallback, useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '@/context/authState';
import teamApi from '@/api/teamApi';
import { Users, UserPlus, Shield, UserX, ChevronLeft, MoreHorizontal, Mail, ShieldCheck, UserCheck } from 'lucide-react';
import Button from '@/components/ui/Button';
import PageControls from '@/components/ui/PageControls';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import toast from 'react-hot-toast';

export default function TeamDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [team, setTeam] = useState(null);
  const [loading, setLoading] = useState(true);
  const [memberPage, setMemberPage] = useState(1);
  const [members, setMembers] = useState(null);
  const [error, setError] = useState('');
  const [action, setAction] = useState(null);

  const fetchTeam = useCallback(() => Promise.all([teamApi.getTeam(id), teamApi.getMembers(id, memberPage)])
    .then(([res, roster]) => {
      setTeam(res.data.data);
      setMembers(roster.data.data);
      setError('');
    })
    .catch((err) => {
      console.error('Failed to fetch team details', err);
      setError(err.response?.data?.message || 'Failed to load team');

    })
    .finally(() => {
      setLoading(false);
    }), [id, memberPage]);

  useEffect(() => {
    fetchTeam();
  }, [fetchTeam]);



  const handleAddMember = () => setAction({ title: 'Add team member', fields: [{ name: 'email', label: 'Member email', type: 'email', required: true }], submit: values => teamApi.addMember(id, { ...values, role: 'Member' }) });
  const handleRemoveMember = memberId => setAction({ title: 'Remove member from team?', fields: [], destructive: true, submit: () => teamApi.removeMember(id, memberId) });
  const handleChangeRole = (memberId, currentRole) => setAction({ title: 'Change member role', fields: [{ name: 'role', label: 'Role', value: currentRole, options: ['Manager', 'Member'] }], submit: values => teamApi.changeMemberRole(id, memberId, values) });
  const submitAction = async values => {
    const response = await action.submit(values);
    if (!response.data.success) throw new Error(response.data.message);
    await fetchTeam();
    toast.success('Team updated.');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (error) return <p role="alert">{error}</p>;
  if (!team) return <div className="text-center py-20 font-medium text-text-muted">Team not found</div>;

  return (
    <div className="space-y-8">
      {action && <TeamActionModal key={action.title} {...action} onSubmit={submitAction} onClose={() => setAction(null)} />}
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => navigate('/teams')}>
            <ChevronLeft size={20} />
          </Button>
          <div>
            <h1 className="text-3xl font-black text-text-main tracking-tight">
              {team.name}
            </h1>
            <p className="text-text-muted mt-1 font-medium">
              Team management and member roles.
            </p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <Button variant="outline" size="md" leftIcon={<MoreHorizontal size={18} />}>Team Settings</Button>
          <Button leftIcon={<UserPlus size={18} />} onClick={handleAddMember}>Invite Member</Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left: Team Info Card */}
        <div className="lg:col-span-1 space-y-6">
          <Card>
            <h3 className="text-lg font-bold text-text-main mb-4">About Team</h3>
            <p className="text-sm text-text-muted font-medium leading-relaxed mb-6">
              {team.description || 'No description provided for this team.'}
            </p>
            <div className="space-y-4">
              <div className="flex items-center justify-between py-3 border-t border-border-subtle">
                <span className="text-xs font-bold text-text-subtle uppercase tracking-widest">Total Members</span>
                <span className="text-sm font-black text-text-main">{team.memberCount}</span>
              </div>
              <div className="flex items-center justify-between py-3 border-t border-border-subtle">
                <span className="text-xs font-bold text-text-subtle uppercase tracking-widest">Created At</span>
                <span className="text-sm font-black text-text-main">May 12, 2026</span>
              </div>
            </div>
          </Card>

          <Card className="bg-primary/5 border-primary/20">
            <div className="flex items-center gap-3 mb-4">
              <div className="p-2 bg-primary/10 rounded-xl text-primary">
                <ShieldCheck size={20} />
              </div>
              <h3 className="text-base font-bold text-primary">Permissions</h3>
            </div>
            <p className="text-xs text-primary/70 font-medium leading-relaxed">
              Only owners and managers can add or remove members from this team. Members can view and collaborate on assigned boards.
            </p>
          </Card>
        </div>

        {/* Right: Members List */}
        <div className="lg:col-span-2">
          <Card padding="p-0 overflow-hidden">
            <div className="px-6 py-5 border-b border-border-subtle bg-surface-1/50 flex items-center justify-between">
              <h3 className="text-lg font-bold text-text-main flex items-center gap-2">
                <Users size={20} /> Team Members
              </h3>
              <Badge variant="neutral">{members?.totalItems} total</Badge>
            </div>
            
            <div className="divide-y divide-border-subtle">
              <PageControls page={members} loading={loading} onPage={setMemberPage} />
              {members?.items.map((member) => {
                const isMe = member.userId === user?.id;
                
                return (
                  <div key={member.id} className="p-6 flex items-center justify-between group hover:bg-hover-bg transition-colors">
                    <div className="flex items-center gap-4">
                      <div className="w-12 h-12 rounded-2xl bg-primary/10 flex items-center justify-center text-primary font-black text-lg border border-primary/5">
                        {member.fullName.charAt(0).toUpperCase()}
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <h4 className="font-bold text-text-main">{member.fullName}</h4>
                          {isMe && <Badge variant="primary" className="text-[10px] py-0">You</Badge>}
                        </div>
                        <p className="text-xs text-text-muted font-medium flex items-center gap-1 mt-0.5">
                          <Mail size={12} /> {member.email}
                        </p>
                      </div>
                    </div>

                    <div className="flex items-center gap-4">
                      <Badge variant={member.role === 'Owner' ? 'primary' : 'neutral'} className="h-7 px-3">
                        <span className="flex items-center gap-1">
                          <Shield size={12} /> {member.role}
                        </span>
                      </Badge>
                      
                      <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <Button variant="ghost" size="icon" onClick={() => handleChangeRole(member.userId, member.role)} title="Edit Role">
                          <UserCheck size={18} className="text-text-subtle hover:text-primary transition-colors" />
                        </Button>
                        {!isMe && (
                          <Button variant="ghost" size="icon" onClick={() => handleRemoveMember(member.userId)} title="Remove Member">
                            <UserX size={18} className="text-text-subtle hover:text-rose-500 transition-colors" />
                          </Button>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
