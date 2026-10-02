import React, { useEffect, useMemo, useRef } from 'react';
import { 
  format, 
  startOfWeek, 
  endOfWeek, 
  addDays, 
  isToday,
  parseISO,
  isSameDay,
  getHours,
  setHours,
  startOfMonth,
  endOfMonth,
  isSameMonth
} from 'date-fns';
import { 
  ChevronLeft, 
  ChevronRight, 
  Calendar as CalendarIcon,
  Plus,
  Clock,
  ArrowRight
} from 'lucide-react';
import { motion as Motion, AnimatePresence } from 'framer-motion';
import { useCalendarStore } from '@/stores/useCalendarStore';
import { useLanguage } from '@/context/LanguageContext';
import Button from '@/components/ui/Button';
import CalendarTaskModal from '@/components/tasks/CalendarTaskModal';
import TaskDetailDrawer from '@/components/tasks/TaskDetailDrawer';

const HOURS = Array.from({ length: 24 }, (_, i) => i);

const CalendarPage = () => {
  const { t } = useLanguage();
  const { 
    taskPage, loadError,
    currentDate, 
    tasks, 
    isLoading, 
    nextWeek, 
    prevWeek, 
    setToday,
    fetchCalendarTasks,
    selectedDate,
    setSelectedDate,
    selectedTask,
    setSelectedTask,
    isTaskDrawerOpen,
    setTaskDrawerOpen,
    isAddTaskModalOpen,
    setAddTaskModalOpen
  } = useCalendarStore();

  const [view, setView] = React.useState('week'); // 'week' | 'month'

  const scrollRef = useRef(null);

  useEffect(() => {
    fetchCalendarTasks();
  }, [currentDate, fetchCalendarTasks]);

  // Scroll to 8 AM by default
  useEffect(() => {
    if (scrollRef.current) {
      // approximate 8 hours * 80px (cell height)
      scrollRef.current.scrollTop = 8 * 80;
    }
  }, []);

  const weekDays = useMemo(() => {
    const startDate = startOfWeek(currentDate);
    const days = [];
    let day = startDate;

    for (let i = 0; i < 7; i++) {
      days.push(day);
      day = addDays(day, 1);
    }
    return days;
  }, [currentDate]);

  const monthDays = useMemo(() => {
    const monthStart = startOfMonth(currentDate);
    const monthEnd = endOfMonth(monthStart);
    const startDate = startOfWeek(monthStart);
    const endDate = endOfWeek(monthEnd);

    const days = [];
    let day = startDate;

    while (day <= endDate) {
      days.push(day);
      day = addDays(day, 1);
    }
    return days;
  }, [currentDate]);

  const getPriorityColor = (priority) => {
    switch (priority) {
      case 'Critical': return '#ef4444';
      case 'High': return '#f97316';
      case 'Medium': return '#f59e0b';
      case 'Low': return '#10b981';
      default: return '#64748b';
    }
  };

  const handleTimeSlotClick = (day, hour) => {
    const clickedDate = setHours(new Date(day), hour);
    setSelectedDate(clickedDate);
    setAddTaskModalOpen(true);
  };

  const renderTask = (task) => {
    const taskDate = task.startDate ? parseISO(task.startDate) : (task.dueDate ? parseISO(task.dueDate) : null);
    if (!taskDate) return null;
    
    // Simplistic rendering for weekly view
    return (
      <Motion.div
        layoutId={task.id}
        key={task.id}
        onClick={(e) => {
          e.stopPropagation();
          setSelectedTask(task);
        }}
        className="absolute left-1 right-1 px-2 py-1.5 rounded-lg text-xs font-bold border-l-2 shadow-sm cursor-pointer hover:scale-[1.02] active:scale-[0.98] transition-all flex flex-col overflow-hidden z-10"
        style={{ 
          backgroundColor: `${task.color || '#6366f1'}E6`, 
          color: '#fff',
          borderLeftColor: getPriorityColor(task.priority),
          top: '4px',
          bottom: '4px'
        }}
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
      >
        <span className="truncate">{task.title}</span>
        <span className="text-[10px] opacity-80">{format(taskDate, 'HH:mm')}</span>
      </Motion.div>
    );
  };

  return (
    <div className="h-full flex flex-col space-y-6 animate-in fade-in duration-700 pb-8">
      {/* Premium Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div className="flex items-center gap-4">
          <div className="w-14 h-14 bg-primary/10 rounded-2xl flex items-center justify-center text-primary shadow-inner border border-primary/10">
            <CalendarIcon size={28} />
          </div>
          <div>
            <h1 className="text-3xl font-black text-text-main tracking-tight">
              {t('calendar.title')}
            </h1>
            <p className="text-sm font-bold text-text-subtle uppercase tracking-widest flex items-center gap-2">
              <Clock size={14} /> {format(weekDays[0], 'MMMM yyyy')}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3 bg-surface-0 p-2 rounded-2xl shadow-sm border border-border-subtle transition-colors">
          <div className="flex items-center bg-surface-2 p-1 rounded-xl border border-border-subtle mr-2">
            <button 
              onClick={() => setView('week')}
              className={`px-3 py-1 text-sm font-bold rounded-lg transition-all ${view === 'week' ? 'bg-primary text-white shadow-md' : 'text-text-muted hover:text-text-main'}`}
            >
              Week
            </button>
            <button 
              onClick={() => setView('month')}
              className={`px-3 py-1 text-sm font-bold rounded-lg transition-all ${view === 'month' ? 'bg-primary text-white shadow-md' : 'text-text-muted hover:text-text-main'}`}
            >
              Month
            </button>
          </div>
          <div className="h-8 w-[1px] bg-border-subtle mx-1" />
          
          <div className="flex items-center bg-surface-2 p-1 rounded-xl border border-border-subtle">
            <button 
              onClick={prevWeek}
              className="p-2 hover:bg-hover-bg hover:shadow-sm rounded-lg transition-all text-text-muted hover:text-text-main"
            >
              <ChevronLeft size={18} />
            </button>
            <div className="px-4 text-sm font-black text-text-main min-w-[180px] text-center">
              {format(weekDays[0], 'MMM d')} - {format(weekDays[6], 'MMM d, yyyy')}
            </div>
            <button 
              onClick={nextWeek}
              className="p-2 hover:bg-hover-bg hover:shadow-sm rounded-lg transition-all text-text-muted hover:text-text-main"
            >
              <ChevronRight size={18} />
            </button>
          </div>
          
          <div className="h-8 w-[1px] bg-border-subtle mx-1" />
          
          <Button 
            variant="outline" 
            size="sm" 
            className="font-black"
            onClick={setToday}
          >
            {t('calendar.today')}
          </Button>
          
          <Button 
            size="sm" 
            leftIcon={<Plus size={18} />}
            className="shadow-lg shadow-primary/20"
            onClick={() => {
              setSelectedDate(new Date());
              setAddTaskModalOpen(true);
            }}
          >
            {t('calendar.addTask')}
          </Button>
        </div>
      </div>

      {/* Calendar Grid Wrapper */}
      <div className="flex-1 bg-surface-0 rounded-[32px] shadow-2xl border border-border-subtle flex flex-col overflow-hidden transition-colors relative">
        {isLoading && (
          <div className="absolute inset-0 z-50 bg-surface-0/40 backdrop-blur-[1px] flex items-center justify-center rounded-[32px]">
            <div className="w-10 h-10 border-4 border-primary border-t-transparent rounded-full animate-spin shadow-xl" />
          </div>
        )}

        {/* Week Days Header */}
        <div className="flex border-b border-border-subtle bg-surface-0 z-20">
          <div className="w-20 shrink-0 border-r border-border-subtle flex items-end justify-center pb-2 text-[10px] font-bold text-text-subtle">
            GMT+07
          </div>
          <div className="flex-1 grid grid-cols-7">
            {weekDays.map(day => {
              const isTodayDate = isToday(day);
              return (
                <div key={day.toISOString()} className="text-center py-4 border-r border-border-subtle">
                  <div className="text-[11px] font-black text-text-subtle uppercase tracking-widest mb-1">
                    {format(day, 'EEE')}
                  </div>
                  <div className={`
                    inline-flex items-center justify-center w-10 h-10 rounded-full text-xl font-black transition-all
                    ${isTodayDate ? 'bg-primary text-white shadow-lg shadow-primary/30' : 'text-text-main'}
                  `}>
                    {format(day, 'd')}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Time Slots Grid or Month Grid */}
        <div className="flex-1 overflow-y-auto custom-scrollbar" ref={scrollRef}>
          {view === 'week' ? (
            <div className="flex min-h-[1440px]"> {/* 24 hours * 60px */}
              {/* Time Labels */}
              <div className="w-20 shrink-0 border-r border-border-subtle bg-surface-0 z-10">
                {HOURS.map(hour => (
                  <div key={hour} className="h-20 border-b border-border-subtle relative">
                    <span className="absolute -top-2.5 right-4 text-[11px] font-bold text-text-subtle">
                      {hour === 0 ? '' : format(setHours(new Date(), hour), 'h a')}
                    </span>
                  </div>
                ))}
              </div>

              {/* Days Grid */}
              <div className="flex-1 grid grid-cols-7 relative">
                {weekDays.map(day => (
                  <div key={day.toISOString()} className="border-r border-border-subtle relative">
                    {HOURS.map(hour => {
                      // Find tasks for this specific hour slot
                      const slotTasks = tasks.filter(task => {
                        const taskDate = task.startDate ? parseISO(task.startDate) : (task.dueDate ? parseISO(task.dueDate) : null);
                        if (!taskDate) return false;
                        return isSameDay(taskDate, day) && getHours(taskDate) === hour;
                      });

                      return (
                        <div 
                          key={`${day.toISOString()}-${hour}`} 
                          className="h-20 border-b border-border-subtle group cursor-pointer hover:bg-hover-bg transition-colors relative"
                          onClick={() => handleTimeSlotClick(day, hour)}
                        >
                          {slotTasks.map(renderTask)}
                        </div>
                      );
                    })}
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-7 auto-rows-[minmax(120px,1fr)] h-full">
              {monthDays.map(day => {
                const dayTasks = tasks.filter(task => {
                  const taskDate = task.startDate ? parseISO(task.startDate) : (task.dueDate ? parseISO(task.dueDate) : null);
                  return taskDate && isSameDay(taskDate, day);
                });
                
                const isCurrentMonth = isSameMonth(day, currentDate);
                
                return (
                  <div 
                    key={day.toISOString()} 
                    className={`border-r border-b border-border-subtle p-2 cursor-pointer transition-colors hover:bg-hover-bg
                      ${!isCurrentMonth ? 'bg-surface-2/50' : ''}`}
                    onClick={() => {
                      setSelectedDate(day);
                      setAddTaskModalOpen(true);
                    }}
                  >
                    <div className="flex justify-between items-center mb-2">
                      <span className={`text-sm font-bold w-7 h-7 flex items-center justify-center rounded-full
                        ${isToday(day) ? 'bg-primary text-white' : !isCurrentMonth ? 'text-text-subtle' : 'text-text-main'}
                      `}>
                        {format(day, 'd')}
                      </span>
                    </div>
                    <div className="space-y-1">
                      {dayTasks.map(task => (
                        <div 
                          key={task.id}
                          onClick={(e) => {
                            e.stopPropagation();
                            setSelectedTask(task);
                          }}
                          className="px-2 py-1 rounded text-xs font-semibold truncate hover:opacity-80 transition-opacity"
                          style={{ 
                            backgroundColor: `${task.color || '#6366f1'}20`, 
                            color: task.color || '#6366f1',
                            borderLeft: `2px solid ${getPriorityColor(task.priority)}`
                          }}
                        >
                          {task.title}
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      <div className="flex items-center gap-3 text-text-muted" aria-live="polite">
        {loadError && <span role="alert">{loadError}</span>}
        {taskPage && <span>Showing {tasks.length} of {taskPage.totalItems} tasks</span>}
        {taskPage?.page < taskPage?.totalPages && <Button disabled={isLoading} onClick={() => fetchCalendarTasks(taskPage.page + 1)}>Load more tasks</Button>}
        {loadError && <Button onClick={() => fetchCalendarTasks()}>Retry</Button>}
      </div>
      {/* Modals & Drawers */}
      <CalendarTaskModal 
        isOpen={isAddTaskModalOpen}
        onClose={() => setAddTaskModalOpen(false)}
        initialDate={selectedDate}
        onSuccess={fetchCalendarTasks}
      />

      <TaskDetailDrawer 
        isOpen={isTaskDrawerOpen}
        onClose={() => setTaskDrawerOpen(false)}
        task={selectedTask}
      />
    </div>
  );
};

export default CalendarPage;

