'use strict';

// ---------- DOM helpers ----------
const $ = (selector) => document.querySelector(selector);

const el = {
  sidebar: $('#sidebar'),
  scrim: $('#scrim'),
  menuBtn: $('#menuBtn'),
  sidebarClose: $('#sidebarClose'),
  nav: $('.nav'),
  categoryList: $('#categoryList'),
  categoryEmpty: $('#categoryEmpty'),
  countAll: $('#countAll'),
  countNone: $('#countNone'),
  addCategoryBtn: $('#addCategoryBtn'),
  apiStatus: $('#apiStatus'),
  viewTitle: $('#viewTitle'),
  viewSubtitle: $('#viewSubtitle'),
  refreshBtn: $('#refreshBtn'),
  themeBtn: $('#themeBtn'),
  newTaskBtn: $('#newTaskBtn'),
  statTotal: $('#statTotal'),
  statActive: $('#statActive'),
  statDone: $('#statDone'),
  statPct: $('#statPct'),
  statBar: $('#statBar'),
  searchInput: $('#searchInput'),
  statusFilter: $('#statusFilter'),
  priorityFilter: $('#priorityFilter'),
  sortSelect: $('#sortSelect'),
  taskList: $('#taskList'),
  emptyState: $('#emptyState'),
  emptyTitle: $('#emptyTitle'),
  emptyText: $('#emptyText'),
  emptyAction: $('#emptyAction'),
  taskDialog: $('#taskDialog'),
  taskDialogTitle: $('#taskDialogTitle'),
  taskForm: $('#taskForm'),
  taskTitle: $('#taskTitle'),
  taskTitleError: $('#taskTitleError'),
  taskCategory: $('#taskCategory'),
  taskCompleted: $('#taskCompleted'),
  taskFormError: $('#taskFormError'),
  taskSubmit: $('#taskSubmit'),
  categoryDialog: $('#categoryDialog'),
  categoryDialogTitle: $('#categoryDialogTitle'),
  categoryForm: $('#categoryForm'),
  categoryName: $('#categoryName'),
  categoryNameError: $('#categoryNameError'),
  categoryFormError: $('#categoryFormError'),
  categorySubmit: $('#categorySubmit'),
  confirmDialog: $('#confirmDialog'),
  confirmTitle: $('#confirmTitle'),
  confirmText: $('#confirmText'),
  confirmOk: $('#confirmOk'),
  toasts: $('#toasts'),
};

function esc(value) {
  return String(value ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

const icon = (name) => `<svg aria-hidden="true"><use href="#i-${name}"/></svg>`;

const plural = (n, word) => `${n} ${word}${n === 1 ? '' : 's'}`;

// ---------- API client ----------
class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.status = status;
  }
}

async function request(method, url, body) {
  let res;
  try {
    res = await fetch(url, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, 'Cannot reach the API. Make sure the server is running.');
  }
  if (!res.ok) throw new ApiError(res.status, await readError(res));
  return res.status === 204 ? null : res.json();
}

// The API returns either ProblemDetails JSON or a plain-text message.
async function readError(res) {
  if (res.status === 404) return 'That item no longer exists. It may have been deleted elsewhere.';
  const text = await res.text().catch(() => '');
  try {
    const data = JSON.parse(text);
    if (data.errors) return Object.values(data.errors).flat().join(' ');
    return data.detail || data.title || `Request failed (${res.status}).`;
  } catch {
    return text || `Request failed (${res.status}).`;
  }
}

const api = {
  listTasks: () => request('GET', 'api/tasks'),
  createTask: (body) => request('POST', 'api/tasks', body),
  updateTask: (id, body) => request('PUT', `api/tasks/${id}`, body),
  deleteTask: (id) => request('DELETE', `api/tasks/${id}`),
  listCategories: () => request('GET', 'api/categories'),
  createCategory: (body) => request('POST', 'api/categories', body),
  updateCategory: (id, body) => request('PUT', `api/categories/${id}`, body),
  deleteCategory: (id) => request('DELETE', `api/categories/${id}`),
};

// ---------- State ----------
const PRIORITY_RANK = { High: 0, Medium: 1, Low: 2 };

