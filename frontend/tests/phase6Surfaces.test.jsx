import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import MyTasksPage from '@/pages/tasks/MyTasksPage';
import ProjectCard from '@/components/projects/ProjectCard';
import ResponsiveGoogleLogin from '@/components/common/ResponsiveGoogleLogin';
import WorkflowSection from '@/components/landing/WorkflowSection';
import { taskApi } from '@/api/taskApi';

vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: key => key }) }));
vi.mock('@/api/taskApi', () => ({ taskApi: { getMyTasks: vi.fn(), getSummary: vi.fn() } }));
vi.mock('@/components/tasks/TaskModal', () => ({ default: ({ taskId }) => taskId ? <output>Opened {taskId}</output> : null }));
vi.mock('@/components/tasks/TaskFormModal', () => ({ default: () => null }));
vi.mock('@/components/projects/InviteMemberModal', () => ({ default: () => null }));
vi.mock('@react-oauth/google', () => ({ GoogleLogin: ({ width }) => <button>Google width {width}</button> }));
vi.mock('framer-motion', async () => {
  const { forwardRef, createElement } = await import('react');
  const cache = {};
  const motionProps = new Set(['initial', 'animate', 'transition', 'variants', 'whileInView', 'whileHover', 'whileDrag', 'viewport', 'drag', 'dragConstraints', 'dragElastic', 'dragMomentum', 'onDragEnd', 'onDrag']);
  return { motion: new Proxy({}, { get: (_, tag) => cache[tag] ||= forwardRef(function MotionMock({ children, ...props }, ref) {
    const nativeProps = Object.fromEntries(Object.entries(props).filter(([key]) => !motionProps.has(key)));
    return createElement(tag, { ...nativeProps, ref }, children);
  }) }), useMotionValue: () => ({ set: vi.fn() }) };
});
beforeEach(() => { vi.clearAllMocks(); });
afterEach(() => { cleanup(); vi.unstubAllGlobals(); vi.restoreAllMocks(); });

it('My Tasks keeps status, priority, deadline and actions beside a semantic task title', async () => {
  const task = { id: 'one', title: 'Important deadline', status: 'Review', priority: 'Critical', dueDate: '2026-01-01' };
  taskApi.getMyTasks.mockResolvedValue({ data: { data: { items: [task], page: 1, totalPages: 1, totalItems: 1 } } });
  taskApi.getSummary.mockResolvedValue({ data: { data: { total: 1 } } });
  render(<MemoryRouter><MyTasksPage /></MemoryRouter>);
  const title = await screen.findByRole('button', { name: task.title });
  expect(screen.getAllByText('Critical').length).toBeGreaterThan(0);
  expect(screen.getAllByText('Review').length).toBeGreaterThan(0);
  expect(screen.getByRole('button', { name: 'View task' })).toBeTruthy();
  expect(screen.getByRole('button', { name: 'Edit task' })).toBeTruthy();
  fireEvent.click(title); expect(screen.getByText('Opened one')).toBeTruthy();
});

it('ProjectCard has a real primary link and secondary controls do not navigate', () => {
  const project = { id: 'project', name: 'Accessible project', slug: 'project', status: 'Active', createdAt: '2026-10-01' };
  function Location() { return <output aria-label="Route">{useLocation().pathname}</output>; }
  render(<MemoryRouter><ProjectCard project={project} /><Location /></MemoryRouter>);
  fireEvent.click(screen.getByRole('button', { name: 'Actions for Accessible project' }));
  expect(screen.getByLabelText('Route').textContent).toBe('/');
  fireEvent.click(screen.getByRole('link', { name: project.name }));
  expect(screen.getByLabelText('Route').textContent).toBe('/projects/project');
});

it('Google auth uses available width and disconnects its responsive observer', async () => {
  const disconnect = vi.fn(); let changed;
  vi.stubGlobal('ResizeObserver', class { constructor(callback) { changed = callback; } observe() {} disconnect() { disconnect(); } });
  let width = 328;
  vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(() => ({ width }));
  const mounted = render(<ResponsiveGoogleLogin onSuccess={vi.fn()} />);
  await screen.findByRole('button', { name: 'Google width 328' });
  width = 700; act(() => changed());
  expect(screen.getByRole('button', { name: 'Google width 400' })).toBeTruthy();
  mounted.unmount(); expect(disconnect).toHaveBeenCalledTimes(1);
});

it('reduced-motion workflow omits continuous SVG motion and remains keyboard-operable; subscriptions clean up', async () => {
  let reduced = true; const listeners = new Set();
  vi.stubGlobal('matchMedia', () => ({ get matches() { return reduced; }, addEventListener: (_, fn) => listeners.add(fn), removeEventListener: (_, fn) => listeners.delete(fn) }));
  const mounted = render(<MemoryRouter><WorkflowSection /></MemoryRouter>);
  expect(document.querySelectorAll('animateMotion')).toHaveLength(0);
  expect(screen.getAllByRole('button').length).toBeGreaterThan(5);
  reduced = false; act(() => [...listeners].forEach(fn => fn()));
  await waitFor(() => expect(document.querySelectorAll('animateMotion').length).toBeGreaterThan(0));
  mounted.unmount(); expect(listeners.size).toBe(0);
});
