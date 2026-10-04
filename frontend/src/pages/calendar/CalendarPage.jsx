import { useEffect, useMemo, useRef } from 'react';
import { useShallow } from 'zustand/react/shallow';
import { eachDayOfInterval, format, isSameMonth, isToday, setHours } from 'date-fns';
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react';
import { useCalendarStore } from '@/stores/useCalendarStore';
import { useLanguage } from '@/context/LanguageContext';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { calendarRange, calendarLabel, layoutCalendarTasks } from '@/utils/calendarLayout';
import { TASK_PRIORITY } from '@/constants/taskStatus';
import Button from '@/components/ui/Button';
import CalendarTaskModal from '@/components/tasks/CalendarTaskModal';
import TaskDetailDrawer from '@/components/tasks/TaskDetailDrawer';

const HOURS = Array.from({ length: 24 }, (_, i) => i);

export default function CalendarPage() {
  const { t } = useLanguage();
  const state = useCalendarStore(useShallow(s => ({ currentDate: s.currentDate, view: s.view, setView: s.setView, navigate: s.navigate, setToday: s.setToday, tasks: s.tasks, taskPage: s.taskPage, loadError: s.loadError, isLoading: s.isLoading, fetchCalendarTasks: s.fetchCalendarTasks, selectedDate: s.selectedDate, setSelectedDate: s.setSelectedDate, selectedTask: s.selectedTask, setSelectedTask: s.setSelectedTask, isTaskDrawerOpen: s.isTaskDrawerOpen, setTaskDrawerOpen: s.setTaskDrawerOpen, isAddTaskModalOpen: s.isAddTaskModalOpen, setAddTaskModalOpen: s.setAddTaskModalOpen })));
  const { currentDate, view, tasks, taskPage, isLoading, loadError, fetchCalendarTasks, setView } = state;
  const phone = useMediaQuery('(max-width: 639px)');
  const chosenView = useRef(false);
  useEffect(() => { if (!chosenView.current) setView(phone ? 'day' : 'week'); }, [phone, setView]);
  useEffect(() => { fetchCalendarTasks(); }, [currentDate, view, fetchCalendarTasks]);
  const range = useMemo(() => calendarRange(currentDate, view), [currentDate, view]);
  const days = useMemo(() => eachDayOfInterval(range), [range]);
  const grouped = useMemo(() => layoutCalendarTasks(tasks, range), [tasks, range]);
  const zone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const openSlot = (day, hour = 0) => state.setSelectedDate(setHours(day, hour));
  const eventButton = (event, timed) => <button key={event.task.id} type="button"
    onClick={() => state.setSelectedTask(event.task)}
    aria-label={event.task.title + ', ' + format(event.start, 'MMM d HH:mm') + ' to ' + format(event.end, 'MMM d HH:mm')}
    className={(timed ? 'absolute z-10 ' : 'relative block w-full mb-1 ') + 'rounded-lg border border-border-subtle border-l-4 bg-surface-2 px-2 py-1 text-left text-xs font-bold text-text-main overflow-hidden hover:bg-hover-bg'}
    style={{ borderLeftColor: event.task.color || (TASK_PRIORITY[event.task.priority] || TASK_PRIORITY.Medium).accent,
      ...(timed ? { top: (event.from / 1440 * 100) + '%', height: ((Math.min(1440, event.to) - event.from) / 1440 * 100) + '%', left: 'calc(' + (event.lane / event.lanes * 100) + '% + 2px)', width: 'calc(' + (100 / event.lanes) + '% - 4px)' } : {}) }}>
    <span className="block truncate">{event.task.title}</span><span className="block text-text-muted">{format(event.start, 'HH:mm')}</span>
  </button>;

  return <div className="min-w-0 space-y-4 pb-8">
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="min-w-0"><h1 className="text-2xl sm:text-3xl font-black">{t('calendar.title')}</h1><p className="text-sm text-text-muted">Times shown in {zone}.</p></div>
      <Button leftIcon={<Plus size={18} />} onClick={() => state.setSelectedDate(new Date())}>{t('calendar.addTask')}</Button>
    </div>
    <div className="flex flex-wrap items-center gap-2 rounded-2xl border border-border-subtle bg-surface-0 p-2">
      <div aria-label="Calendar view" className="flex gap-1">{['day', 'week', 'month'].map(option => <Button key={option} variant={view === option ? 'primary' : 'ghost'} aria-pressed={view === option} onClick={() => { chosenView.current = true; state.setView(option); }}>{option[0].toUpperCase() + option.slice(1)}</Button>)}</div>
      <Button variant="ghost" size="icon" aria-label={'Previous ' + view} onClick={() => state.navigate(-1)}><ChevronLeft size={20} /></Button>
      <h2 className="flex-1 min-w-0 text-center text-sm font-bold" aria-live="polite">{calendarLabel(currentDate, view)}</h2>
      <Button variant="ghost" size="icon" aria-label={'Next ' + view} onClick={() => state.navigate(1)}><ChevronRight size={20} /></Button>
      <Button variant="outline" onClick={state.setToday}>{t('calendar.today')}</Button>
    </div>
    {isLoading && <p role="status">Loading calendar…</p>}
    {loadError && <div role="alert">{loadError} <Button onClick={() => fetchCalendarTasks()}>Retry</Button></div>}
    {!isLoading && !loadError && !tasks.length && <p className="text-text-muted">No tasks in this date range.</p>}
    <p className="text-sm text-text-muted">Tasks with only one date are deadline markers. All-day events are not supported.{view !== 'day' && ' Scroll within the calendar to see more days.'}</p>
    {view === 'day' && <section aria-label="Day agenda" className="rounded-2xl border border-border-subtle bg-surface-0 p-3 space-y-2">
      <h3 className="font-bold">Agenda</h3>
      {(grouped.get(format(currentDate, 'yyyy-MM-dd')) || []).map(event => <Button key={event.task.id} variant="ghost" className="w-full justify-start text-left" onClick={() => state.setSelectedTask(event.task)}>{format(event.start, 'HH:mm')} · <span className="min-w-0 break-words">{event.task.title}</span></Button>)}
    </section>}
    <section aria-label={view + ' calendar'} className="max-w-full min-w-0 overflow-auto overscroll-contain rounded-2xl border border-border-subtle bg-surface-0 max-h-[70dvh]" tabIndex={0}>
      {view === 'month' ? <div className="grid grid-cols-7 min-w-[700px]">
        {days.slice(0, 7).map(day => <div key={day.toISOString()} className="p-2 text-center font-bold">{format(day, 'EEE')}</div>)}
        {days.map(day => <div key={day.toISOString()} className={'min-h-32 min-w-0 border-t border-r border-border-subtle p-2 ' + (isSameMonth(day, currentDate) ? '' : 'bg-surface-1')}>
          <button type="button" aria-label={'Add task on ' + format(day, 'MMMM d, yyyy')} onClick={() => openSlot(day)} className={'mb-2 min-h-10 min-w-10 rounded-xl font-bold ' + (isToday(day) ? 'bg-primary text-text-inverse' : '')}>{format(day, 'd')}</button>
          {(grouped.get(format(day, 'yyyy-MM-dd')) || []).map(event => eventButton(event, false))}
        </div>)}
      </div> : <div style={{ minWidth: view === 'week' ? 1050 : undefined }}>
        <div className="sticky top-0 z-20 flex border-b border-border-subtle bg-surface-0">
          <div className="w-12 shrink-0" /><div className="grid flex-1" style={{ gridTemplateColumns: 'repeat(' + days.length + ', minmax(0, 1fr))' }}>{days.map(day => <h3 key={day.toISOString()} className={'p-3 text-center font-bold ' + (isToday(day) ? 'bg-primary text-text-inverse' : '')}>{format(day, 'EEE d')}</h3>)}</div>
        </div>
        <div className="flex">
          <div aria-hidden="true" className="w-12 shrink-0">{HOURS.map(hour => <div key={hour} className="h-20 text-xs text-text-muted text-center pt-1">{String(hour).padStart(2, '0')}:00</div>)}</div>
          <div className="grid flex-1 min-w-0" style={{ gridTemplateColumns: 'repeat(' + days.length + ', minmax(0, 1fr))' }}>{days.map(day => <div key={day.toISOString()} className="relative border-r border-border-subtle">
            {HOURS.map(hour => <button type="button" key={hour} aria-label={'Add task ' + format(day, 'MMMM d, yyyy') + ' at ' + hour + ':00'} onClick={() => openSlot(day, hour)} className="block h-20 w-full border-b border-border-subtle hover:bg-hover-bg focus-visible:bg-primary/10" />)}
            {(grouped.get(format(day, 'yyyy-MM-dd')) || []).map(event => eventButton(event, true))}
          </div>)}</div>
        </div>
      </div>}
    </section>
    <div className="flex flex-wrap items-center gap-3 text-text-muted" aria-live="polite">
      {taskPage && <span>Showing {tasks.length} of {taskPage.totalItems} tasks</span>}
      {taskPage?.page < taskPage?.totalPages && <Button disabled={isLoading} onClick={() => fetchCalendarTasks(taskPage.page + 1)}>Load more tasks</Button>}
    </div>
    <CalendarTaskModal isOpen={state.isAddTaskModalOpen} onClose={() => state.setAddTaskModalOpen(false)} initialDate={state.selectedDate} onSuccess={fetchCalendarTasks} />
    <TaskDetailDrawer isOpen={state.isTaskDrawerOpen} onClose={() => state.setTaskDrawerOpen(false)} taskId={state.selectedTask} onChanged={fetchCalendarTasks} />
  </div>;
}

