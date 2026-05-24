import React, { useEffect, useMemo, useRef } from 'react';
import { 
  format, 
  startOfWeek, 
  endOfWeek, 
  addDays, 
  isToday,
  parseISO,
  startOfDay,
  endOfDay,
  isSameDay,
  getHours,
  getMinutes,
  setHours
} from 'date-fns';
import { 
  ChevronLeft, 
  ChevronRight, 
  Calendar as CalendarIcon,
  Plus,
  Clock,
  ArrowRight
} from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { useCalendarStore } from '@/stores/useCalendarStore';
import { useLanguage } from '@/context/LanguageContext';
import Button from '@/components/ui/Button';
import CalendarTaskModal from '@/components/tasks/CalendarTaskModal';
import TaskDetailDrawer from '@/components/tasks/TaskDetailDrawer';

const HOURS = Array.from({ length: 24 }, (_, i) => i);

const CalendarPage = () => {
  const { t } = useLanguage();
  const { 
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

  const scrollRef = useRef(null);

  useEffect(() => {
    fetchCalendarTasks();
  }, [currentDate]);

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
      <motion.div
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
      </motion.div>
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
            <h1 className="text-3xl font-black text-slate-900 dark:text-white tracking-tight">
              {t('calendar.title')}
            </h1>
            <p className="text-sm font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest flex items-center gap-2">
              <Clock size={14} /> {format(weekDays[0], 'MMMM yyyy')}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3 bg-white dark:bg-slate-800 p-2 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-700 transition-colors">
          <div className="flex items-center bg-slate-50 dark:bg-slate-900 p-1 rounded-xl border border-slate-100 dark:border-slate-800">
            <button 
              onClick={prevWeek}
              className="p-2 hover:bg-white dark:hover:bg-slate-800 hover:shadow-sm rounded-lg transition-all text-slate-600 dark:text-slate-400"
            >
              <ChevronLeft size={18} />
            </button>
            <div className="px-4 text-sm font-black text-slate-800 dark:text-white min-w-[180px] text-center">
              {format(weekDays[0], 'MMM d')} - {format(weekDays[6], 'MMM d, yyyy')}
            </div>
            <button 
              onClick={nextWeek}
              className="p-2 hover:bg-white dark:hover:bg-slate-800 hover:shadow-sm rounded-lg transition-all text-slate-600 dark:text-slate-400"
            >
              <ChevronRight size={18} />
            </button>
          </div>
          
          <div className="h-8 w-[1px] bg-slate-100 dark:bg-slate-700 mx-1" />
          
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
      <div className="flex-1 bg-white dark:bg-slate-900 rounded-[32px] shadow-2xl shadow-slate-200/50 dark:shadow-none border border-slate-100 dark:border-slate-800 flex flex-col overflow-hidden transition-colors relative">
        {isLoading && (
          <div className="absolute inset-0 z-50 bg-white/40 dark:bg-slate-900/40 backdrop-blur-[1px] flex items-center justify-center rounded-[32px]">
            <div className="w-10 h-10 border-4 border-primary border-t-transparent rounded-full animate-spin shadow-xl" />
          </div>
        )}

        {/* Week Days Header */}
        <div className="flex border-b border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-900 z-20">
          <div className="w-20 shrink-0 border-r border-slate-100 dark:border-slate-800 flex items-end justify-center pb-2 text-[10px] font-bold text-slate-400">
            GMT+07
          </div>
          <div className="flex-1 grid grid-cols-7">
            {weekDays.map(day => {
              const isTodayDate = isToday(day);
              return (
                <div key={day.toISOString()} className="text-center py-4 border-r border-slate-50 dark:border-slate-800/50">
                  <div className="text-[11px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest mb-1">
                    {format(day, 'EEE')}
                  </div>
                  <div className={`
                    inline-flex items-center justify-center w-10 h-10 rounded-full text-xl font-black transition-all
                    ${isTodayDate ? 'bg-primary text-white shadow-lg shadow-primary/30' : 'text-slate-700 dark:text-slate-200'}
                  `}>
                    {format(day, 'd')}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Time Slots Grid */}
        <div className="flex-1 overflow-y-auto custom-scrollbar" ref={scrollRef}>
          <div className="flex min-h-[1440px]"> {/* 24 hours * 60px */}
            {/* Time Labels */}
            <div className="w-20 shrink-0 border-r border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-900 z-10">
              {HOURS.map(hour => (
                <div key={hour} className="h-20 border-b border-slate-50 dark:border-slate-800/30 relative">
                  <span className="absolute -top-2.5 right-4 text-[11px] font-bold text-slate-400">
                    {hour === 0 ? '' : format(setHours(new Date(), hour), 'h a')}
                  </span>
                </div>
              ))}
            </div>

            {/* Days Grid */}
            <div className="flex-1 grid grid-cols-7 relative">
              {weekDays.map(day => (
                <div key={day.toISOString()} className="border-r border-slate-50 dark:border-slate-800/50 relative">
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
                        className="h-20 border-b border-slate-50 dark:border-slate-800/30 group cursor-pointer hover:bg-primary/5 transition-colors relative"
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
        </div>
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

