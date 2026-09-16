import React, { useEffect, useState } from 'react';
import { useLanguage } from '@/context/LanguageContext';
import { analyticsApi } from '@/api/analyticsApi';
import teamApi from '@/api/teamApi';
import { Layout, CheckCircle2, AlertCircle, Clock, Loader2, BarChart2, PieChart, TrendingUp } from 'lucide-react';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';

export default function AnalyticsPage() {
  const { t } = useLanguage();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [days, setDays] = useState(30);
  const [teams, setTeams] = useState([]);
  const [teamId, setTeamId] = useState('');
  const [velocityTimeframe, setVelocityTimeframe] = useState('SixMonths');
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    teamApi.getTeams()
      .then((response) => { if (active) setTeams(response.data.data || []); })
      .catch(() => { if (active) setTeams([]); });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    let active = true;
    analyticsApi.getOverview(days, null, teamId ? 'Team' : 'Personal', teamId || null, velocityTimeframe)
      .then((response) => {
        if (active) {
          setData(response.data.data);
          setError('');
          setLoading(false);
        }
      })
      .catch(() => {
        if (active) {
          setData(null);
          setError('Analytics could not be loaded. Please try again.');
          setLoading(false);
        }
      });
    return () => { active = false; };
  }, [days, teamId, velocityTimeframe]);

  if (loading && !data) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="animate-spin text-primary" size={40} />
      </div>
    );
  }

  return (
    <div className="space-y-8 animate-in fade-in duration-500 pb-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <div className="w-14 h-14 bg-primary/10 rounded-2xl flex items-center justify-center text-primary shadow-inner">
            <BarChart2 size={28} />
          </div>
          <div>
            <h1 className="text-3xl font-black text-text-main tracking-tight">
              {t('nav.analytics') || 'Analytics & Reporting'}
            </h1>
            <p className="text-sm font-bold text-text-subtle uppercase tracking-widest">
              Performance Insights
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3">
        <label className="text-sm font-bold text-text-muted" htmlFor="analytics-scope">Scope</label>
        <select
          id="analytics-scope"
          value={teamId}
          onChange={(event) => { setLoading(true); setTeamId(event.target.value); }}
          className="rounded-xl border border-border-subtle bg-surface-0 px-3 py-2 text-sm font-bold text-text-main"
        >
          <option value="">Personal</option>
          {teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
        </select>
        <div className="flex bg-surface-0 p-1 rounded-xl shadow-sm border border-border-subtle">
          {[7, 14, 30, 90].map(d => (
            <button
              key={d}
              onClick={() => { setLoading(true); setDays(d); }}
              className={`px-4 py-2 text-sm font-bold rounded-lg transition-all ${
                days === d
                  ? 'bg-primary text-white shadow-md'
                  : 'text-text-muted hover:text-text-main'
              }`}
            >
              {d} Days
            </button>
          ))}
        </div>
        </div>
      </div>

      {error && <p role="alert" className="rounded-xl border border-danger/30 bg-danger/10 p-4 text-sm text-danger">{error}</p>}

      {/* Stats Row */}
      {data && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          <StatCard 
            icon={<CheckCircle2 className="text-emerald-500" />} 
            label="Completion Rate" 
            value={`${data.completionRate}%`} 
          />
          <StatCard 
            icon={<Clock className="text-primary" />} 
            label="Avg Completion Time" 
            value={`${data.avgCompletionDays} Days`} 
          />
          <StatCard 
            icon={<AlertCircle className="text-rose-500" />} 
            label="Overdue Tasks" 
            value={data.overdueTasks} 
          />
          <StatCard 
            icon={<Layout className="text-purple-500" />} 
            label="Total Tasks" 
            value={data.totalTasks} 
          />
        </div>
      )}

      {/* Charts placeholder */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="p-6">
          <div className="flex items-center gap-2 mb-6">
            <PieChart className="text-text-subtle" />
            <h3 className="text-lg font-bold text-text-main">Status Distribution</h3>
          </div>
          <div className="space-y-4">
            {data?.statusDistribution.map(item => (
              <div key={item.label}>
                <div className="flex justify-between text-sm mb-1 font-bold text-text-main">
                  <span>{item.label}</span>
                  <span>{item.count} ({item.percentage}%)</span>
                </div>
                <div className="w-full bg-surface-2 rounded-full h-2">
                  <div className="bg-primary h-2 rounded-full" style={{ width: `${item.percentage}%` }} />
                </div>
              </div>
            ))}
          </div>
        </Card>

        <Card className="p-6">
          <div className="flex flex-wrap items-center justify-between gap-2 mb-6">
            <div className="flex items-center gap-2">
              <TrendingUp className="text-text-subtle" />
              <h3 className="text-lg font-bold text-text-main">Velocity (Created vs Completed)</h3>
            </div>
            <label className="sr-only" htmlFor="analytics-velocity-timeframe">Velocity timeframe</label>
            <select
              id="analytics-velocity-timeframe"
              value={velocityTimeframe}
              onChange={(event) => { setLoading(true); setVelocityTimeframe(event.target.value); }}
              className="rounded-xl border border-border-subtle bg-surface-0 px-3 py-2 text-sm font-bold text-text-main"
            >
              <option value="Week">Week</option>
              <option value="Month">Month</option>
              <option value="SixMonths">Six Months</option>
              <option value="Year">Year</option>
            </select>
          </div>
          <div className="space-y-4">
            {data?.velocity.map(item => (
              <div key={item.period} className="flex flex-wrap items-center justify-between gap-2 p-3 bg-surface-2/50 rounded-xl">
                <span className="font-bold text-text-muted">{item.period}</span>
                <div className="flex flex-wrap gap-2">
                  <Badge variant="neutral" className="text-primary bg-primary/10">Created: {item.created}</Badge>
                  <Badge variant="success">Completed: {item.completed}</Badge>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}

function StatCard({ icon, label, value }) {
  return (
    <Card className="flex flex-col relative overflow-hidden group" padding="p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="p-3 bg-surface-2 rounded-2xl group-hover:scale-110 transition-transform">
          {icon}
        </div>
      </div>
      <p className="text-sm font-bold text-text-subtle uppercase tracking-wider">{label}</p>
      <h3 className="text-3xl font-black text-text-main mt-1">{value}</h3>
      <div className="absolute -bottom-6 -right-6 w-24 h-24 bg-surface-2/50 rounded-full blur-2xl -z-10 group-hover:bg-primary/10 transition-colors" />
    </Card>
  );
}
