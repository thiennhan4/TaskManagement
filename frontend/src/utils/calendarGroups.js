import { format, getHours, parseISO, isValid } from 'date-fns';

export function groupCalendarTasks(tasks) {
  const days = new Map();
  const hours = new Map();
  for (const task of tasks) {
    const value = task.startDate || task.dueDate;
    if (!value) continue;
    const date = parseISO(value);
    if (!isValid(date)) continue;
    const day = format(date, 'yyyy-MM-dd');
    const hour = day + ':' + getHours(date);
    if (!days.has(day)) days.set(day, []);
    if (!hours.has(hour)) hours.set(hour, []);
    days.get(day).push(task);
    hours.get(hour).push(task);
  }
  return { days, hours };
}
