import { create } from 'zustand';
import { 
  startOfMonth, 
  endOfMonth, 
  startOfWeek, 
  endOfWeek,
  addWeeks,
  subWeeks,
  addMonths,
  subMonths
} from 'date-fns';
import { calendarApi } from '@/api/calendarApi';
import { toast } from 'react-hot-toast';

export const useCalendarStore = create((set, get) => ({
  currentDate: new Date(),
  tasks: [],
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
  setSelectedTask: (task) => set({ selectedTask: task, isTaskDrawerOpen: !!task }),
  
  setAddTaskModalOpen: (isOpen) => set({ isAddTaskModalOpen: isOpen }),
  setTaskDrawerOpen: (isOpen) => set({ isTaskDrawerOpen: isOpen }),

  fetchCalendarTasks: async () => {
    const { currentDate } = get();
    set({ isLoading: true });
    try {
      const start = startOfWeek(startOfMonth(currentDate));
      const end = endOfWeek(endOfMonth(currentDate));
      const res = await calendarApi.getCalendarTasks(start, end);
      set({ tasks: res.data.data || [] });
    } catch (err) {
      toast.error('Failed to load tasks');
    } finally {
      set({ isLoading: false });
    }
  }
}));
