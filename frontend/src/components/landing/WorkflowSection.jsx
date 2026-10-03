import React, { memo, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
// ESLint's core no-unused-vars rule in this project does not count JSX member usage.
// eslint-disable-next-line no-unused-vars
import { motion, useMotionValue, useScroll, useTransform } from 'framer-motion';
import {
  ArrowRight,
  CheckCircle2,
  GitBranch,
  Layers3,
  LineChart,
  LockKeyhole,
  MessageSquareText,
  Play,
  Radar,
  Sparkles,
  Timer,
  Zap,
  Megaphone,
  FileText,
  Image as ImageIcon,
  Search,
  UserPlus,
  FileCheck,
  Laptop,
  Presentation,
  Smile,
  Shield,
  Globe
} from 'lucide-react';
import Button from '@/components/ui/Button';
import { useLanguage } from '@/context/LanguageContext';

const templates = [
  {
    id: 'engineering',
    label: 'Product & Engineering',
    source: 'McKinsey & Company',
    nodes: [
      { id: 'brief', title: 'Product Brief', meta: 'Spec approved', detail: 'Goals, scope, owners, and launch criteria are aligned before work starts.', x: 7, y: 20, accent: '#4F46E5', icon: Layers3, avatars: ['LN', 'AK'], status: 'Ready' },
      { id: 'design', title: 'Design Review', meta: '3 comments', detail: 'Design, copy, and edge states move through review with visible blockers.', x: 36, y: 10, accent: '#06B6D4', icon: MessageSquareText, avatars: ['MT', 'HL'], status: 'In review' },
      { id: 'build', title: 'Build Sprint', meta: '8 tasks active', detail: 'Engineering tasks inherit dependencies and update the roadmap in real time.', x: 30, y: 50, accent: '#4F46E5', icon: GitBranch, avatars: ['DV', 'JR', 'PI'], status: 'Active' },
      { id: 'qa', title: 'QA Gate', meta: '2 blockers', detail: 'Failed checks are surfaced instantly with ownership and next action.', x: 65, y: 33, accent: '#22C55E', icon: LockKeyhole, avatars: ['NS'], status: 'Guarded' },
      { id: 'launch', title: 'Launch Sync', meta: '95% confidence', detail: 'Release readiness updates as approvals, metrics, and open risks change.', x: 77, y: 64, accent: '#06B6D4', icon: Radar, avatars: ['AK', 'DV'], status: 'On track' },
    ],
    connections: [
      { id: 'brief-design', from: 'brief', to: 'design', delay: 0.1 },
      { id: 'brief-build', from: 'brief', to: 'build', delay: 0.2 },
      { id: 'design-qa', from: 'design', to: 'qa', delay: 0.3 },
      { id: 'build-qa', from: 'build', to: 'qa', delay: 0.4 },
      { id: 'qa-launch', from: 'qa', to: 'launch', delay: 0.5 },
      { id: 'build-launch', from: 'build', to: 'launch', delay: 0.6 },
    ],
    metrics: [
      { labelKey: 'Productivity', value: '+25%', icon: Timer, color: '#06B6D4' },
      { labelKey: 'Time-to-Market', value: '-30%', icon: Zap, color: '#22C55E' },
      { labelKey: 'Quality', value: '99.8%', icon: CheckCircle2, color: '#4F46E5' },
    ],
    floating: {
      healthTitle: 'workflow.releaseHealth', healthValue: '92%', healthIcon: Zap,
      pulseTitle: 'workflow.teamPulse', pulseValue: 'workflow.teamLive', pulseAvatars: ['LN', 'AK', 'DV', 'MT', '+4']
    }
  },
  {
    id: 'marketing',
    label: 'Marketing Campaign',
    source: 'HubSpot State of Marketing',
    nodes: [
      { id: 'mkt-brief', title: 'Campaign Brief', meta: 'Goals set', detail: 'Target audience, budget, and messaging aligned.', x: 10, y: 30, accent: '#8B5CF6', icon: FileText, avatars: ['JS', 'AM'], status: 'Approved' },
      { id: 'mkt-content', title: 'Content Creation', meta: 'Drafting', detail: 'Blog posts and social copy being drafted.', x: 35, y: 15, accent: '#F59E0B', icon: MessageSquareText, avatars: ['CK'], status: 'Active' },
      { id: 'mkt-design', title: 'Creative Assets', meta: '2 Revisions', detail: 'Banners, videos, and graphics production.', x: 35, y: 55, accent: '#EC4899', icon: ImageIcon, avatars: ['HL', 'DV'], status: 'In review' },
      { id: 'mkt-seo', title: 'SEO & Legal', meta: 'Pending', detail: 'Keywords optimization and compliance check.', x: 65, y: 35, accent: '#10B981', icon: Search, avatars: ['NS'], status: 'Guarded' },
      { id: 'mkt-publish', title: 'Go Live', meta: 'Scheduled', detail: 'All assets ready for automated deployment.', x: 80, y: 70, accent: '#3B82F6', icon: Megaphone, avatars: ['JS', 'AM', 'CK'], status: 'On track' },
    ],
    connections: [
      { id: 'b-c', from: 'mkt-brief', to: 'mkt-content', delay: 0.1 },
      { id: 'b-d', from: 'mkt-brief', to: 'mkt-design', delay: 0.2 },
      { id: 'c-s', from: 'mkt-content', to: 'mkt-seo', delay: 0.3 },
      { id: 'd-s', from: 'mkt-design', to: 'mkt-seo', delay: 0.4 },
      { id: 's-p', from: 'mkt-seo', to: 'mkt-publish', delay: 0.5 },
    ],
    metrics: [
      { labelKey: 'Lead Gen', value: '+14.5%', icon: Radar, color: '#8B5CF6' },
      { labelKey: 'ROI Growth', value: '3.2x', icon: LineChart, color: '#F59E0B' },
      { labelKey: 'Engagement', value: '+40%', icon: MessageSquareText, color: '#3B82F6' },
    ],
    floating: {
      healthTitle: 'Campaign Readiness', healthValue: '85%', healthIcon: Sparkles,
      pulseTitle: 'Creative Team', pulseValue: '6 creators active', pulseAvatars: ['JS', 'AM', 'CK', 'HL', '+2']
    }
  },
  {
    id: 'hr',
    label: 'Employee Onboarding',
    source: 'Brandon Hall Group',
    nodes: [
      { id: 'hr-offer', title: 'Offer Accepted', meta: 'Signed', detail: 'Candidate signed the offer and background check initiated.', x: 8, y: 20, accent: '#22C55E', icon: UserPlus, avatars: ['MK'], status: 'Done' },
      { id: 'hr-bg', title: 'Background Check', meta: 'Processing', detail: 'Verifying employment history and references.', x: 30, y: 40, accent: '#F59E0B', icon: FileCheck, avatars: ['RJ'], status: 'Active' },
      { id: 'hr-it', title: 'IT Provisioning', meta: 'Laptop shipped', detail: 'Setting up accounts, email, and hardware shipping.', x: 55, y: 15, accent: '#3B82F6', icon: Laptop, avatars: ['IT'], status: 'On track' },
      { id: 'hr-orient', title: 'Orientation Prep', meta: 'Schedule set', detail: 'Manager 1:1s and team introductions scheduled.', x: 55, y: 65, accent: '#8B5CF6', icon: Presentation, avatars: ['MK', 'HM'], status: 'Ready' },
      { id: 'hr-day1', title: 'Day 1 Ready', meta: 'All set', detail: 'Employee has access to all tools and schedule.', x: 82, y: 40, accent: '#EC4899', icon: Smile, avatars: ['MK', 'RJ', 'IT'], status: 'Guarded' },
    ],
    connections: [
      { id: 'o-b', from: 'hr-offer', to: 'hr-bg', delay: 0.1 },
      { id: 'b-i', from: 'hr-bg', to: 'hr-it', delay: 0.2 },
      { id: 'b-or', from: 'hr-bg', to: 'hr-orient', delay: 0.3 },
      { id: 'i-d', from: 'hr-it', to: 'hr-day1', delay: 0.4 },
      { id: 'or-d', from: 'hr-orient', to: 'hr-day1', delay: 0.5 },
    ],
    metrics: [
      { labelKey: 'Retention', value: '82%', icon: UserPlus, color: '#22C55E' },
      { labelKey: 'Setup Time', value: '-65%', icon: Timer, color: '#3B82F6' },
      { labelKey: 'Day 1 Satisfaction', value: '96%', icon: Smile, color: '#EC4899' },
    ],
    floating: {
      healthTitle: 'Compliance Check', healthValue: '100%', healthIcon: Shield,
      pulseTitle: 'HR & IT Sync', pulseValue: '4 admins online', pulseAvatars: ['MK', 'RJ', 'IT', 'HM']
    }
  }
];


const sectionVariants = {
  hidden: { opacity: 0, y: 36 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.7, ease: [0.16, 1, 0.3, 1], staggerChildren: 0.1 },
  },
};