const state = {
  tasks: [],
  categories: [],
  loaded: false,
  loadError: null,
  view: 'all', // 'all' | 'none' | category id
  status: 'all', // 'all' | 'active' | 'done'
  priority: 'all',
  search: '',
  sort: 'newest',
  pending: new Set(), // task ids with a request in flight
};

const findTask = (id) => state.tasks.find((t) => t.id === id);
const findCategory = (id) => state.categories.find((c) => c.id === id);

function replaceTask(task) {
  const i = state.tasks.findIndex((t) => t.id === task.id);
  if (i >= 0) state.tasks[i] = task;
}

const toRequest = (t) => ({
  title: t.title,
  isCompleted: t.isCompleted,
  priority: t.priority,
  categoryId: t.categoryId ?? null,
});

// Tasks in the selected sidebar view, before toolbar filters.
function viewTasks() {
  if (state.view === 'all') return state.tasks;
  if (state.view === 'none') return state.tasks.filter((t) => t.categoryId == null);
  return state.tasks.filter((t) => t.categoryId === state.view);
}

function visibleTasks() {
  const q = state.search.trim().toLowerCase();
  const list = viewTasks().filter((t) =>
    (state.status === 'all' || (state.status === 'done') === t.isCompleted) &&
    (state.priority === 'all' || t.priority === state.priority) &&
    (!q || t.title.toLowerCase().includes(q) || (t.categoryName ?? '').toLowerCase().includes(q)));

  const sorters = {
    newest: (a, b) => b.id - a.id,
    oldest: (a, b) => a.id - b.id,
    priority: (a, b) => PRIORITY_RANK[a.priority] - PRIORITY_RANK[b.priority] || b.id - a.id,
    title: (a, b) => a.title.localeCompare(b.title, undefined, { sensitivity: 'base' }) || a.id - b.id,
  };
  return list.sort(sorters[state.sort]);
}

const hasFilters = () => state.status !== 'all' || state.priority !== 'all' || state.search.trim() !== '';

// ---------- Loading ----------
async function load({ quiet = false } = {}) {
  setApiStatus('loading');
  el.refreshBtn.classList.add('spinning');
  el.refreshBtn.disabled = true;
  if (!state.loaded) renderSkeleton();

  try {
    const [tasks, categories] = await Promise.all([api.listTasks(), api.listCategories()]);
    state.tasks = tasks;
    state.categories = categories;
    state.loaded = true;
    state.loadError = null;
    if (typeof state.view === 'number' && !findCategory(state.view)) state.view = 'all';
    setApiStatus('ok');
  } catch (err) {
    state.loadError = err.message;
    setApiStatus('error');
    if (!quiet && state.loaded) toast(err.message, 'error');
  } finally {
    el.refreshBtn.classList.remove('spinning');
    el.refreshBtn.disabled = false;
  }
  render();
}

function setApiStatus(status) {
  el.apiStatus.dataset.state = status;
  el.apiStatus.querySelector('.api-status-text').textContent =
    { loading: 'Connecting…', ok: 'API connected', error: 'API offline' }[status];
}

// ---------- Rendering ----------
function render() {
  renderNav();
  renderHeader();
  renderStats();
  renderTasks();
}

function renderNav() {
  el.countAll.textContent = state.tasks.length;
  el.countNone.textContent = state.tasks.filter((t) => t.categoryId == null).length;

  el.categoryList.innerHTML = state.categories.map((c) => {
    const count = state.tasks.filter((t) => t.categoryId === c.id).length;
    return `
      <li class="cat-row">
        <button class="nav-item" type="button" data-view="${c.id}">
          ${icon('folder')}<span class="nav-text">${esc(c.name)}</span><span class="nav-count">${count}</span>
        </button>
        <div class="cat-actions">
          <button class="icon-btn icon-btn-sm" type="button" data-cat-action="rename" data-id="${c.id}" aria-label="Rename ${esc(c.name)}" title="Rename">${icon('edit')}</button>
          <button class="icon-btn icon-btn-sm danger" type="button" data-cat-action="delete" data-id="${c.id}" aria-label="Delete ${esc(c.name)}" title="Delete">${icon('trash')}</button>
        </div>
      </li>`;
  }).join('');

  el.categoryEmpty.hidden = !state.loaded || state.categories.length > 0;

  for (const item of el.nav.querySelectorAll('.nav-item')) {
    item.setAttribute('aria-current', String(item.dataset.view === String(state.view)));
  }
}

