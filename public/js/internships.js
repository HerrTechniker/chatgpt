(function () {
  const calendarGrid = document.getElementById('calendar-grid');
  const monthLabel = document.getElementById('calendar-month');
  const prevBtn = document.getElementById('calendar-prev');
  const nextBtn = document.getElementById('calendar-next');
  const bookingForm = document.getElementById('booking-form');
  const bookingNotice = document.getElementById('booking-notice');
  const resetBtn = document.getElementById('booking-reset');
  const startDateInput = document.getElementById('booking-start-date');
  const endDateInput = document.getElementById('booking-end-date');
  const startTimeInput = document.getElementById('booking-start-time');
  const endTimeInput = document.getElementById('booking-end-time');

  if (!calendarGrid || !monthLabel || !bookingForm) {
    return;
  }

  const lang = (document.documentElement.lang || 'de').toLowerCase().startsWith('en') ? 'en' : 'de';
  const localeConfig = {
    de: {
      locale: 'de-DE',
      weekdays: ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'],
      status: {
        pending: 'angefragt',
        confirmed: 'bestätigt',
        cancelled: 'storniert',
        completed: 'abgeschlossen'
      },
      noReservations: 'Keine Reservierungen',
      reservations: (count, statuses) => `${count} Reservierung(en): ${statuses}`,
      invalidTimes: 'Bitte geben Sie gültige Start- und Endzeiten an.',
      endBeforeStart: 'Die Endzeit muss nach der Startzeit liegen.',
      overlap: 'Der Zeitraum überschneidet sich mit einer bestehenden Reservierung.',
      genericError: 'Ihre Anfrage konnte nicht verarbeitet werden.',
      success: 'Vielen Dank! Wir haben Ihre Anfrage erhalten.',
      calendarError: 'Kalender konnte nicht aktualisiert werden.',
      calendarConsole: 'Buchungen konnten nicht geladen werden'
    },
    en: {
      locale: 'en-US',
      weekdays: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
      status: {
        pending: 'requested',
        confirmed: 'confirmed',
        cancelled: 'cancelled',
        completed: 'completed'
      },
      noReservations: 'No reservations',
      reservations: (count, statuses) => `${count} reservation(s): ${statuses}`,
      invalidTimes: 'Please provide valid start and end times.',
      endBeforeStart: 'The end time must be after the start time.',
      overlap: 'The selected time overlaps with an existing reservation.',
      genericError: 'Your request could not be processed.',
      success: 'Thank you! We have received your request.',
      calendarError: 'Could not update the calendar.',
      calendarConsole: 'Bookings could not be loaded'
    }
  };

  const dictionary = localeConfig[lang] || localeConfig.de;

  const dayFormatter = new Intl.DateTimeFormat(dictionary.locale, { month: 'long', year: 'numeric' });
  const weekdayNames = dictionary.weekdays;
  const statusLabels = dictionary.status;

  let currentMonth = new Date();
  currentMonth.setDate(1);
  let bookings = [];

  function setNotice(message, type = 'success') {
    if (!bookingNotice) return;
    if (!message) {
      bookingNotice.classList.add('hidden');
      bookingNotice.classList.remove('error');
      bookingNotice.textContent = '';
      return;
    }
    bookingNotice.textContent = message;
    bookingNotice.classList.remove('hidden');
    bookingNotice.classList.toggle('error', type === 'error');
  }

  function sanitizeDateInput(value) {
    return value ? value.trim() : '';
  }

  function toISODate(date) {
    return date.toISOString().split('T')[0];
  }

  function combineDateTime(dateValue, timeValue) {
    if (!dateValue || !timeValue) return null;
    const iso = `${dateValue}T${timeValue}`;
    const date = new Date(iso);
    if (Number.isNaN(date.valueOf())) {
      return null;
    }
    return date;
  }

  function hasOverlap(start, end) {
    return bookings.some((booking) => {
      if (booking.status === 'cancelled') {
        return false;
      }
      const bookingStart = new Date(booking.startDateTime);
      const bookingEnd = new Date(booking.endDateTime);
      return bookingStart < end && bookingEnd > start;
    });
  }

  function renderCalendar() {
    const monthTitle = dayFormatter.format(currentMonth);
    monthLabel.textContent = monthTitle.charAt(0).toUpperCase() + monthTitle.slice(1);

    const year = currentMonth.getFullYear();
    const month = currentMonth.getMonth();
    const firstOfMonth = new Date(year, month, 1);
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const startWeekday = (firstOfMonth.getDay() + 6) % 7; // Monday = 0
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const fragments = [];
    weekdayNames.forEach((name) => {
      fragments.push(`<span class="weekday" role="columnheader">${name}</span>`);
    });

    for (let i = 0; i < startWeekday; i += 1) {
      fragments.push('<span class="calendar-cell empty" aria-hidden="true"></span>');
    }

    for (let day = 1; day <= daysInMonth; day += 1) {
      const date = new Date(year, month, day);
      const isoDate = toISODate(date);
      const isPast = date < today;
      const dayBookings = bookings.filter((booking) => {
        const bookingStart = new Date(booking.startDateTime);
        const bookingEnd = new Date(booking.endDateTime);
        const dayStart = new Date(date);
        const dayEnd = new Date(date);
        dayEnd.setHours(23, 59, 59, 999);
        return bookingStart <= dayEnd && bookingEnd >= dayStart;
      });
      let state = 'free';
      if (dayBookings.some((booking) => booking.status === 'confirmed')) {
        state = 'confirmed';
      } else if (dayBookings.length) {
        state = 'pending';
      }
      const labelParts = [`${day}. ${monthTitle}`];
      if (dayBookings.length) {
        const statuses = dayBookings
          .map((booking) => statusLabels[booking.status] || booking.status)
          .join(', ');
        labelParts.push(dictionary.reservations(dayBookings.length, statuses));
      } else {
        labelParts.push(dictionary.noReservations);
      }
      const disabledAttr = isPast ? 'disabled' : '';
      const classes = ['calendar-cell', state];
      if (isPast) {
        classes.push('past');
      }
      fragments.push(
        `<button type="button" class="${classes.join(' ')}" data-date="${isoDate}" aria-label="${labelParts.join(
          '. '
        )}" ${disabledAttr}>${day}</button>`
      );
    }

    calendarGrid.innerHTML = fragments.join('');
  }

  function adjustEndDateMin() {
    const startValue = sanitizeDateInput(startDateInput.value);
    if (!startValue) return;
    endDateInput.min = startValue;
    if (sanitizeDateInput(endDateInput.value) < startValue) {
      endDateInput.value = startValue;
    }
  }

  async function loadBookings() {
    const from = new Date(currentMonth);
    const to = new Date(currentMonth);
    from.setHours(0, 0, 0, 0);
    to.setMonth(to.getMonth() + 1, 1);
    to.setHours(23, 59, 59, 999);
    const params = new URLSearchParams({ from: from.toISOString(), to: to.toISOString() });
    try {
      const response = await fetch(`/api/bookings?${params.toString()}`);
      const data = await response.json();
      bookings = Array.isArray(data.bookings)
        ? data.bookings.slice().sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime))
        : [];
      renderCalendar();
    } catch (error) {
      console.error(dictionary.calendarConsole, error);
      setNotice(dictionary.calendarError, 'error');
    }
  }

  calendarGrid.addEventListener('click', (event) => {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;
    const button = target.closest('button.calendar-cell');
    if (!button || button.hasAttribute('disabled')) return;
    const pickedDate = button.dataset.date;
    if (!pickedDate) return;
    startDateInput.value = pickedDate;
    if (!endDateInput.value || endDateInput.value < pickedDate) {
      endDateInput.value = pickedDate;
    }
    adjustEndDateMin();
  });

  if (prevBtn) {
    prevBtn.addEventListener('click', () => {
      currentMonth.setMonth(currentMonth.getMonth() - 1);
      loadBookings();
    });
  }

  if (nextBtn) {
    nextBtn.addEventListener('click', () => {
      currentMonth.setMonth(currentMonth.getMonth() + 1);
      loadBookings();
    });
  }

  if (startDateInput) {
    startDateInput.addEventListener('change', adjustEndDateMin);
  }

  if (endDateInput) {
    endDateInput.addEventListener('change', adjustEndDateMin);
  }

  if (resetBtn) {
    resetBtn.addEventListener('click', () => {
      bookingForm.reset();
      setNotice('');
      const todayIso = toISODate(new Date());
      startDateInput.value = todayIso;
      endDateInput.value = todayIso;
      adjustEndDateMin();
    });
  }

  bookingForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    setNotice('');

    const startDate = sanitizeDateInput(startDateInput.value);
    const endDate = sanitizeDateInput(endDateInput.value);
    const startTime = sanitizeDateInput(startTimeInput.value);
    const endTime = sanitizeDateInput(endTimeInput.value);

    const start = combineDateTime(startDate, startTime);
    const end = combineDateTime(endDate, endTime);

    if (!start || !end) {
      setNotice(dictionary.invalidTimes, 'error');
      return;
    }

    if (end <= start) {
      setNotice(dictionary.endBeforeStart, 'error');
      return;
    }

    if (hasOverlap(start, end)) {
      setNotice(dictionary.overlap, 'error');
      return;
    }

    const formData = new FormData(bookingForm);
    formData.set('startDateTime', start.toISOString());
    formData.set('endDateTime', end.toISOString());

    try {
      const response = await fetch('/api/bookings', {
        method: 'POST',
        body: formData
      });
      const data = await response.json();
      if (!response.ok) {
        throw new Error(data.error || dictionary.genericError);
      }
      if (data.booking) {
        bookings.push(data.booking);
        bookings.sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime));
      }
      renderCalendar();
      setNotice(data.message || dictionary.success);
      bookingForm.reset();
      const todayIso = toISODate(new Date());
      startDateInput.value = todayIso;
      endDateInput.value = todayIso;
      adjustEndDateMin();
    } catch (error) {
      setNotice(error.message, 'error');
    }
  });

  const todayIso = toISODate(new Date());
  startDateInput.value = todayIso;
  endDateInput.value = todayIso;
  startDateInput.min = todayIso;
  endDateInput.min = todayIso;
  adjustEndDateMin();
  loadBookings();
})();