const itemVariants = {
  hidden: { opacity: 0, y: 22 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.55, ease: [0.16, 1, 0.3, 1] } },
};

const initialNodePositions = (nodes) => nodes.reduce((positions, node) => {
  positions[node.id] = { x: node.x, y: node.y };
  return positions;
}, {});

const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

export default function WorkflowSection() {
  const [activeTemplateIdx, setActiveTemplateIdx] = useState(0);
  const activeTemplate = templates[activeTemplateIdx];
  const [activeNode, setActiveNode] = useState(null);
  const dragConstraintsRef = useRef(null);
  const { t } = useLanguage();

  const translatedNodes = useMemo(
    () => activeTemplate.nodes.map((node) => ({
      ...node,
      title: activeTemplate.id === 'engineering' ? t(`workflow.node.${node.id}.title`) || node.title : node.title,
      meta: activeTemplate.id === 'engineering' ? t(`workflow.node.${node.id}.meta`) || node.meta : node.meta,
      detail: activeTemplate.id === 'engineering' ? t(`workflow.node.${node.id}.detail`) || node.detail : node.detail,
      status: activeTemplate.id === 'engineering' ? t(`workflow.node.${node.id}.status`) || node.status : node.status,
    })),
    [t, activeTemplate],
  );

  const translatedMetrics = useMemo(
    () => activeTemplate.metrics.map((metric) => ({
      ...metric,
      label: activeTemplate.id === 'engineering' ? t(metric.labelKey) || metric.labelKey : metric.labelKey
    })),
    [t, activeTemplate],
  );

  const activeDetails = useMemo(
    () => translatedNodes.find((node) => node.id === activeNode) || translatedNodes[Math.floor(translatedNodes.length / 2)],
    [activeNode, translatedNodes],
  );

  const HealthIcon = activeTemplate.floating.healthIcon;

  return (
    <section id="workflow" className="relative isolate overflow-hidden bg-bg-main px-6 py-24 text-text-main sm:py-28 lg:py-32 transition-colors duration-300">
      <motion.div
        className="relative z-10 mx-auto grid max-w-[85rem] items-center gap-14 lg:grid-cols-[0.42fr_0.58fr] lg:gap-16"
        variants={sectionVariants}
        initial="hidden"
        whileInView="visible"
        viewport={{ once: true, amount: 0.28 }}
      >
        <div>
          <motion.div variants={itemVariants} className="mb-6 inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.05] px-3 py-1.5 text-xs font-black uppercase tracking-[0.18em] text-cyan-200 shadow-[0_0_40px_rgba(6,182,212,0.12)] backdrop-blur-xl">
            <Sparkles size={14} />
            {t('workflow.badge') || 'Workflow Intelligence'}
          </motion.div>

          <motion.h2 variants={itemVariants} className="max-w-2xl text-4xl font-black leading-[1.05] tracking-tight text-text-main sm:text-5xl lg:text-6xl">
            {t('workflow.title') || 'See every handoff, blocker, and launch path in one living workflow.'}
          </motion.h2>

          <motion.p variants={itemVariants} className="mt-6 max-w-xl text-base font-medium leading-8 text-text-muted sm:text-lg">
            {t('workflow.description')}
          </motion.p>

          <motion.div variants={itemVariants} className="mt-8 flex flex-col gap-4 sm:flex-row">
            <Link to="/register" className="w-full sm:w-auto">
              <Button size="lg" className="h-[3.25rem] w-full bg-[#4F46E5] px-7 shadow-[0_18px_50px_rgba(79,70,229,0.35)] hover:bg-indigo-500" rightIcon={<ArrowRight size={18} />}>
                {t('workflow.ctaBuild') || 'Build a workflow'}
              </Button>
            </Link>

            <div className="flex gap-1.5 bg-white/5 p-1 rounded-2xl border border-white/10 backdrop-blur-xl">
              {templates.map((tpl, idx) => (
                <button
                  key={tpl.id}
                  onClick={() => setActiveTemplateIdx(idx)}
                  className={`px-4 py-2.5 rounded-xl text-xs font-black uppercase tracking-widest transition-all duration-300 ${activeTemplateIdx === idx
                      ? 'bg-white/10 text-cyan-200 shadow-lg'
                      : 'text-slate-500 hover:text-slate-300 hover:bg-white/5'
                    }`}
                >
                  {tpl.id === 'engineering' ? 'Product' : tpl.id === 'marketing' ? 'Marketing' : 'HR'}
                </button>
              ))}
            </div>
          </motion.div>

          <motion.div variants={itemVariants} className="mt-9 grid gap-4 text-sm font-bold text-text-muted">
            {['Visual dependency mapping across teams', 'Live status signals for every handoff', 'Workflow templates for product, design, and ops'].map((bullet) => (
              <div key={bullet} className="flex items-center gap-3">
                <span className="flex h-6 w-6 items-center justify-center rounded-full border border-cyan-300/25 bg-cyan-300/10 text-cyan-500 dark:text-cyan-200">
                  <CheckCircle2 size={14} />
                </span>
                <span>{bullet}</span>
              </div>
            ))}
          </motion.div>

          <motion.div variants={itemVariants} className="mt-10 flex items-center gap-3 px-4 py-3 rounded-2xl bg-bg-card border border-border-subtle w-fit shadow-sm">
            <div className="flex flex-col">
              <span className="text-xl font-black text-text-main">{templates[activeTemplateIdx].metrics[0].value}</span>
              <span className="text-[10px] font-bold uppercase tracking-widest text-text-muted">Industry {templates[activeTemplateIdx].metrics[0].labelKey}</span>
            </div>
            <div className="w-px h-8 bg-border-subtle mx-2" />
            <div className="flex flex-col">
              <span className="text-xl font-black text-text-main">{templates[activeTemplateIdx].metrics[1].value}</span>
              <span className="text-[10px] font-bold uppercase tracking-widest text-text-muted">Source: {templates[activeTemplateIdx].source}</span>
            </div>
          </motion.div>
        </div>

        <motion.div
          ref={dragConstraintsRef}
          className="relative min-h-[550px] overflow-hidden rounded-[2.5rem] lg:min-h-[680px] shadow-[0_0_100px_rgba(0,0,0,0.4)] border border-white/5"
        >
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_50%_0%,rgba(79,70,229,0.12),transparent)]" />

          <WorkflowCanvas
            key={activeTemplate.id}
            activeNode={activeNode}
            setActiveNode={setActiveNode}
            activeDetails={activeDetails}
            nodes={translatedNodes}
            connections={activeTemplate.connections}
            metrics={translatedMetrics}
            dragConstraintsRef={dragConstraintsRef}
          />

          <FloatingCard key={`health-${activeTemplate.id}`} className="left-4 top-5 sm:left-8" delay={0.2} dragConstraintsRef={dragConstraintsRef}>
            <div className="flex items-center justify-between gap-5">
              <div>
                <p className="text-[11px] font-black uppercase tracking-[0.2em] text-text-muted">
                  {activeTemplate.id === 'engineering' ? t(activeTemplate.floating.healthTitle) : activeTemplate.floating.healthTitle}
                </p>
                <p className="mt-2 text-2xl font-black text-text-main">{activeTemplate.floating.healthValue}</p>
              </div>
              <div className="grid h-12 w-12 place-items-center rounded-2xl bg-cyan-400/15 text-cyan-500 dark:text-cyan-200">
                <HealthIcon size={20} />
              </div>
            </div>
          </FloatingCard>

          <FloatingCard key={`pulse-${activeTemplate.id}`} className="right-4 top-20 hidden w-52 sm:block" delay={0.4} dragConstraintsRef={dragConstraintsRef}>
            <p className="text-[11px] font-black uppercase tracking-[0.2em] text-text-muted">
              {activeTemplate.id === 'engineering' ? t(activeTemplate.floating.pulseTitle) : activeTemplate.floating.pulseTitle}
            </p>
            <div className="mt-4 flex -space-x-2">
              {activeTemplate.floating.pulseAvatars.map((name) => (
                <Avatar key={name} label={name} />
              ))}
            </div>
          </FloatingCard>

          <FloatingCard key={`node-${activeTemplate.id}`} className="bottom-6 right-5 w-64 sm:right-10" delay={0.6} dragConstraintsRef={dragConstraintsRef}>
            <p className="text-[11px] font-black uppercase tracking-[0.2em] text-text-muted">{t('workflow.currentNode')}</p>
            <p className="mt-2 text-lg font-black text-text-main">{activeDetails.title}</p>
            <p className="mt-2 text-xs font-medium leading-5 text-text-muted">{activeDetails.detail}</p>
          </FloatingCard>
        </motion.div>
      </motion.div>
    </section>
  );
}

