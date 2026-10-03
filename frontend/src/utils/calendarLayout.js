import { addDays, addMonths, addWeeks, eachDayOfInterval, endOfDay, endOfMonth, endOfWeek, format, isValid, parseISO, startOfDay, startOfMonth, startOfWeek } from 'date-fns';

// Preserve the existing parseISO contract: explicit offsets are instants; offsetless
// legacy DateTime values are local wall times. Never add an offset to stored data.
export const calendarDate = value => {
  const date = value ? parseISO(value) : null;
  return date && isValid(date) ? date : null;
};
export const calendarDefaultView = width => width < 640 ? 'day' : 'week';
export function calendarRange(date, view) {
  if (view === 'day') return { start: startOfDay(date), end: endOfDay(date) };
  if (view === 'month') return { start: startOfWeek(startOfMonth(date)), end: endOfWeek(endOfMonth(date)) };
  return { start: startOfWeek(date), end: endOfWeek(date) };
}
export const navigateCalendar = (date, view, amount) => (view === 'month' ? addMonths : view === 'day' ? addDays : addWeeks)(date, amount);
export function calendarLabel(date, view) {
  const range = calendarRange(date, view);
  return view === 'month' ? format(date, 'MMMM yyyy') : view === 'day' ? format(date, 'EEEE, MMM d, yyyy') : `${format(range.start, 'MMM d')} – ${format(range.end, 'MMM d, yyyy')}`;
}

// Split intervals at local midnight (calendar-day arithmetic also handles DST).
// Missing/equal end dates are deadline markers, displayed for 30 minutes.
export function layoutCalendarTasks(tasks, range) {
  const days = new Map(eachDayOfInterval(range).map(day => [format(day, 'yyyy-MM-dd'), []]));
  for (const task of tasks) {
    const start = calendarDate(task.startDate || task.dueDate);
    if (!start) continue;
    const due = calendarDate(task.dueDate);
    const end = due && due > start ? due : new Date(start.getTime() + 30 * 60_000);
    const clippedStart = new Date(Math.max(start.getTime(), range.start.getTime()));
    const clippedEnd = new Date(Math.min(end.getTime(), range.end.getTime() + 1));
    if (clippedStart >= clippedEnd) continue;
    for (let day = startOfDay(clippedStart); day < clippedEnd; day = addDays(day, 1)) {
      const segmentStart = new Date(Math.max(day.getTime(), clippedStart.getTime()));
      const nextDay = addDays(day, 1);
      const segmentEnd = new Date(Math.min(nextDay.getTime(), clippedEnd.getTime()));
      const minute = value => value.getHours() * 60 + value.getMinutes();
      const from = minute(segmentStart);
      const to = segmentEnd.getTime() === nextDay.getTime() ? 1440 : minute(segmentEnd);
      days.get(format(day, 'yyyy-MM-dd'))?.push({ task, start: segmentStart, end: segmentEnd, from, to: Math.max(from + 15, to), lane: 0, lanes: 1 });
    }
  }
  for (const events of days.values()) {
    events.sort((a, b) => a.from - b.from || b.to - a.to || String(a.task.id).localeCompare(String(b.task.id)));
    let group = [], laneEnds = [], groupEnd = -1;
    const finish = () => group.forEach(event => { event.lanes = laneEnds.length; });
    for (const event of events) {
      if (event.from >= groupEnd) { finish(); group = []; laneEnds = []; }
      let lane = laneEnds.findIndex(end => end <= event.from);
      if (lane < 0) lane = laneEnds.length;
      laneEnds[lane] = event.to; event.lane = lane; group.push(event);
      groupEnd = group.length === 1 ? event.to : Math.max(groupEnd, event.to);
    }
    finish();
  }
  return days;
}
