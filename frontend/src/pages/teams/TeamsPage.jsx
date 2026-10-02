import { useCallback, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import teamApi from '@/api/teamApi';
import { Users, Plus, Layout, ArrowRight, MoreHorizontal, UserPlus } from 'lucide-react';
import Button from '@/components/ui/Button';
import PageControls from '@/components/ui/PageControls';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import toast from 'react-hot-toast';

export default function TeamsPage() {

  const navigate = useNavigate();
  const [teams, setTeams] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [pageInfo, setPageInfo] = useState(null);
  const [error, setError] = useState('');

  const fetchTeams = useCallback(() => teamApi.getPage({ page })
    .then((res) => {
      setTeams(res.data.data.items);
      setPageInfo(res.data.data);
      setError('');
    })
    .catch((err) => {
      console.error('Failed to fetch teams', err);
      toast.error('Failed to load teams');
      setError(err.response?.data?.message || 'Failed to load teams');
    })
    .finally(() => {
      setLoading(false);
    }), [page]);

  useEffect(() => {
    fetchTeams();
  }, [fetchTeams]);



  const createTeam = async () => {
    const name = prompt('Enter team name:');
    if (!name) return;
    const desc = prompt('Enter team description (optional):');
    
    try {
      await teamApi.createTeam({ name, description: desc });
      await fetchTeams();
      toast.success('Team created successfully!');
    } catch (err) {
      console.error('Failed to create team', err);
      toast.error('Failed to create team');
    }
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-text-main tracking-tight">
            Teams
          </h1>
          <p className="text-text-muted mt-1 font-medium">
            Manage your collaborations and team memberships.
          </p>
        </div>
        <Button 
          leftIcon={<UserPlus size={18} />} 
          onClick={createTeam}
          className="md:w-auto w-full"
        >
          Create New Team
        </Button>
      </div>

      <PageControls page={pageInfo} loading={loading} onPage={setPage} />
      {error ? <p role="alert">{error}</p> : loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3].map(i => (
            <div key={i} className="h-56 rounded-2xl bg-surface-2 animate-pulse"></div>
          ))}
        </div>
      ) : teams.length === 0 ? (
        <Card className="text-center py-20">
          <div className="w-20 h-20 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-6 text-primary">
            <Users size={40} />
          </div>
          <h3 className="text-xl font-bold text-text-main">No teams found</h3>
          <p className="text-text-muted mb-8 max-w-md mx-auto">
            You are not part of any teams yet. Create a team to start collaborating with others.
          </p>
          <Button variant="outline" onClick={createTeam}>Create First Team</Button>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {teams.map(team => (
            <Card 
              key={team.id} 
              className="group cursor-pointer hover:border-primary/50 flex flex-col justify-between"
              onClick={() => navigate(`/teams/${team.id}`)}
              padding="p-0"
            >
              <div className="p-6">
                <div className="flex justify-between items-start mb-4">
                  <div className="w-12 h-12 rounded-xl bg-surface-1 flex items-center justify-center text-text-subtle group-hover:bg-primary/10 group-hover:text-primary transition-colors">
                    <Users size={24} />
                  </div>
                  <Badge variant={team.currentUserRole === 'Owner' ? 'primary' : 'neutral'}>
                    {team.currentUserRole}
                  </Badge>
                </div>
                <h3 className="text-lg font-bold text-text-main group-hover:text-primary transition-colors mb-2">
                  {team.name}
                </h3>
                <p className="text-sm text-text-muted line-clamp-2 font-medium mb-4">
                  {team.description || 'No description provided.'}
                </p>
              </div>

              <div className="px-6 py-4 bg-surface-1 border-t border-border-subtle flex items-center justify-between rounded-b-2xl">
                <div className="flex items-center -space-x-2">
                  {[...Array(Math.min(team.memberCount, 3))].map((_, i) => (
                    <div key={i} className="w-7 h-7 rounded-full bg-surface-2 border-2 border-surface-0 flex items-center justify-center text-[10px] font-bold ring-1 ring-border-subtle">
                      {String.fromCharCode(65 + i)}
                    </div>
                  ))}
                  {team.memberCount > 3 && (
                    <div className="w-7 h-7 rounded-full bg-surface-0 border-2 border-border-subtle flex items-center justify-center text-[10px] font-bold text-text-subtle">
                      +{team.memberCount - 3}
                    </div>
                  )}
                </div>
                <span className="text-xs font-bold text-text-subtle uppercase tracking-widest group-hover:text-primary transition-colors flex items-center gap-1">
                  Details <ArrowRight size={14} />
                </span>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