function WorkflowCanvas({ activeNode, setActiveNode, nodes, connections, metrics, dragConstraintsRef }) {
  const { t } = useLanguage();
  const canvasRef = useRef(null);
  // Re-initialize node positions when nodes change (template change)
  const [nodePositions, setNodePositions] = useState(() => initialNodePositions(nodes));
  const [dragOffsets, setDragOffsets] = useState({});
  const [hoveredNodeId, setHoveredNodeId] = useState(null); // Node that the dragged node is currently "over"
  const [activeDraggingId, setActiveDraggingId] = useState(null);
  const liveLabel = t('workflow.live') || 'Live';

  const permanentConnections = useMemo(
    () => connections.map((connection) => ({
      ...connection,
      isDynamic: false,
      path: createConnectionPath(
        getNodeCenter(connection.from, nodePositions, dragOffsets),
        getNodeCenter(connection.to, nodePositions, dragOffsets),
      ),
    })),
    [connections, dragOffsets, nodePositions],
  );

  const proximityConnection = useMemo(() => {
    if (!activeDraggingId || !hoveredNodeId || activeDraggingId === hoveredNodeId) return null;

    return {
      id: `dynamic-${activeDraggingId}-${hoveredNodeId}`,
      from: activeDraggingId,
      to: hoveredNodeId,
      isDynamic: true,
      path: createConnectionPath(
        getNodeCenter(activeDraggingId, nodePositions, dragOffsets),
        getNodeCenter(hoveredNodeId, nodePositions, dragOffsets),
      ),
    };
  }, [activeDraggingId, hoveredNodeId, nodePositions, dragOffsets]);

  const allConnections = useMemo(() => {
    return proximityConnection ? [...permanentConnections, proximityConnection] : permanentConnections;
  }, [permanentConnections, proximityConnection]);

  const getOffsetAsPercent = (offset) => {
    const rect = canvasRef.current?.getBoundingClientRect();

    if (!rect) {
      return { x: 0, y: 0 };
    }

    return {
      x: (offset.x / rect.width) * 100,
      y: (offset.y / rect.height) * 100,
    };
  };

  const handleNodeDrag = (nodeId, offset) => {
    const percentOffset = getOffsetAsPercent(offset);
    setDragOffsets((current) => ({
      ...current,
      [nodeId]: percentOffset,
    }));
    setActiveDraggingId(nodeId);

    // Calculate absolute position of dragging node
    const currentPos = nodePositions[nodeId];
    if (!currentPos) return;

    const absX = currentPos.x + percentOffset.x;
    const absY = currentPos.y + percentOffset.y;

    // Check for nearest neighbor (overlap)
    let nearest = null;
    let minDistance = 15; // Threshold for connection

    nodes.forEach(target => {
      if (target.id === nodeId) return;
      const targetPos = nodePositions[target.id];
      const dist = Math.sqrt(Math.pow(absX - targetPos.x, 2) + Math.pow(absY - targetPos.y, 2));
      if (dist < minDistance) {
        minDistance = dist;
        nearest = target.id;
      }
    });

    setHoveredNodeId(nearest);
  };

  const handleNodeDragEnd = (nodeId, offset) => {
    const percentOffset = getOffsetAsPercent(offset);

    setNodePositions((current) => ({
      ...current,
      [nodeId]: {
        x: clamp((current[nodeId]?.x || 0) + percentOffset.x, 0, 76),
        y: clamp((current[nodeId]?.y || 0) + percentOffset.y, 0, 76),
      },
    }));
    setDragOffsets((current) => ({
      ...current,
      [nodeId]: { x: 0, y: 0 },
    }));
    setActiveDraggingId(null);
    setHoveredNodeId(null);
  };

  return (
    <div ref={canvasRef} className="relative h-[520px] overflow-hidden rounded-[2rem] p-4 sm:h-[620px]">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(6,182,212,0.12),transparent_34%),linear-gradient(rgba(255,255,255,0.045)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.045)_1px,transparent_1px)] bg-[size:100%_100%,44px_44px,44px_44px]" />
      <svg className="absolute inset-0 h-full w-full" viewBox="0 0 860 560" preserveAspectRatio="none" aria-hidden="true">
        <defs>
          <linearGradient id="workflowLine" x1="0" x2="1" y1="0" y2="1">
            <stop offset="0%" stopColor="#4F46E5" />
            <stop offset="100%" stopColor="#06B6D4" />
          </linearGradient>
          <filter id="lineGlow">
            <feGaussianBlur stdDeviation="3" result="coloredBlur" />
            <feMerge>
              <feMergeNode in="coloredBlur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>
        {allConnections.map((connection) => (
          <WorkflowConnection
            key={connection.id}
            {...connection}
            active={Boolean(activeNode) || connection.isDynamic}
            isDynamic={connection.isDynamic}
          />
        ))}
      </svg>

      {nodes.map((node, index) => (
        <WorkflowNode
          key={node.id}
          node={node}
          index={index}
          isActive={activeNode === node.id}
          onEnter={() => setActiveNode(node.id)}
          onLeave={() => setActiveNode(null)}
          liveLabel={liveLabel}
          dragConstraintsRef={dragConstraintsRef}
          position={nodePositions[node.id]}
          onNodeDrag={handleNodeDrag}
          onNodeDragEnd={handleNodeDragEnd}
        />
      ))}

      <div className="absolute bottom-6 left-5 grid w-[calc(100%-2.5rem)] grid-cols-1 gap-3 sm:left-8 sm:w-auto sm:grid-cols-3">
        {metrics.map((metric, index) => (
          <MetricPill key={metric.label} metric={metric} index={index} />
        ))}
      </div>
    </div>
  );
}

