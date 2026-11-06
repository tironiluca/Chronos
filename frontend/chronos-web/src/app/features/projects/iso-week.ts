// ISO-8601 week helpers for the project x week load grid. All dates are handled as UTC
// midnight and exchanged as yyyy-MM-dd strings (same as the API and the leave grid), so string
// comparison is a valid date comparison.

export interface IsoWeekColumn {
  key: string; // e.g. 2026-W14
  isoYear: number;
  week: number;
  start: string; // Monday, yyyy-MM-dd
  end: string; // Sunday, yyyy-MM-dd
}

const DAY_MS = 86_400_000;

export function isoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function parseIsoDate(value: string): Date {
  return new Date(`${value}T00:00:00Z`);
}

// Monday of the ISO week containing `date`.
export function startOfIsoWeek(date: Date): Date {
  const dayOfWeek = (date.getUTCDay() + 6) % 7; // Monday = 0 ... Sunday = 6
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate() - dayOfWeek));
}

// ISO week-numbering year + week: the week belongs to the year that contains its Thursday.
export function isoWeekOf(date: Date): { isoYear: number; week: number } {
  const thursday = new Date(startOfIsoWeek(date).getTime() + 3 * DAY_MS);
  const isoYear = thursday.getUTCFullYear();
  const firstThursday = new Date(startOfIsoWeek(new Date(Date.UTC(isoYear, 0, 4))).getTime() + 3 * DAY_MS);
  const week = 1 + Math.round((thursday.getTime() - firstThursday.getTime()) / (7 * DAY_MS));
  return { isoYear, week };
}

// First day of the calendar quarter `offsetQuarters` away from the quarter containing `today`.
export function quarterStart(today: Date, offsetQuarters: number): Date {
  const currentQuarterMonth = Math.floor(today.getUTCMonth() / 3) * 3;
  return new Date(Date.UTC(today.getUTCFullYear(), currentQuarterMonth + offsetQuarters * 3, 1));
}

export function quarterEnd(start: Date): Date {
  return new Date(Date.UTC(start.getUTCFullYear(), start.getUTCMonth() + 3, 0));
}

export function quarterLabel(start: Date): string {
  return `Q${Math.floor(start.getUTCMonth() / 3) + 1} ${start.getUTCFullYear()}`;
}

// Every ISO week that overlaps [from, to] (inclusive), in order. A quarter yields 13 or 14
// columns, since its first/last weeks usually straddle the quarter boundary.
export function isoWeeksBetween(from: string, to: string): IsoWeekColumn[] {
  const columns: IsoWeekColumn[] = [];
  const last = parseIsoDate(to).getTime();
  for (let monday = startOfIsoWeek(parseIsoDate(from)); monday.getTime() <= last; ) {
    const { isoYear, week } = isoWeekOf(monday);
    const sunday = new Date(monday.getTime() + 6 * DAY_MS);
    columns.push({
      key: `${isoYear}-W${String(week).padStart(2, '0')}`,
      isoYear,
      week,
      start: isoDate(monday),
      end: isoDate(sunday)
    });
    monday = new Date(monday.getTime() + 7 * DAY_MS);
  }
  return columns;
}

export function overlaps(rangeStart: string, rangeEnd: string, column: IsoWeekColumn): boolean {
  return rangeStart <= column.end && rangeEnd >= column.start;
}
