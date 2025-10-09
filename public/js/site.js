(function () {
  const yearEl = document.getElementById('year');
  if (yearEl) {
    yearEl.textContent = new Date().getFullYear();
  }

  const navLinks = document.querySelectorAll('.main-nav a');
  const path = window.location.pathname;
  navLinks.forEach((link) => {
    if (link.getAttribute('href') === path) {
      link.classList.add('active');
    }
  });
})();
