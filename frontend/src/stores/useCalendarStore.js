import { create } from 'zustand';
import { 
  startOfMonth, 
  endOfMonth, 
  startOfWeek, 
  endOfWeek,
  addWeeks,
  subWeeks,
} from 'date-fns';
import { calendarApi } from '@/api/calendarApi';
import { toast } from 'react-hot-toast';

export const useCalendarStore = create((set, get) => ({
  currentDate: new Date(),
  tasks: [],
  taskPage: null,
  loadError: null,
  requestVersion: 0,
  reset: () => set(state => ({
    requestVersion: state.requestVersion + 1, currentDate: new Date(),
    tasks: [], taskPage: null, loadError: null, isLoading: false,
    selectedDate: null, selectedTask: null, isTaskDrawerOpen: false, isAddTaskModalOpen: false,
  })),
  isLoading: false,
  selectedDate: null,
  selectedTask: null,
  isTaskDrawerOpen: false,
  isAddTaskModalOpen: false,

  setCurrentDate: (date) => set({ currentDate: date }),
  
  nextWeek: () => set((state) => ({ currentDate: addWeeks(state.currentDate, 1) })),
  prevWeek: () => set((state) => ({ currentDate: subWeeks(state.currentDate, 1) })),
  setToday: () => set({ currentDate: new Date() }),

  setSelectedDate: (date) => set({ selectedDate: date, isAddTaskModalOpen: !!date }),
  setSelectedTask: (task) => set({ selectedTask: task?.id || null, isTaskDrawerOpen: !!task }),
  
  setAddTaskModalOpen: (isOpen) => set({ isAddTaskModalOpen: isOpen }),
  setTaskDrawerOpen: (isOpen) => set({ isTaskDrawerOpen: isOpen, ...(!isOpen ? { selectedTask: null } : {}) }),

  fetchCalendarTasks: async (page = 1) => {
    const { currentDate } = get();
    const requestVersion = get().requestVersion + 1;
    const isCurrent = () => get().requestVersion === requestVersion && get().currentDate === currentDate;
    if (typeof page !== 'number') page = 1;
    set({ requestVersion, isLoading: true, loadError: null, ...(page === 1 ? { tasks: [], taskPage: null } : {}) });
    try {
      const start = startOfWeek(startOfMonth(currentDate));
      const end = endOfWeek(endOfMonth(currentDate));
      const res = await calendarApi.getCalendarTasks(start, end, null, null, page);
      const result = res.data.data;
      if (!isCurrent()) return;
      set(state => ({ tasks: page === 1 ? result.items : [...new Map([...state.tasks, ...result.items].map(task => [task.id, task])).values()], taskPage: result }));
    } catch {
      if (!isCurrent()) return;
      set({ loadError: 'Failed to load tasks' });
      toast.error('Failed to load tasks');
    } finally {
      if (isCurrent()) set({ isLoading: false });
    }
  }
}));