function WorkflowConnection({ path, delay, active, isDynamic }) {
  return (
    <g>
      <motion.path
        d={path}
        fill="none"
        stroke={isDynamic ? "rgba(34, 211, 238, 0.2)" : "rgba(255,255,255,0.08)"}
        strokeWidth={isDynamic ? "3" : "2"}
        initial={isDynamic ? { opacity: 0 } : false}
        animate={isDynamic ? { opacity: 1 } : false}
      />
      <motion.path
        d={path}
        fill="none"
        stroke={isDynamic ? "#22D3EE" : "url(#workflowLine)"}
        strokeLinecap="round"
        strokeWidth={active ? '3.2' : '2.4'}
        filter="url(#lineGlow)"
        initial={isDynamic ? { pathLength: 0, opacity: 0 } : { pathLength: 0, opacity: 0 }}
        animate={isDynamic ? { pathLength: 1, opacity: 1 } : {}}
        whileInView={!isDynamic ? { pathLength: 1, opacity: active ? 1 : 0.72 } : {}}
        viewport={{ once: true }}
        transition={isDynamic ? { duration: 0.3 } : { delay, duration: 1.1, ease: 'easeInOut' }}
      />
      <motion.circle r={isDynamic ? "5" : "4.5"} fill={isDynamic ? "#22D3EE" : "#67E8F9"} filter="url(#lineGlow)">
        <animateMotion dur={isDynamic ? "2s" : "4.5s"} repeatCount="indefinite" path={path} />
      </motion.circle>
    </g>
  );
}

