const state = {
	notes: [],
	query: ""
};

const SEARCH_KEY = 'notesapp.query';

const els = {
	container: document.querySelector('#notesContainer'),
	addBtn: document.querySelector('#addNoteBtn'),
	emptyAddBtn: document.querySelector('#emptyAddBtn'),
	searchInput: document.querySelector('#searchInput'),
	clearSearchBtn: document.querySelector('#clearSearchBtn'),
	searchFocusBtn: document.querySelector('#searchFocusBtn'),
	count: document.querySelector('#noteCount'),
	dialog: document.querySelector('#noteDialog'),
	form: document.querySelector('#noteForm'),
	closeDialog: document.querySelector('#closeDialog'),
	cancelDialog: document.querySelector('#cancelDialog'),
	titleInput: document.querySelector('#titleInput'),
	bodyInput: document.querySelector('#bodyInput'),
	tagsInput: document.querySelector('#tagsInput'),
	noteId: document.querySelector('#noteId'),
	dialogMode: document.querySelector('#dialogMode'),
	dialogTitle: document.querySelector('#dialogTitle'),
	saveBtn: document.querySelector('#saveNoteBtn')
};

async function apiGetNotes(query = "") {
	const res = await fetch(`/api/notes${query ? `?query=${encodeURIComponent(query)}` : ""}`);
	if (!res.ok) throw new Error('Failed to load notes');
	return await res.json();
}

async function apiCreateNote(note) {
	const res = await fetch('/api/notes', {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(note)
	});
	if (!res.ok) throw new Error('Failed to create note');
	return await res.json();
}

async function apiUpdateNote(id, note) {
	const res = await fetch(`/api/notes/${id}`, {
		method: 'PUT',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(note)
	});
	if (!res.ok) throw new Error('Failed to update note');
	return await res.json();
}

async function apiDeleteNote(id) {
	const res = await fetch(`/api/notes/${id}`, { method: 'DELETE' });
	if (!res.ok) throw new Error('Failed to delete note');
}

function formatDate(iso) {
	const date = new Date(iso);
	return date.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
}

function tagsToArray(text) {
	const seen = new Set();
	const list = [];
	text
		.split(',')
		.map(t => t.trim())
		.filter(Boolean)
		.forEach(t => {
			const key = t.toLowerCase();
			if (seen.has(key)) return;
			seen.add(key);
			list.push(t);
		});
	return list.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }));
}

function renderNotes() {
	const { notes } = state;
	els.container.innerHTML = '';

	if (!notes.length) {
		const empty = document.createElement('div');
		empty.className = 'empty';
		const searching = Boolean(state.query?.length);
		const title = searching ? 'No matches' : 'Start with a title and a thought.';
		const message = searching
			? `No notes matched "${escapeHtml(state.query)}". Try a different keyword or tag.`
			: 'Notes stay local. Create one to see it appear here.';
		empty.innerHTML = `
			<p class="eyebrow">${searching ? 'Empty search' : 'No notes yet'}</p>
			<h2>${title}</h2>
			<p>${message}</p>
			<button id="emptyAddDynamic" class="btn primary">Add your first note</button>
		`;
		els.container.appendChild(empty);
		empty.querySelector('#emptyAddDynamic')?.addEventListener('click', openCreateDialog);
		updateCount();
		return;
	}

	notes.forEach(note => {
		const card = document.createElement('article');
		card.className = 'note-card';
		card.innerHTML = `
			<header>
				<span class="pill-date">Updated ${formatDate(note.updatedAt)}</span>
				<div class="actions-row">
					<button class="icon-btn" data-edit="${note.id}">Edit</button>
					<button class="icon-btn danger" data-delete="${note.id}">Delete</button>
				</div>
			</header>
			<h3>${escapeHtml(note.title)}</h3>
			<p>${escapeHtml(note.body || '')}</p>
			<div class="tags">${(note.tags || []).map(t => `<span class="tag">${escapeHtml(t)}</span>`).join('')}</div>
		`;
		card.querySelector('[data-edit]')?.addEventListener('click', () => openEditDialog(note));
		card.querySelector('[data-delete]')?.addEventListener('click', () => confirmDelete(note));
		els.container.appendChild(card);
	});

	updateCount();
}

function escapeHtml(str) {
	return str
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;');
}

