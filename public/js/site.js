(function () {
  const yearEl = document.getElementById('year');
  if (yearEl) {
    yearEl.textContent = new Date().getFullYear();
  }

  const navLinks = document.querySelectorAll('.main-nav a');
  const path = window.location.pathname.replace(/\/$/, '') || '/';
  const aliasMap = {
    '/index': '/',
    '/index.html': '/',
    '/internships.html': '/internships',
    '/impressum.html': '/impressum',
    '/datenschutz.html': '/datenschutz',
    '/legal': '/impressum',
    '/legal.html': '/impressum'
  };
  const normalizedPath = aliasMap[path] || path;

  navLinks.forEach((link) => {
    const href = link.getAttribute('href');
    if (href === normalizedPath) {
      link.classList.add('active');
    }
  });
})();
