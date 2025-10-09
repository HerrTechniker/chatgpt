(async function () {
  const sessionInfo = document.getElementById('session-info');
  const logoutBtn = document.getElementById('logout-btn');
  const downloadForm = document.getElementById('download-form');
  const downloadTable = document.getElementById('download-table');
  const downloadNotice = document.getElementById('download-notice');
  const resetDownloadBtn = document.getElementById('reset-download');
  const bookingSection = document.getElementById('booking-management');
  const bookingTable = document.getElementById('booking-table');
  const userSection = document.getElementById('user-management');
  const userForm = document.getElementById('user-form');
  const userTable = document.getElementById('user-table');
  const userNotice = document.getElementById('user-notice');
  const resetUserBtn = document.getElementById('reset-user');
  const toast = document.getElementById('toast');

  let currentUser = null;
  let downloads = [];
  let bookings = [];
  let users = [];

  const dateTimeFormatter = new Intl.DateTimeFormat('de-DE', { dateStyle: 'medium', timeStyle: 'short' });
  const statusLabels = {
    pending: 'Offen',
    confirmed: 'Bestätigt',
    cancelled: 'Storniert',
    completed: 'Abgeschlossen'
  };

  function setNotice(element, message, type = 'success') {
    if (!element) return;
    if (!message) {
      element.classList.add('hidden');
      element.textContent = '';
      element.classList.remove('error');
      return;
    }
    element.textContent = message;
    element.classList.remove('hidden');
    element.classList.toggle('error', type === 'error');
  }

  function showToast(message, type = 'success') {
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add('visible');
    toast.classList.toggle('error', type === 'error');
    clearTimeout(showToast.timeout);
    showToast.timeout = setTimeout(() => toast.classList.remove('visible'), 2600);
  }

  async function fetchJson(url, options = {}) {
    const response = await fetch(url, {
      headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
      credentials: 'same-origin',
      ...options
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(data.error || 'Unbekannter Fehler');
    }
    return data;
  }

  function escapeHtml(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function formatBytes(bytes) {
    if (typeof bytes !== 'number' || Number.isNaN(bytes)) {
      return '';
    }
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;
    while (size >= 1000 && unitIndex < units.length - 1) {
      size /= 1000;
      unitIndex += 1;
    }
    const formatted = size >= 10 || unitIndex === 0 ? Math.round(size) : Math.round(size * 10) / 10;
    return `${String(formatted).replace('.', ',')} ${units[unitIndex]}`;
  }

  function getAttachmentLabel(field) {
    switch (field) {
      case 'cv':
        return 'Lebenslauf';
      case 'application':
        return 'Anschreiben';
      case 'photo':
        return 'Foto';
      case 'documents':
        return 'Dokument';
      default:
        return 'Anhang';
    }
  }

  async function loadSession() {
    const { user } = await fetchJson('/api/session');
    if (!user) {
      window.location.href = '/admin/login';
      return;
    }
    currentUser = user;
    if (sessionInfo) {
      sessionInfo.textContent = `${user.name} (${user.role === 'admin' ? 'Administrator' : 'Editor'})`;
    }
    if (userSection && user.role !== 'admin') {
      userSection.classList.add('hidden');
    }
    if (bookingSection && user.role !== 'admin') {
      bookingSection.classList.add('hidden');
    }
  }

  async function loadDownloads() {
    const data = await fetchJson('/api/downloads', { method: 'GET', headers: {} });
    downloads = data.downloads || [];
    renderDownloads();
  }

  function renderBookings() {
    if (!bookingTable) return;
    if (!bookings.length) {
      bookingTable.innerHTML = '<tr><td colspan="7">Noch keine Anfragen eingegangen.</td></tr>';
      return;
    }
    bookingTable.innerHTML = bookings
      .map((booking) => {
        const period = `${dateTimeFormatter.format(new Date(booking.startDateTime))} – ${dateTimeFormatter.format(
          new Date(booking.endDateTime)
        )}`;
        const note = booking.note ? escapeHtml(booking.note).replace(/\n/g, '<br />') : '—';
        const contactParts = [];
        if (booking.email) {
          const mail = encodeURIComponent(String(booking.email));
          contactParts.push(`<a href="mailto:${mail}">${escapeHtml(booking.email)}</a>`);
        }
        if (booking.phone) {
          const tel = String(booking.phone).replace(/[^+\d]/g, '');
          contactParts.push(`<a href="tel:${tel}">${escapeHtml(booking.phone)}</a>`);
        }
        const attachments = Array.isArray(booking.attachments) && booking.attachments.length
          ? `<ul class="attachment-list">${booking.attachments
              .map((attachment) => {
                const label = getAttachmentLabel(attachment.field);
                const url = attachment.downloadUrl ? escapeHtml(attachment.downloadUrl) : '#';
                const title = attachment.originalName ? escapeHtml(attachment.originalName) : label;
                const size = typeof attachment.size === 'number' ? formatBytes(attachment.size) : '';
                const meta = [title, size].filter(Boolean).join(' · ');
                return `<li><a href="${url}" target="_blank" rel="noopener">${escapeHtml(label)}</a>${
                  meta ? `<span class="table-sub">${meta}</span>` : ''
                }</li>`;
              })
              .join('')}</ul>`
          : '—';
        const actions = [];
        if (booking.status !== 'confirmed') {
          actions.push('<button class="btn btn-outline" data-action="set-status" data-status="confirmed">Bestätigen</button>');
        }
        if (booking.status !== 'completed') {
          actions.push('<button class="btn btn-outline" data-action="set-status" data-status="completed">Abschließen</button>');
        }
        if (booking.status !== 'cancelled') {
          actions.push('<button class="btn btn-secondary" data-action="set-status" data-status="cancelled">Stornieren</button>');
        }
        actions.push('<button class="btn btn-secondary" data-action="delete-booking">Löschen</button>');

        const statusLabel = statusLabels[booking.status] || booking.status;

        return `
          <tr data-id="${booking.id}">
            <td>
              <strong>${escapeHtml(booking.name)}</strong><br />
              <span class="table-sub">erstellt am ${dateTimeFormatter.format(new Date(booking.createdAt))}</span>
            </td>
            <td>${period}</td>
            <td>${contactParts.join('<br />') || '—'}</td>
            <td><span class="status-badge status-${booking.status}">${statusLabel}</span></td>
            <td>${note}</td>
            <td>${attachments}</td>
            <td>
              <div class="admin-actions">
                ${actions.join('')}
              </div>
            </td>
          </tr>
        `;
      })
      .join('');
  }

  async function loadBookingsAdmin() {
    if (!bookingSection || bookingSection.classList.contains('hidden')) return;
    const data = await fetchJson('/api/bookings');
    bookings = (data.bookings || []).slice().sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
    renderBookings();
  }

  function renderDownloads() {
    if (!downloadTable) return;
    if (!downloads.length) {
      downloadTable.innerHTML = '<tr><td colspan="5">Noch keine Downloads vorhanden.</td></tr>';
      return;
    }
    downloadTable.innerHTML = downloads
      .map((item) => {
        const fileLink = item.filePath
          ? `<a href="${item.filePath}" target="_blank" rel="noopener">${item.filePath.split('/').pop()}</a>`
          : '<span class="badge-muted status-badge">keine Datei</span>';
        return `
          <tr data-id="${item.id}">
            <td>${item.title}</td>
            <td>${item.version || '–'}</td>
            <td>${item.releaseDate || '–'}</td>
            <td>${fileLink}</td>
            <td>
              <div class="admin-actions">
                <button class="btn btn-outline" data-action="edit">Bearbeiten</button>
                <button class="btn btn-secondary" data-action="delete">Löschen</button>
              </div>
            </td>
          </tr>
        `;
      })
      .join('');
  }

  function resetDownloadForm() {
    if (!downloadForm) return;
    downloadForm.reset();
    const idField = document.getElementById('download-id');
    if (idField) {
      idField.value = '';
    }
    setNotice(downloadNotice, '');
  }

  async function fileToBase64(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result);
      reader.onerror = () => reject(new Error('Datei konnte nicht gelesen werden.'));
      reader.readAsDataURL(file);
    });
  }

  if (downloadForm) {
    downloadForm.addEventListener('submit', async (event) => {
      event.preventDefault();
      const formData = new FormData(downloadForm);
      const id = document.getElementById('download-id').value;
      const payload = {
        title: formData.get('title'),
        version: formData.get('version') || '',
        releaseDate: formData.get('releaseDate') || '',
        description: formData.get('description') || ''
      };
      if (!payload.title) {
        setNotice(downloadNotice, 'Bitte geben Sie einen Titel an.', 'error');
        return;
      }
      const file = formData.get('file');
      if (file && file.size) {
        payload.fileName = file.name;
        payload.file = await fileToBase64(file);
      }
      try {
        if (id) {
          const { download } = await fetchJson(`/api/downloads/${id}`, {
            method: 'PUT',
            body: JSON.stringify(payload)
          });
          downloads = downloads.map((item) => (item.id === download.id ? download : item));
          showToast('Download aktualisiert');
        } else {
          const { download } = await fetchJson('/api/downloads', {
            method: 'POST',
            body: JSON.stringify(payload)
          });
          downloads.push(download);
          showToast('Download hinzugefügt');
        }
        renderDownloads();
        resetDownloadForm();
      } catch (error) {
        setNotice(downloadNotice, error.message, 'error');
        showToast(error.message, 'error');
      }
    });
  }

  if (resetDownloadBtn) {
    resetDownloadBtn.addEventListener('click', () => {
      resetDownloadForm();
    });
  }

  if (downloadTable) {
    downloadTable.addEventListener('click', async (event) => {
      const target = event.target;
      if (!(target instanceof HTMLElement)) return;
      const action = target.dataset.action;
      if (!action) return;
      const row = target.closest('tr');
      if (!row) return;
      const id = row.getAttribute('data-id');
      const download = downloads.find((item) => item.id === id);
      if (!download) return;

      if (action === 'edit') {
        document.getElementById('download-id').value = download.id;
        document.getElementById('download-title').value = download.title;
        document.getElementById('download-version').value = download.version || '';
        document.getElementById('download-date').value = download.releaseDate || '';
        document.getElementById('download-description').value = download.description || '';
        setNotice(downloadNotice, 'Download zur Bearbeitung geladen.');
        downloadForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }

      if (action === 'delete') {
        const confirmDelete = window.confirm(`Download "${download.title}" wirklich löschen?`);
        if (!confirmDelete) return;
        try {
          await fetchJson(`/api/downloads/${id}`, { method: 'DELETE', body: JSON.stringify({}) });
          downloads = downloads.filter((item) => item.id !== id);
          renderDownloads();
          showToast('Download gelöscht');
        } catch (error) {
          showToast(error.message, 'error');
        }
      }
    });
  }

  function resetUserForm() {
    if (!userForm) return;
    userForm.reset();
    const idField = document.getElementById('user-id');
    if (idField) {
      idField.value = '';
    }
    setNotice(userNotice, '');
  }

  function renderUsers() {
    if (!userTable) return;
    if (!users.length) {
      userTable.innerHTML = '<tr><td colspan="4">Keine Benutzer angelegt.</td></tr>';
      return;
    }
    userTable.innerHTML = users
      .map((user) => `
        <tr data-id="${user.id}">
          <td>${user.name}</td>
          <td>${user.email}</td>
          <td><span class="tag">${user.role}</span></td>
          <td>
            <div class="admin-actions">
              <button class="btn btn-outline" data-action="edit-user">Bearbeiten</button>
              <button class="btn btn-secondary" data-action="delete-user">Löschen</button>
            </div>
          </td>
        </tr>
      `)
      .join('');
  }

  async function loadUsers() {
    if (!userSection || userSection.classList.contains('hidden')) return;
    const data = await fetchJson('/api/users');
    users = data.users || [];
    renderUsers();
  }

  if (userForm) {
    userForm.addEventListener('submit', async (event) => {
      event.preventDefault();
      if (!currentUser || currentUser.role !== 'admin') {
        showToast('Keine Berechtigung', 'error');
        return;
      }
      const formData = new FormData(userForm);
      const id = document.getElementById('user-id').value;
      const payload = {
        name: formData.get('name'),
        email: formData.get('email'),
        role: formData.get('role')
      };
      const password = formData.get('password');
      if (!payload.name || !payload.email || !payload.role) {
        setNotice(userNotice, 'Bitte füllen Sie alle Pflichtfelder aus.', 'error');
        return;
      }
      if (!id && !password) {
        setNotice(userNotice, 'Bitte vergeben Sie ein Passwort für neue Konten.', 'error');
        return;
      }
      if (password) {
        payload.password = password;
      }
      try {
        if (id) {
          const { user } = await fetchJson(`/api/users/${id}`, {
            method: 'PUT',
            body: JSON.stringify(payload)
          });
          users = users.map((item) => (item.id === user.id ? user : item));
          showToast('Benutzer aktualisiert');
        } else {
          const { user } = await fetchJson('/api/users', {
            method: 'POST',
            body: JSON.stringify(payload)
          });
          users.push(user);
          showToast('Benutzer erstellt');
        }
        renderUsers();
        resetUserForm();
      } catch (error) {
        setNotice(userNotice, error.message, 'error');
        showToast(error.message, 'error');
      }
    });
  }

  if (resetUserBtn) {
    resetUserBtn.addEventListener('click', () => {
      resetUserForm();
    });
  }

  if (bookingTable) {
    bookingTable.addEventListener('click', async (event) => {
      const target = event.target;
      if (!(target instanceof HTMLElement)) return;
      const row = target.closest('tr');
      if (!row) return;
      const id = row.getAttribute('data-id');
      if (!id) return;

      const action = target.dataset.action;
      if (action === 'set-status') {
        const status = target.dataset.status;
        if (!status) return;
        try {
          const { booking } = await fetchJson(`/api/bookings/${id}`, {
            method: 'PUT',
            body: JSON.stringify({ status })
          });
          bookings = bookings.map((item) => (item.id === booking.id ? booking : item));
          renderBookings();
          showToast('Status aktualisiert');
        } catch (error) {
          showToast(error.message, 'error');
        }
      }

      if (action === 'delete-booking') {
        const confirmDelete = window.confirm('Anfrage endgültig löschen?');
        if (!confirmDelete) return;
        try {
          await fetchJson(`/api/bookings/${id}`, { method: 'DELETE', body: JSON.stringify({}) });
          bookings = bookings.filter((item) => item.id !== id);
          renderBookings();
          showToast('Anfrage gelöscht');
        } catch (error) {
          showToast(error.message, 'error');
        }
      }
    });
  }

  if (userTable) {
    userTable.addEventListener('click', async (event) => {
      const target = event.target;
      if (!(target instanceof HTMLElement)) return;
      const action = target.dataset.action;
      if (!action) return;
      const row = target.closest('tr');
      if (!row) return;
      const id = row.getAttribute('data-id');
      const user = users.find((item) => item.id === id);
      if (!user) return;

      if (action === 'edit-user') {
        document.getElementById('user-id').value = user.id;
        document.getElementById('user-name').value = user.name;
        document.getElementById('user-email').value = user.email;
        document.getElementById('user-role').value = user.role;
        document.getElementById('user-password').value = '';
        setNotice(userNotice, 'Benutzer zur Bearbeitung geladen.');
        userForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }

      if (action === 'delete-user') {
        if (currentUser && currentUser.id === user.id) {
          showToast('Eigenes Konto kann nicht gelöscht werden.', 'error');
          return;
        }
        const confirmed = window.confirm(`Benutzer "${user.name}" wirklich löschen?`);
        if (!confirmed) return;
        try {
          await fetchJson(`/api/users/${id}`, { method: 'DELETE', body: JSON.stringify({}) });
          users = users.filter((item) => item.id !== id);
          renderUsers();
          showToast('Benutzer gelöscht');
        } catch (error) {
          showToast(error.message, 'error');
        }
      }
    });
  }

  if (logoutBtn) {
    logoutBtn.addEventListener('click', async () => {
      try {
        await fetchJson('/api/logout', { method: 'POST', body: JSON.stringify({}) });
      } catch (error) {
        // ignore
      }
      window.location.href = '/admin/login';
    });
  }

  try {
    await loadSession();
    await loadDownloads();
    await loadBookingsAdmin();
    await loadUsers();
  } catch (error) {
    showToast(error.message || 'Fehler beim Laden der Daten', 'error');
  }
})();
