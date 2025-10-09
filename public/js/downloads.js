(async function () {
  const container = document.getElementById('downloads-container');
  if (!container) return;

  try {
    const response = await fetch('/api/downloads');
    if (!response.ok) {
      throw new Error('Fehler beim Laden der Downloads');
    }
    const data = await response.json();
    const downloads = data.downloads || [];
    if (!downloads.length) {
      container.innerHTML = '<p class="empty">Derzeit sind keine Dateien verfügbar.</p>';
      return;
    }
    container.innerHTML = downloads
      .map((item) => {
        const version = item.version ? `<span class="download-meta">Version ${item.version}</span>` : '';
        const release = item.releaseDate ? `<span class="download-meta">Veröffentlicht: ${item.releaseDate}</span>` : '';
        const description = item.description
          ? `<p>${item.description}</p>`
          : '<p class="download-meta">Keine Beschreibung vorhanden.</p>';
        const link = item.filePath
          ? `<a class="btn btn-primary" href="${item.filePath}" download>Download</a>`
          : '<span class="status-badge badge-muted">Datei wird bereitgestellt</span>';
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
    container.innerHTML = '<p class="empty">Die Downloadliste konnte nicht geladen werden.</p>';
  }
})();