function renderHeader() {
  let title = 'All tasks';
  if (state.view === 'none') title = 'Uncategorized';
  else if (typeof state.view === 'number') title = findCategory(state.view)?.name ?? 'Category';

  const tasks = viewTasks();
  const done = tasks.filter((t) => t.isCompleted).length;
  el.viewTitle.textContent = title;
  el.viewSubtitle.textContent = state.loaded
    ? `${plural(tasks.length, 'task')} · ${done} completed`
    : 'Loading your tasks…';
  document.title = `${title} · Task Manager`;
}

function renderStats() {
  const tasks = viewTasks();
  const done = tasks.filter((t) => t.isCompleted).length;
  const pct = tasks.length ? Math.round((done / tasks.length) * 100) : 0;
  el.statTotal.textContent = tasks.length;
  el.statActive.textContent = tasks.length - done;
  el.statDone.textContent = done;
  el.statPct.textContent = `${pct}%`;
  el.statBar.style.width = `${pct}%`;
}

function renderSkeleton() {
  el.emptyState.hidden = true;
  el.taskList.innerHTML = Array.from({ length: 4 }, (_, i) => `
    <li class="skeleton" aria-hidden="true">
      <i style="width:22px;height:22px;border-radius:50%"></i>
      <span style="flex:1;display:flex;flex-direction:column;gap:8px">
        <i style="width:${[55, 40, 65, 35][i]}%;height:12px"></i>
        <i style="width:120px;height:10px"></i>
      </span>
    </li>`).join('');
}

let emptyActionHandler = null;

function showEmpty(title, text, actionLabel, onAction) {
  el.taskList.innerHTML = '';
  el.emptyState.hidden = false;
  el.emptyTitle.textContent = title;
  el.emptyText.textContent = text;
  el.emptyAction.textContent = actionLabel;
  emptyActionHandler = onAction;
}

function renderTasks() {
  if (!state.loaded) {
    if (state.loadError) showEmpty("Can't reach the API", state.loadError, 'Try again', () => load());
    return;
  }

  const inView = viewTasks();
  const tasks = visibleTasks();

  if (inView.length === 0) {
    const text = state.view === 'all'
      ? 'Create your first task to get started.'
      : state.view === 'none'
        ? 'Every task belongs to a category.'
        : 'This category has no tasks yet.';
    return showEmpty('No tasks here', text, 'New task', () => openTaskDialog());
  }
  if (tasks.length === 0) {
    return showEmpty('No matching tasks', 'Try a different search term or filter.', 'Clear filters', clearFilters);
  }

  el.emptyState.hidden = true;
  el.taskList.innerHTML = tasks.map(taskHtml).join('');
}

function taskHtml(t) {
  const busy = state.pending.has(t.id);
  const category = t.categoryName
    ? `<span class="badge badge-cat" title="${esc(t.categoryName)}">${icon('folder')}<span>${esc(t.categoryName)}</span></span>`
    : '<span class="badge badge-none">No category</span>';
  return `
    <li class="task${t.isCompleted ? ' is-done' : ''}" data-id="${t.id}">
      <button class="check" type="button" role="checkbox" data-action="toggle" aria-checked="${t.isCompleted}"
        aria-label="${t.isCompleted ? 'Mark as not completed' : 'Mark as completed'}: ${esc(t.title)}" ${busy ? 'disabled' : ''}>${icon('check')}</button>
      <div class="task-body">
        <p class="task-title">${esc(t.title)}</p>
        <div class="task-meta">
          <span class="badge badge-${t.priority}">${icon('flag')}${t.priority}</span>
          ${category}
          <span class="task-id">#${t.id}</span>
        </div>
      </div>
      <div class="task-actions">
        <button class="icon-btn" type="button" data-action="edit" aria-label="Edit ${esc(t.title)}" title="Edit" ${busy ? 'disabled' : ''}>${icon('edit')}</button>
        <button class="icon-btn danger" type="button" data-action="delete" aria-label="Delete ${esc(t.title)}" title="Delete" ${busy ? 'disabled' : ''}>${icon('trash')}</button>
      </div>
    </li>`;
}