function getNodeCenter(nodeId, nodePositions, dragOffsets) {
  const position = nodePositions[nodeId] || { x: 0, y: 0 };
  const offset = dragOffsets[nodeId] || { x: 0, y: 0 };

  return {
    x: (position.x + offset.x) * 8.6 + 110,
    y: (position.y + offset.y) * 5.6 + 78,
  };
}

function createConnectionPath(from, to) {
  const distance = Math.abs(to.x - from.x);
  const curve = clamp(distance * 0.48, 72, 170);
  const verticalPull = (to.y - from.y) * 0.12;

  return [
    `M${from.x.toFixed(1)} ${from.y.toFixed(1)}`,
    `C${(from.x + curve).toFixed(1)} ${(from.y + verticalPull).toFixed(1)}`,
    `${(to.x - curve).toFixed(1)} ${(to.y - verticalPull).toFixed(1)}`,
    `${to.x.toFixed(1)} ${to.y.toFixed(1)}`,
  ].join(' ');
}

function WorkflowNode({
  node,
  index,
  isActive,
  onEnter,
  onLeave,
  liveLabel,
  dragConstraintsRef,
  position,
  onNodeDrag,
  onNodeDragEnd,
}) {
  const Icon = node.icon;
  const dragX = useMotionValue(0);
  const dragY = useMotionValue(0);

  return (
    <motion.button
      type="button"
      className="group absolute w-[13.75rem] max-w-[42vw] cursor-grab touch-none rounded-2xl border border-white/[0.08] bg-white/[0.05] p-4 text-left shadow-[0_18px_50px_rgba(0,0,0,0.24)] outline-none backdrop-blur-xl transition-colors hover:border-cyan-300/35 active:cursor-grabbing focus-visible:border-cyan-300/60 sm:max-w-none"
      style={{ left: `${position?.x || node.x}%`, top: `${position?.y || node.y}%`, x: dragX, y: dragY }}
      drag
      dragConstraints={dragConstraintsRef}
      dragElastic={0.08}
      dragMomentum={false}
      initial={{ opacity: 0, scale: 0.88, y: 18 }}
      whileInView={{ opacity: 1, scale: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ delay: 0.16 + index * 0.08, duration: 0.55, ease: [0.16, 1, 0.3, 1] }}
      whileHover={{ y: -8, scale: 1.035 }}
      whileDrag={{
        scale: 1.06,
        zIndex: 40,
        boxShadow: '0 26px 80px rgba(6, 182, 212, 0.24)',
      }}
      onDrag={(_, info) => onNodeDrag(node.id, info.offset)}
      onDragEnd={(_, info) => {
        onNodeDragEnd(node.id, info.offset);
        dragX.set(0);
        dragY.set(0);
      }}
      onMouseEnter={onEnter}
      onMouseLeave={onLeave}
      onFocus={onEnter}
      onBlur={onLeave}
    >
      <div className="absolute inset-0 rounded-2xl opacity-0 shadow-[0_0_45px_rgba(6,182,212,0.22)] transition-opacity duration-300 group-hover:opacity-100" />
      <div className="relative flex items-start justify-between gap-3">
        <div className="grid h-10 w-10 shrink-0 place-items-center rounded-xl border border-white/10" style={{ backgroundColor: `${node.accent}22`, color: node.accent }}>
          <Icon size={18} />
        </div>
        <span className="rounded-full border border-white/10 bg-white/[0.06] px-2 py-1 text-[10px] font-black uppercase tracking-[0.16em] text-slate-300">
          {node.status}
        </span>
      </div>

      <div className="relative mt-4">
        <h3 className="text-sm font-black text-white">{node.title}</h3>
        <p className="mt-1 text-xs font-bold text-slate-400">{node.meta}</p>
      </div>

      <div className="relative mt-4 flex items-center justify-between">
        <div className="flex -space-x-2">
          {node.avatars.map((avatar) => (
            <Avatar key={avatar} label={avatar} small />
          ))}
        </div>
        <span className="flex items-center gap-1.5 text-[11px] font-black text-cyan-200">
          <span className={`h-2 w-2 rounded-full ${isActive ? 'bg-cyan-300' : 'bg-emerald-400'}`} />
          {liveLabel}
        </span>
      </div>

      <motion.div
        className="pointer-events-none absolute left-4 right-4 top-[calc(100%+0.75rem)] z-20 rounded-xl border border-white/10 bg-slate-950/90 p-3 text-xs font-medium leading-5 text-slate-300 opacity-0 shadow-2xl backdrop-blur-xl group-hover:opacity-100 group-focus-visible:opacity-100"
        initial={false}
        animate={{ y: isActive ? 0 : -4 }}
      >
        {node.detail}
      </motion.div>
    </motion.button>
  );
}