async function loadNotes(query = '') {
	try {
		state.query = query;
		localStorage.setItem(SEARCH_KEY, query);
		state.notes = await apiGetNotes(query);
		renderNotes();
	} catch (err) {
		console.error(err);
		alert('Could not load notes.');
	}
}

function updateCount() {
	const count = state.notes.length;
	if (els.count) {
		const label = state.query
			? `${count} result${count === 1 ? '' : 's'} for "${state.query}"`
			: `${count} note${count === 1 ? '' : 's'}`;
		els.count.textContent = label;
	}
}

function openCreateDialog() {
	els.dialogMode.textContent = 'New note';
	els.dialogTitle.textContent = 'Create note';
	els.noteId.value = '';
	els.titleInput.value = '';
	els.bodyInput.value = '';
	els.tagsInput.value = '';
	els.saveBtn.textContent = 'Save';
	openDialog();
}

function openEditDialog(note) {
	els.dialogMode.textContent = 'Edit note';
	els.dialogTitle.textContent = 'Update note';
	els.noteId.value = note.id;
	els.titleInput.value = note.title;
	els.bodyInput.value = note.body || '';
	els.tagsInput.value = (note.tags || []).join(', ');
	els.saveBtn.textContent = 'Update';
	openDialog();
}

function openDialog() {
	if (typeof els.dialog.showModal === 'function') {
		els.dialog.showModal();
	} else {
		els.dialog.setAttribute('open', 'true');
	}
	els.titleInput.focus();
}

function closeDialog() {
	if (typeof els.dialog.close === 'function') {
		els.dialog.close();
	} else {
		els.dialog.removeAttribute('open');
	}
}

async function confirmDelete(note) {
	const ok = confirm(`Delete "${note.title}"? This cannot be undone.`);
	if (!ok) return;
	try {
		await apiDeleteNote(note.id);
		await loadNotes(state.query);
	} catch (err) {
		console.error(err);
		alert('Could not delete note.');
	}
}

els.form?.addEventListener('submit', async (e) => {
	e.preventDefault();
	const payload = {
		title: els.titleInput.value.trim(),
		body: els.bodyInput.value,
		tags: tagsToArray(els.tagsInput.value)
	};

	if (!payload.title) {
		alert('Title is required.');
		return;
	}

	const id = els.noteId.value;
	try {
		if (id) {
			await apiUpdateNote(id, payload);
		} else {
			await apiCreateNote(payload);
		}
		closeDialog();
		await loadNotes(state.query);
	} catch (err) {
		console.error(err);
		alert('Could not save note.');
	}
});

els.addBtn?.addEventListener('click', openCreateDialog);
els.emptyAddBtn?.addEventListener('click', openCreateDialog);
els.closeDialog?.addEventListener('click', closeDialog);
els.cancelDialog?.addEventListener('click', (e) => { e.preventDefault(); closeDialog(); });
els.searchFocusBtn?.addEventListener('click', () => els.searchInput?.focus());
els.clearSearchBtn?.addEventListener('click', () => {
	if (!els.searchInput) return;
	els.searchInput.value = '';
	localStorage.setItem(SEARCH_KEY, '');
	loadNotes('');
	els.searchInput.focus();
});

els.searchInput?.addEventListener('input', (e) => {
	const value = e.target.value;
	const timeoutId = els.searchInput.dataset.tid;
	if (timeoutId) window.clearTimeout(timeoutId);
	const tid = window.setTimeout(() => loadNotes(value), 160);
	els.searchInput.dataset.tid = tid;
});

document.addEventListener('keydown', (e) => {
	if (e.key === '/' && document.activeElement !== els.searchInput) {
		e.preventDefault();
		els.searchInput?.focus();
	}
	if (e.key === 'n' && !e.ctrlKey && !e.metaKey && !e.altKey) {
		e.preventDefault();
		openCreateDialog();
	}
	if ((e.key === 'Enter' && (e.metaKey || e.ctrlKey)) && isDialogOpen()) {
		e.preventDefault();
		els.form?.requestSubmit();
	}
	if (e.key === 'Escape' && isDialogOpen()) {
		closeDialog();
	}
});

const savedQuery = localStorage.getItem(SEARCH_KEY) ?? '';
if (els.searchInput) {
	els.searchInput.value = savedQuery;
}
loadNotes(savedQuery);

function isDialogOpen() {
	if (!els.dialog) return false;
	return typeof els.dialog.open === 'boolean' ? els.dialog.open : els.dialog.hasAttribute('open');
}
