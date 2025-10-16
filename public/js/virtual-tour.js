(function () {
  const viewerContainer = document.querySelector('[data-tour-viewer]');
  if (!viewerContainer) {
    return;
  }

  const PLACEHOLDER_TOKEN = '__placeholder__';
  const PLACEHOLDER_PANORAMA = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAIAAAB7QOjdAAAAD0lEQVR4nGPgljUTyo8AAAMeAThJurEoAAAAAElFTkSuQmCC';

  const lang = (viewerContainer.getAttribute('data-lang') || 'de').toLowerCase();
  let panorama = viewerContainer.getAttribute('data-panorama');
  if (panorama === PLACEHOLDER_TOKEN) {
    panorama = PLACEHOLDER_PANORAMA;
  }
  const caption = viewerContainer.getAttribute('data-caption') || '';
  const statusEl = document.querySelector('[data-tour-status]');

  const messages = {
    de: {
      missing: 'Es ist kein 360°-Bild hinterlegt. Bitte legen Sie eine Datei ab und passen Sie den Datenpfad an.',
      loading: '360°-Panorama wird geladen …',
      ready: 'Rundgang bereit – nutzen Sie Maus oder Touchgesten für die Navigation.',
      error: 'Das Panorama konnte nicht geladen werden. Prüfen Sie Dateiname und Pfad.'
    },
    en: {
      missing: 'No 360° asset configured. Upload a file and update the data attribute accordingly.',
      loading: 'Loading 360° panorama …',
      ready: 'Tour ready – use your mouse or touch gestures to explore.',
      error: 'Unable to load the panorama. Please verify the filename and path.'
    }
  };

  const copy = messages[lang] || messages.de;

  function setStatus(type) {
    if (!statusEl) {
      return;
    }
    statusEl.classList.remove('is-ready', 'is-error');
    if (type === 'ready') {
      statusEl.textContent = copy.ready;
      statusEl.classList.add('is-ready');
    } else if (type === 'error') {
      statusEl.textContent = copy.error;
      statusEl.classList.add('is-error');
    } else if (type === 'loading') {
      statusEl.textContent = copy.loading;
    } else {
      statusEl.textContent = copy.missing;
      statusEl.classList.add('is-error');
    }
  }

  if (!panorama) {
    setStatus('missing');
    return;
  }

  if (typeof PhotoSphereViewer === 'undefined' || !PhotoSphereViewer.Viewer) {
    console.error('[virtual-tour] Photo Sphere Viewer ist nicht verfügbar.');
    setStatus('error');
    return;
  }

  setStatus('loading');

  function bootstrapViewer() {
    try {
      const viewer = new PhotoSphereViewer.Viewer({
        container: viewerContainer,
        panorama,
        caption,
        touchmoveTwoFingers: true,
        mousewheelCtrlKey: true,
        navbar: [
          'autorotate',
          'zoom',
          'fullscreen'
        ],
        defaultYaw: '100deg',
        canvasBackground: '#0b1d36'
      });

      viewer.once('ready', function () {
        setStatus('ready');
      });

      viewer.on('panorama-load-failed', function (e) {
        console.error('[virtual-tour] Fehler beim Laden des Panoramas:', e?.error || e);
        setStatus('error');
      });
    } catch (error) {
      console.error('[virtual-tour] Initialisierung fehlgeschlagen:', error);
      setStatus('error');
    }
  }

  const img = new Image();
  img.onload = function () {
    bootstrapViewer();
  };
  img.onerror = function (event) {
    console.error('[virtual-tour] Panorama-Datei nicht gefunden oder ungültig:', event);
    setStatus('error');
  };
  img.src = panorama;
})();
