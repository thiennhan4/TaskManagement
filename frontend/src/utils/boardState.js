const order = (a, b) => (a.position || 0) - (b.position || 0) || String(a.id).localeCompare(String(b.id));
const same = (a, b) => Object.keys(b).every(key => a[key] === b[key]);
const taskMetadata = (column, delta) => {
  if (!column.taskPage || !delta) return column.taskPage;
  const totalItems = Math.max(0, column.taskPage.totalItems + delta);
  return { ...column.taskPage, totalItems, totalPages: Math.ceil(totalItems / column.taskPage.pageSize) };
};
export const pageMetadata = page => ({ page: page.page, pageSize: page.pageSize, totalItems: page.totalItems, totalPages: page.totalPages });
export const adaptColumns = items => items.map(({ tasks, ...column }) => ({
  ...column, tasks: tasks.items, taskPage: pageMetadata(tasks),
}));
export function upsertColumns(columns, incoming) {
  let result = columns;
  for (const column of incoming) {
    const existing = result.find(item => item.id === column.id);
    const patch = existing ? { ...column, tasks: existing.tasks, taskPage: existing.taskPage } : { ...column, tasks: column.tasks || [] };
    if (existing && same(existing, patch)) continue;
    result = existing ? result.map(item => item === existing ? { ...item, ...patch } : item) : [...result, patch];
  }
  return result === columns ? columns : result.sort(order).slice(0, Math.max(20, columns.length));
}
export function upsertTask(columns, task) {
  let changed = false;
  const result = columns.map(column => {
    const existing = column.tasks.find(item => item.id === task.id);
    if (column.id === task.listId) {
      if (existing && same(existing, task)) return column;
      changed = true;
      const tasks = (existing
        ? column.tasks.map(item => item === existing ? { ...item, ...task } : item)
        : [...column.tasks, task]).sort(order);
      const capacity = Math.max(50, column.tasks.length);
      return { ...column, refreshRequired: tasks.length > capacity || column.refreshRequired,
        taskPage: taskMetadata(column, existing ? 0 : 1), tasks: tasks.slice(0, capacity) };
    }
    if (!existing) return column;
    changed = true;
    return { ...column, taskPage: taskMetadata(column, -1), tasks: column.tasks.filter(item => item.id !== task.id) };
  });
  return changed ? result : columns;
}
export function removeTask(columns, id) {
  return columns.map(column => column.tasks.some(task => task.id === id)
    ? { ...column, taskPage: taskMetadata(column, -1), tasks: column.tasks.filter(task => task.id !== id) } : column);
}
export function updateTaskCount(columns, { taskId, count }) {
  return columns.map(column => column.tasks.some(task => task.id === taskId && task.commentsCount !== count)
    ? { ...column, tasks: column.tasks.map(task => task.id === taskId ? { ...task, commentsCount: count } : task) } : column);
}
export function mergeBoardPage(columns, incoming, listId) {
  if (!listId) return [...new Map([...columns, ...incoming].map(column => [column.id, column])).values()];
  const next = incoming.find(column => column.id === listId);
  if (!next) return columns;
  return columns.map(column => column.id === listId ? { ...next,
    tasks: [...new Map([...column.tasks, ...next.tasks].map(task => [task.id, task])).values()] } : column);
}