function FloatingCard({ children, className = '', delay = 0, dragConstraintsRef }) {
  return (
    <motion.div
      className={`absolute z-20 cursor-grab touch-none rounded-2xl border border-white/[0.08] bg-white/[0.06] shadow-[0_20px_70px_rgba(0,0,0,0.32)] backdrop-blur-2xl active:cursor-grabbing ${className}`}
      drag
      dragConstraints={dragConstraintsRef}
      dragElastic={0.08}
      dragMomentum={false}
      initial={{ opacity: 0, y: 24 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ delay, duration: 0.45 }}
      whileHover={{ scale: 1.02 }}
      whileDrag={{
        scale: 1.04,
        zIndex: 50,
        boxShadow: '0 26px 90px rgba(79, 70, 229, 0.26)',
      }}
    >
      <motion.div
        className="p-4"
        animate={{ y: [0, -8, 0] }}
        transition={{ delay, duration: 5, repeat: Infinity, ease: 'easeInOut' }}
      >
        {children}
      </motion.div>
    </motion.div>
  );
}

function MetricPill({ metric, index }) {
  const Icon = metric.icon;

  return (
    <motion.div
      className="flex min-w-0 items-center gap-3 rounded-2xl border border-white/[0.08] bg-slate-950/45 px-4 py-3 backdrop-blur-xl"
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ delay: 0.7 + index * 0.08, duration: 0.45 }}
    >
      <span className="grid h-9 w-9 shrink-0 place-items-center rounded-xl" style={{ backgroundColor: `${metric.color}22`, color: metric.color }}>
        <Icon size={17} />
      </span>
      <span className="min-w-0">
        <span className="block text-sm font-black text-white">{metric.value}</span>
        <span className="block truncate text-[11px] font-bold uppercase tracking-[0.14em] text-slate-400">{metric.label}</span>
      </span>
    </motion.div>
  );
}

