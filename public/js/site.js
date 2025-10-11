(function () {
  const yearEl = document.getElementById('year');
  if (yearEl) {
    yearEl.textContent = new Date().getFullYear();
  }

  const navLinks = document.querySelectorAll('.main-nav a');
  const path = window.location.pathname.replace(/\/$/, '') || '/';
  const aliasMap = {
    '/impressum': '/legal',
    '/datenschutz': '/legal',
    '/legal.html': '/legal',
    '/index': '/',
    '/index.html': '/',
    '/internships.html': '/internships'
  };
  const normalizedPath = aliasMap[path] || path;

  navLinks.forEach((link) => {
    const href = link.getAttribute('href');
    if (href === normalizedPath) {
      link.classList.add('active');
    }
  });
})();