// ---------- Toasts ----------
function toast(message, type = 'success') {
  const node = document.createElement('div');
  node.className = `toast ${type}`;
  node.setAttribute('role', type === 'error' ? 'alert' : 'status');
  node.innerHTML = `<p></p><button class="icon-btn icon-btn-sm" type="button" aria-label="Dismiss">${icon('x')}</button>`;
  node.querySelector('p').textContent = message;

  const dismiss = () => {
    if (node.classList.contains('leaving')) return;
    node.classList.add('leaving');
    node.addEventListener('animationend', () => node.remove(), { once: true });
  };
  node.querySelector('button').addEventListener('click', dismiss);
  setTimeout(dismiss, type === 'error' ? 6000 : 3500);

  el.toasts.append(node);
  while (el.toasts.children.length > 4) el.toasts.firstElementChild.remove();
}

function handleError(err) {
  toast(err.message, 'error');
  // Our copy is stale if something vanished on the server; resync.
  if (err.status === 404) load({ quiet: true });
}

function setBusy(button, busy, busyLabel) {
  if (busy) {
    button.dataset.label = button.textContent;
    button.textContent = busyLabel;
    button.disabled = true;
  } else {
    if (button.dataset.label) button.textContent = button.dataset.label;
    button.disabled = false;
  }
}

// ---------- Task actions ----------
async function toggleTask(id) {
  const task = findTask(id);
  if (!task || state.pending.has(id)) return;

  const updated = { ...task, isCompleted: !task.isCompleted };
  state.pending.add(id);
  replaceTask(updated); // optimistic
  render();

  try {
    const saved = await api.updateTask(id, toRequest(updated));
    replaceTask(saved);
    toast(saved.isCompleted
      ? `Completed “${saved.title}”. Email notification sent.`
      : `“${saved.title}” is active again.`);
  } catch (err) {
    replaceTask(task);
    handleError(err);
  } finally {
    state.pending.delete(id);
    render();
  }
}

let editingTaskId = null;

function openTaskDialog(task = null) {
  editingTaskId = task ? task.id : null;
  el.taskDialogTitle.textContent = task ? 'Edit task' : 'New task';
  el.taskSubmit.textContent = task ? 'Save changes' : 'Create task';

  el.taskCategory.innerHTML = '<option value="">No category</option>' +
    state.categories.map((c) => `<option value="${c.id}">${esc(c.name)}</option>`).join('');

  const defaultCategory = typeof state.view === 'number' ? state.view : '';
  el.taskTitle.value = task?.title ?? '';
  el.taskForm.elements.priority.value = task?.priority ?? 'Low';
  el.taskCategory.value = String(task ? task.categoryId ?? '' : defaultCategory);
  el.taskCompleted.checked = task?.isCompleted ?? false;

  setFieldError(el.taskTitle, el.taskTitleError, false);
  el.taskFormError.hidden = true;

  el.taskDialog.showModal();
  el.taskTitle.focus();
  el.taskTitle.select();
}

async function submitTask(event) {
  event.preventDefault();
  const title = el.taskTitle.value.trim();
  if (!title) {
    setFieldError(el.taskTitle, el.taskTitleError, true);
    el.taskTitle.focus();
    return;
  }

  const body = {
    title,
    priority: el.taskForm.elements.priority.value,
    categoryId: el.taskCategory.value ? Number(el.taskCategory.value) : null,
    isCompleted: el.taskCompleted.checked,
  };

  el.taskFormError.hidden = true;
  setBusy(el.taskSubmit, true, 'Saving…');
  try {
    if (editingTaskId == null) {
      const created = await api.createTask(body);
      state.tasks.push(created);
      toast(`Created “${created.title}”.`);
    } else {
      const before = findTask(editingTaskId);
      const saved = await api.updateTask(editingTaskId, body);
      replaceTask(saved);
      const justCompleted = before && !before.isCompleted && saved.isCompleted;
      toast(justCompleted ? 'Task completed. Email notification sent.' : 'Task updated.');
    }
    el.taskDialog.close();
    render();
  } catch (err) {
    if (err.status === 404) {
      el.taskDialog.close();
      handleError(err);
    } else {
      el.taskFormError.textContent = err.message;
      el.taskFormError.hidden = false;
    }
  } finally {
    setBusy(el.taskSubmit, false);
  }
}