function Avatar({ label, small = false }) {
  return (
    <span className={`${small ? 'h-7 w-7 text-[9px]' : 'h-9 w-9 text-[10px]'} grid place-items-center rounded-full border border-white/15 bg-gradient-to-br from-indigo-500 to-cyan-400 font-black text-white shadow-lg`}>
      {label}
    </span>
  );
}

const AnimatedBackground = memo(function AnimatedBackground() {
  return (
    <div className="absolute inset-0 -z-10">
      <div className="absolute inset-0 bg-[linear-gradient(115deg,rgba(79,70,229,0.22),transparent_30%,rgba(6,182,212,0.16)_62%,transparent_78%)]" />
      <motion.div
        className="absolute inset-x-0 top-0 h-1/2 bg-[linear-gradient(180deg,rgba(6,182,212,0.11),transparent)]"
        animate={{ opacity: [0.45, 0.75, 0.45] }}
        transition={{ duration: 7, repeat: Infinity, ease: 'easeInOut' }}
      />
      <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,0.035)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.035)_1px,transparent_1px)] bg-[size:72px_72px]" />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,rgba(79,70,229,0.28),transparent_42%),radial-gradient(ellipse_at_bottom_right,rgba(6,182,212,0.18),transparent_48%)]" />
    </div>
  );
});
