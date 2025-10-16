(function () {
  const documentLang = (document.documentElement.lang || 'de').toLowerCase();
  const activeLang = documentLang.startsWith('en') ? 'en' : 'de';

  const yearEl = document.getElementById('year');
  if (yearEl) {
    yearEl.textContent = new Date().getFullYear();
  }

  const aliasMap = {
    '/index': '/de/',
    '/index.html': '/de/',
    '/de': '/de/',
    '/de/index': '/de/',
    '/de/index.html': '/de/',
    '/internships.html': '/de/internships',
    '/impressum.html': '/de/impressum',
    '/datenschutz.html': '/de/datenschutz',
    '/de/internships.html': '/de/internships',
    '/de/impressum.html': '/de/impressum',
    '/de/datenschutz.html': '/de/datenschutz',
    '/de/virtual-tour.html': '/de/virtual-tour',
    '/legal': '/de/impressum',
    '/legal.html': '/de/impressum',
    '/en/index': '/en',
    '/en/index.html': '/en',
    '/en/internships.html': '/en/internships',
    '/en/impressum': '/en/imprint',
    '/en/impressum.html': '/en/imprint',
    '/en/datenschutz': '/en/privacy',
    '/en/datenschutz.html': '/en/privacy',
    '/en/legal': '/en/imprint',
    '/en/legal.html': '/en/imprint',
    '/en/virtual-tour.html': '/en/virtual-tour'
  };

  const pathMap = {
    home: { de: '/de/', en: '/en' },
    solutions: { de: '/de/solutions', en: '/en/solutions' },
    services: { de: '/de/services', en: '/en/services' },
    industries: { de: '/de/industries', en: '/en/industries' },
    downloads: { de: '/de/downloads', en: '/en/downloads' },
    tour: { de: '/de/virtual-tour', en: '/en/virtual-tour' },
    about: { de: '/de/about', en: '/en/about' },
    internships: { de: '/de/internships', en: '/en/internships' },
    contact: { de: '/de/contact', en: '/en/contact' },
    imprint: { de: '/de/impressum', en: '/en/imprint' },
    privacy: { de: '/de/datenschutz', en: '/en/privacy' }
  };

  const reversePathMap = Object.entries(pathMap).reduce((acc, [key, value]) => {
    acc[value.de] = key;
    acc[value.en] = key;
    return acc;
  }, {});

  function normalizePathname(pathname) {
    if (!pathname || pathname === '/') {
      return '/';
    }
    const trimmed = pathname.endsWith('/') ? pathname.slice(0, -1) : pathname;
    return aliasMap[trimmed] || trimmed || '/';
  }

  function resolveKey(pathname) {
    const normalized = normalizePathname(pathname);
    return reversePathMap[normalized] || 'home';
  }

  function buildPath(lang, key) {
    const mapping = pathMap[key];
    if (!mapping) {
      return lang === 'en' ? '/en' : '/de/';
    }
    return mapping[lang] || mapping.de;
  }

  const currentKey = resolveKey(window.location.pathname);

  const navLinks = document.querySelectorAll('.main-nav a');
  navLinks.forEach((link) => {
    const href = normalizePathname(link.getAttribute('href') || '');
    const linkKey = reversePathMap[href];
    if (linkKey && linkKey === currentKey && buildPath(activeLang, linkKey) === href) {
      link.classList.add('active');
    }
  });

  const languageSwitcher = document.querySelector('[data-lang-switcher]');
  if (languageSwitcher) {
    const buttons = languageSwitcher.querySelectorAll('[data-lang]');
    buttons.forEach((button) => {
      const targetLang = button.getAttribute('data-lang');
      if (targetLang === activeLang) {
        button.classList.add('active');
        button.setAttribute('aria-current', 'true');
      } else {
        button.classList.remove('active');
        button.removeAttribute('aria-current');
      }
      button.addEventListener('click', (event) => {
        event.preventDefault();
        if (targetLang === activeLang) {
          return;
        }
        const destination = buildPath(targetLang, currentKey);
        if (destination && destination !== window.location.pathname) {
          window.location.href = destination;
        }
      });
    });
  }
})();
