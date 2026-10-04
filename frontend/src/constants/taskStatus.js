import { Circle, Timer, CheckCircle2, AlertCircle, ArrowUpCircle, ArrowRightCircle, ArrowDownCircle } from 'lucide-react';

export const TASK_STATUS = {
  Todo: { label: 'To Do', bg: 'bg-surface-2', text: 'text-text-muted', color: 'var(--color-text-muted)', dot: 'bg-text-muted', icon: Circle },
  InProgress: { label: 'In Progress', bg: 'bg-warning/10', text: 'text-text-main', color: 'var(--color-warning)', dot: 'bg-warning', icon: Timer },
  Review: { label: 'Review', bg: 'bg-primary/15', text: 'text-text-main', color: 'var(--color-primary)', dot: 'bg-primary', icon: AlertCircle },
  Done: { label: 'Done', bg: 'bg-success/10', text: 'text-text-main', color: 'var(--color-success)', dot: 'bg-success', icon: CheckCircle2 },
};
export const TASK_PRIORITY = {
  Low: { accent: 'var(--color-text-muted)', label: 'Low', bg: 'bg-surface-2', color: 'text-text-muted', border: 'border-border-subtle', icon: ArrowDownCircle },
  Medium: { accent: 'var(--color-warning)', label: 'Medium', bg: 'bg-warning/10', color: 'text-text-main', border: 'border-warning/30', icon: ArrowRightCircle },
  High: { accent: 'var(--color-danger)', label: 'High', bg: 'bg-danger/10', color: 'text-text-main', border: 'border-danger/30', icon: ArrowUpCircle },
  Critical: { accent: 'var(--color-danger)', label: 'Critical', bg: 'bg-danger/20', color: 'text-text-main', border: 'border-danger/50', icon: AlertCircle },
};
export const TASK_STATUSES = Object.keys(TASK_STATUS);
export const TASK_PRIORITIES = Object.keys(TASK_PRIORITY);