function deleteTask(id) {
  const task = findTask(id);
  if (!task) return;
  confirmDelete({
    title: 'Delete task?',
    message: `<strong>${esc(task.title)}</strong> will be permanently deleted.`,
    onConfirm: async () => {
      await api.deleteTask(id);
      state.tasks = state.tasks.filter((t) => t.id !== id);
      render();
      toast('Task deleted.');
    },
  });
}

// ---------- Category actions ----------
let editingCategoryId = null;

function openCategoryDialog(category = null) {
  editingCategoryId = category ? category.id : null;
  el.categoryDialogTitle.textContent = category ? 'Rename category' : 'New category';
  el.categorySubmit.textContent = category ? 'Save' : 'Create category';
  el.categoryName.value = category?.name ?? '';
  setFieldError(el.categoryName, el.categoryNameError, false);
  el.categoryFormError.hidden = true;
  el.categoryDialog.showModal();
  el.categoryName.focus();
  el.categoryName.select();
}

async function submitCategory(event) {
  event.preventDefault();
  const name = el.categoryName.value.trim();
  if (!name) {
    setFieldError(el.categoryName, el.categoryNameError, true);
    el.categoryName.focus();
    return;
  }

  el.categoryFormError.hidden = true;
  setBusy(el.categorySubmit, true, 'Saving…');
  try {
    if (editingCategoryId == null) {
      const created = await api.createCategory({ name });
      state.categories.push(created);
      toast(`Created category “${created.name}”.`);
    } else {
      const saved = await api.updateCategory(editingCategoryId, { name });
      const category = findCategory(saved.id);
      if (category) category.name = saved.name;
      for (const t of state.tasks) if (t.categoryId === saved.id) t.categoryName = saved.name;
      toast('Category renamed.');
    }
    el.categoryDialog.close();
    render();
  } catch (err) {
    if (err.status === 404) {
      el.categoryDialog.close();
      handleError(err);
    } else {
      el.categoryFormError.textContent = err.message;
      el.categoryFormError.hidden = false;
    }
  } finally {
    setBusy(el.categorySubmit, false);
  }
}

function deleteCategory(id) {
  const category = findCategory(id);
  if (!category) return;
  const count = state.tasks.filter((t) => t.categoryId === id).length;
  const note = count
    ? ` Its ${plural(count, 'task')} will be kept and moved to Uncategorized.`
    : '';
  confirmDelete({
    title: 'Delete category?',
    message: `The category <strong>${esc(category.name)}</strong> will be deleted.${note}`,
    onConfirm: async () => {
      await api.deleteCategory(id);
      state.categories = state.categories.filter((c) => c.id !== id);
      for (const t of state.tasks) {
        if (t.categoryId === id) {
          t.categoryId = null;
          t.categoryName = null;
        }
      }
      if (state.view === id) state.view = 'all';
      render();
      toast('Category deleted.');
    },
  });
}

// ---------- Shared dialog helpers ----------
function setFieldError(input, message, show) {
  message.hidden = !show;
  input.setAttribute('aria-invalid', String(show));
}

let confirmHandler = null;

function confirmDelete({ title, message, onConfirm }) {
  el.confirmTitle.textContent = title;
  el.confirmText.innerHTML = message;
  confirmHandler = onConfirm;
  el.confirmDialog.showModal();
}

el.confirmOk.addEventListener('click', async () => {
  if (!confirmHandler) return;
  setBusy(el.confirmOk, true, 'Deleting…');
  try {
    await confirmHandler();
    el.confirmDialog.close();
  } catch (err) {
    el.confirmDialog.close();
    handleError(err);
  } finally {
    setBusy(el.confirmOk, false);
  }
});

el.confirmDialog.addEventListener('close', () => { confirmHandler = null; });

