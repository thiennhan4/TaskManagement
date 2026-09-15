import React, { useEffect, useState } from 'react';
import { useLanguage } from '@/context/LanguageContext';
import { analyticsApi } from '@/api/analyticsApi';
import { Layout, CheckCircle2, AlertCircle, Clock, Loader2, BarChart2, PieChart, TrendingUp } from 'lucide-react';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';

export default function AnalyticsPage() {
  const { t } = useLanguage();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [days, setDays] = useState(30);

  useEffect(() => {
    fetchData(days);
  }, [days]);

  const fetchData = async (daysCount) => {
    setLoading(true);
    try {
      const res = await analyticsApi.getOverview(daysCount);
      setData(res.data.data);
    } catch (err) {
      console.error('Failed to fetch analytics', err);
    } finally {
      setLoading(false);
    }
  };

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

        <div className="flex bg-surface-0 p-1 rounded-xl shadow-sm border border-border-subtle">
          {[7, 14, 30, 90].map(d => (
            <button
              key={d}
              onClick={() => setDays(d)}
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
          <div className="flex items-center gap-2 mb-6">
            <TrendingUp className="text-text-subtle" />
            <h3 className="text-lg font-bold text-text-main">Velocity (Created vs Completed)</h3>
          </div>
          <div className="space-y-4">
            {data?.velocity.map(item => (
              <div key={item.period} className="flex items-center justify-between p-3 bg-surface-2/50 rounded-xl">
                <span className="font-bold text-text-muted">{item.period}</span>
                <div className="flex gap-4">
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
