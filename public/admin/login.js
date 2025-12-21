(function () {
  const form = document.getElementById('login-form');
  const notice = document.getElementById('login-notice');
  const toast = document.getElementById('toast');

  const suspiciousPatterns = [
    /('|")\s*or\s+1=1/i,
    /('|")\s*or\s+('|\")[^'\"]+('|\")\s*=\s*('|\")[^'\"]+('|\")/i,
    /;\s*(?:drop|delete|insert|update|exec|create)\b/i,
    /\bunion\s+select\b/i,
    /\bwaitfor\s+delay\b/i,
    /\bsleep\s*\(/i,
    /\bbenchmark\s*\(/i,
    /(?:^|[\s'\"])--/i,
    /\/\*/,
    /\bxp_/i
  ];

  function isSuspicious(value) {
    if (typeof value !== 'string' || !value) {
      return false;
    }
    return suspiciousPatterns.some((pattern) => pattern.test(value));
  }

  function isValidEmail(email) {
    if (!email || email.length > 254) {
      return false;
    }
    return /^[a-z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-z0-9-]+(?:\.[a-z0-9-]+)+$/i.test(email);
  }

  async function showToast(message, isError = false) {
    toast.textContent = message;
    toast.classList.toggle('visible', true);
    toast.classList.toggle('error', isError);
    setTimeout(() => toast.classList.remove('visible'), 2500);
  }

  function showError(message) {
    notice.textContent = message;
    notice.classList.remove('hidden');
    notice.classList.add('error');
    showToast(message, true);
  }

  if (form) {
    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      notice.classList.add('hidden');
      const formData = new FormData(form);
      const payload = Object.fromEntries(formData.entries());
      const email = (payload.email || '').trim();
      const password = typeof payload.password === 'string' ? payload.password : '';

      if (!email || !password) {
        return showError('Bitte füllen Sie beide Felder aus.');
      }

      if (!isValidEmail(email)) {
        return showError('Bitte geben Sie eine gültige E-Mail-Adresse ein.');
      }

      if (password.length > 256) {
        return showError('Das Passwort überschreitet die maximale Länge.');
      }

      if (isSuspicious(email) || isSuspicious(password)) {
        return showError('Die Eingaben enthalten unzulässige Muster.');
      }

      payload.email = email;

      try {
        const response = await fetch('/api/login', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });
        if (!response.ok) {
          const error = await response.json().catch(() => ({ error: 'Anmeldung fehlgeschlagen.' }));
          throw new Error(error.error || 'Anmeldung fehlgeschlagen.');
        }
        await showToast('Anmeldung erfolgreich');
        setTimeout(() => {
          window.location.href = '/admin';
        }, 400);
      } catch (error) {
        showError(error.message);
      }
    });
  }
})();
