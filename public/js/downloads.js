(async function () {
  const container = document.getElementById('downloads-container');
  if (!container) return;

  const lang = (document.documentElement.lang || 'de').toLowerCase().startsWith('en') ? 'en' : 'de';
  const messages = {
    de: {
      loadError: 'Fehler beim Laden der Downloads',
      empty: 'Derzeit sind keine Dateien verfügbar.',
      version: 'Version',
      published: 'Veröffentlicht',
      noDescription: 'Keine Beschreibung vorhanden.',
      download: 'Download',
      pending: 'Datei wird bereitgestellt',
      fallback: 'Die Downloadliste konnte nicht geladen werden.'
    },
    en: {
      loadError: 'Failed to load downloads',
      empty: 'No files are available right now.',
      version: 'Version',
      published: 'Published',
      noDescription: 'No description provided.',
      download: 'Download',
      pending: 'File is being prepared',
      fallback: 'Could not load the download list.'
    }
  };

  const t = (key) => (messages[lang] && messages[lang][key]) || messages.de[key] || key;

  try {
    const response = await fetch('/api/downloads');
    if (!response.ok) {
      throw new Error(t('loadError'));
    }
    const data = await response.json();
    const downloads = data.downloads || [];
    if (!downloads.length) {
      container.innerHTML = `<p class="empty">${t('empty')}</p>`;
      return;
    }
    container.innerHTML = downloads
      .map((item) => {
        const version = item.version ? `<span class="download-meta">${t('version')} ${item.version}</span>` : '';
        const release = item.releaseDate
          ? `<span class="download-meta">${t('published')}: ${item.releaseDate}</span>`
          : '';
        const description = item.description
          ? `<p>${item.description}</p>`
          : `<p class="download-meta">${t('noDescription')}</p>`;
        const link = item.filePath
          ? `<a class="btn btn-primary" href="${item.filePath}" download>${t('download')}</a>`
          : `<span class="status-badge badge-muted">${t('pending')}</span>`;
        return `
          <article class="download-card">
            <h3>${item.title}</h3>
            <div class="download-meta-wrap">${version} ${release}</div>
            ${description}
            ${link}
          </article>
        `;
      })
      .join('');
  } catch (error) {
    console.error(error);
    container.innerHTML = `<p class="empty">${t('fallback')}</p>`;
  }
})();
