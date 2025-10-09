(function () {
  const form = document.getElementById('login-form');
  const notice = document.getElementById('login-notice');
  const toast = document.getElementById('toast');

  async function showToast(message, isError = false) {
    toast.textContent = message;
    toast.classList.toggle('visible', true);
    toast.classList.toggle('error', isError);
    setTimeout(() => toast.classList.remove('visible'), 2500);
  }

  if (form) {
    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      notice.classList.add('hidden');
      const formData = new FormData(form);
      const payload = Object.fromEntries(formData.entries());
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
        notice.textContent = error.message;
        notice.classList.remove('hidden');
        notice.classList.add('error');
        showToast(error.message, true);
      }
    });
  }
})();
