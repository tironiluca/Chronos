// Display helpers for ISO dates ('YYYY-MM-DD'). Parsed as local dates (not UTC midnight) so the
// rendered day never shifts by one in timezones west of UTC.
function parseIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, (m ?? 1) - 1, d ?? 1);
}

const shortDate = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short' });
const longDate = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short', year: 'numeric' });

export function formatDate(iso: string): string {
  return longDate.format(parseIsoDate(iso));
}

// "5 Jan – 10 Jan 2026", or a single date when start === end. Year shown once, on the end date,
// unless the range crosses a year boundary.
export function formatDateRange(startIso: string, endIso: string): string {
  if (startIso === endIso) return formatDate(startIso);
  const start = parseIsoDate(startIso);
  const end = parseIsoDate(endIso);
  const startText = start.getFullYear() === end.getFullYear() ? shortDate.format(start) : longDate.format(start);
  return `${startText} – ${longDate.format(end)}`;
}

// Inclusive calendar-day count.
export function dayCount(startIso: string, endIso: string): number {
  const ms = parseIsoDate(endIso).getTime() - parseIsoDate(startIso).getTime();
  return Math.round(ms / 86_400_000) + 1;
}
