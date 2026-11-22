// Member 3 - Appointment Booking: FullCalendar slot picker on /Patient/Book/{doctorId}.
(function () {
    'use strict';

    var el = document.getElementById('bkCalendar');
    if (!el || typeof FullCalendar === 'undefined') return;

    var slotsUrl = el.dataset.slotsUrl;
    var checkoutUrl = el.dataset.checkoutUrl;
    var slotMinutes = parseInt(el.dataset.slotMinutes, 10) || 30;
    var maxDays = parseInt(el.dataset.maxDays, 10) || 60;

    var empty = document.getElementById('bkSelectionEmpty');
    var details = document.getElementById('bkSelectionDetails');
    var dayText = document.getElementById('bkSelectedDay');
    var timeText = document.getElementById('bkSelectedTime');
    var continueBtn = document.getElementById('bkContinue');
    var errorBox = document.getElementById('bkCalendarError');
    var selectedEl = null;

    var pad = function (n) { return (n < 10 ? '0' : '') + n; };

    // Local "yyyy-MM-ddTHH:mm:ss" (no time zone) - the server stores clinic local time.
    function toLocalIso(d) {
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) +
            'T' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':00';
    }

    var today = new Date();
    today.setHours(0, 0, 0, 0);
    var lastDay = new Date(today);
    lastDay.setDate(lastDay.getDate() + maxDays + 1);

    var isSmall = window.matchMedia('(max-width: 767.98px)').matches;

    var calendar = new FullCalendar.Calendar(el, {
        // "Next 7 days" views start from the shown date instead of a fixed week,
        // so the patient never lands on a half-empty week of past days.
        views: {
            timeGridNext7: { type: 'timeGrid', duration: { days: 7 }, dateAlignment: 'day', buttonText: 'Week' },
            listNext7: { type: 'list', duration: { days: 7 }, dateAlignment: 'day', buttonText: 'List' }
        },
        initialView: isSmall ? 'listNext7' : 'timeGridNext7',
        headerToolbar: isSmall
            ? { left: 'prev,next', center: 'title', right: 'today' }
            : { left: 'prev,next today', center: 'title', right: 'timeGridNext7,timeGridDay,listNext7' },
        buttonText: { today: 'Today', day: 'Day' },
        allDaySlot: false,
        nowIndicator: true,
        height: 'auto',
        slotMinTime: '08:00:00',
        slotMaxTime: '18:00:00',
        slotDuration: '00:' + pad(slotMinutes) + ':00',
        expandRows: true,
        validRange: { start: today, end: lastDay },
        eventTimeFormat: { hour: 'numeric', minute: '2-digit', meridiem: 'short' },
        noEventsContent: 'No available times in these days.',

        // Time grid: show just the start time (the colour already says free / booked).
        eventContent: function (arg) {
            if (arg.view.type.indexOf('list') === 0) return true; // default list rendering
            var label = arg.event.extendedProps.available ? arg.timeText : arg.timeText + ' · Booked';
            return { html: '<div class="fc-event-main-frame"><div class="fc-event-time">' + label + '</div></div>' };
        },
        displayEventEnd: false,

        events: function (info, success, failure) {
            var url = slotsUrl + '?start=' + encodeURIComponent(toLocalIso(info.start)) +
                '&end=' + encodeURIComponent(toLocalIso(info.end));
            fetch(url, { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' })
                .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
                .then(function (slots) {
                    errorBox.classList.add('d-none');
                    success(slots.map(function (s) {
                        return {
                            id: s.id, title: s.title, start: s.start, end: s.end,
                            classNames: s.classNames, extendedProps: { available: s.available }
                        };
                    }));
                })
                .catch(function (err) { errorBox.classList.remove('d-none'); failure(err); });
        },

        eventClick: function (info) {
            info.jsEvent.preventDefault();
            if (!info.event.extendedProps.available) return;

            if (selectedEl) selectedEl.classList.remove('bk-slot-selected');
            selectedEl = info.el;
            selectedEl.classList.add('bk-slot-selected');

            var start = info.event.start;
            var end = info.event.end;
            var timeFmt = { hour: 'numeric', minute: '2-digit' };
            dayText.textContent = start.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric', year: 'numeric' });
            timeText.textContent = start.toLocaleTimeString(undefined, timeFmt) + ' – ' + end.toLocaleTimeString(undefined, timeFmt);
            continueBtn.href = checkoutUrl + '?start=' + encodeURIComponent(toLocalIso(start));

            empty.classList.add('d-none');
            details.classList.remove('d-none');
            if (isSmall) document.getElementById('bkSelection').scrollIntoView({ behavior: 'smooth' });
        },

        // Keep the selection highlighted when the user switches week/day views.
        eventDidMount: function (info) {
            if (continueBtn.href.indexOf(encodeURIComponent(toLocalIso(info.event.start))) !== -1 && info.event.extendedProps.available) {
                selectedEl = info.el;
                info.el.classList.add('bk-slot-selected');
            }
            if (!info.event.extendedProps.available) info.el.setAttribute('aria-disabled', 'true');
        }
    });

    calendar.render();
})();