for (const dialog of document.querySelectorAll('dialog')) {
  // Close buttons and clicks on the backdrop.
  dialog.addEventListener('click', (e) => {
    if (e.target === dialog || e.target.closest('[data-close]')) dialog.close();
  });
}

// ---------- Filters ----------
function clearFilters() {
  state.status = 'all';
  state.priority = 'all';
  state.search = '';
  el.searchInput.value = '';
  el.priorityFilter.value = 'all';
  syncStatusButtons();
  render();
}

function syncStatusButtons() {
  for (const b of el.statusFilter.querySelectorAll('button')) {
    b.setAttribute('aria-pressed', String(b.dataset.status === state.status));
  }
}

function setView(view) {
  state.view = view;
  closeSidebar();
  render();
}

// ---------- Sidebar (mobile) ----------
function openSidebar() {
  el.sidebar.classList.add('open');
  el.scrim.hidden = false;
}

function closeSidebar() {
  el.sidebar.classList.remove('open');
  el.scrim.hidden = true;
}

// ---------- Theme ----------
function toggleTheme() {
  const current = document.documentElement.dataset.theme ||
    (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  const next = current === 'dark' ? 'light' : 'dark';
  document.documentElement.dataset.theme = next;
  try { localStorage.setItem('tm-theme', next); } catch { /* storage unavailable */ }
}

// ---------- Events ----------
el.nav.addEventListener('click', (e) => {
  const action = e.target.closest('[data-cat-action]');
  if (action) {
    const id = Number(action.dataset.id);
    if (action.dataset.catAction === 'rename') openCategoryDialog(findCategory(id));
    else deleteCategory(id);
    return;
  }
  const item = e.target.closest('.nav-item');
  if (item) {
    const v = item.dataset.view;
    setView(v === 'all' || v === 'none' ? v : Number(v));
  }
});

el.taskList.addEventListener('click', (e) => {
  const button = e.target.closest('[data-action]');
  if (!button) return;
  const id = Number(button.closest('.task').dataset.id);
  if (button.dataset.action === 'toggle') toggleTask(id);
  else if (button.dataset.action === 'edit') openTaskDialog(findTask(id));
  else if (button.dataset.action === 'delete') deleteTask(id);
});

el.newTaskBtn.addEventListener('click', () => openTaskDialog());
el.addCategoryBtn.addEventListener('click', () => openCategoryDialog());
el.refreshBtn.addEventListener('click', () => load());
el.themeBtn.addEventListener('click', toggleTheme);
el.emptyAction.addEventListener('click', () => emptyActionHandler?.());
el.taskForm.addEventListener('submit', submitTask);
el.categoryForm.addEventListener('submit', submitCategory);

el.taskTitle.addEventListener('input', () => {
  if (el.taskTitle.value.trim()) setFieldError(el.taskTitle, el.taskTitleError, false);
});
el.categoryName.addEventListener('input', () => {
  if (el.categoryName.value.trim()) setFieldError(el.categoryName, el.categoryNameError, false);
});

el.searchInput.addEventListener('input', () => {
  state.search = el.searchInput.value;
  renderTasks();
});
el.statusFilter.addEventListener('click', (e) => {
  const b = e.target.closest('button');
  if (!b) return;
  state.status = b.dataset.status;
  syncStatusButtons();
  renderTasks();
});
el.priorityFilter.addEventListener('change', () => {
  state.priority = el.priorityFilter.value;
  renderTasks();
});
el.sortSelect.addEventListener('change', () => {
  state.sort = el.sortSelect.value;
  renderTasks();
});

el.menuBtn.addEventListener('click', openSidebar);
el.sidebarClose.addEventListener('click', closeSidebar);
el.scrim.addEventListener('click', closeSidebar);

// Shortcuts: "n" new task, "/" search, Esc closes the mobile menu.
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') closeSidebar();
  const typing = e.target.closest('input, select, textarea') || document.querySelector('dialog[open]');
  if (typing || e.ctrlKey || e.metaKey || e.altKey) return;
  if (e.key === 'n' && state.loaded) {
    e.preventDefault();
    openTaskDialog();
  } else if (e.key === '/') {
    e.preventDefault();
    el.searchInput.focus();
  }
});

render();
load();
