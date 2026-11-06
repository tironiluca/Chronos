import { isoWeekOf, isoWeeksBetween, overlaps, quarterEnd, quarterLabel, quarterStart, startOfIsoWeek } from './iso-week';

const utc = (value: string) => new Date(`${value}T00:00:00Z`);

describe('iso-week helpers', () => {
  it('finds the Monday of the ISO week', () => {
    expect(startOfIsoWeek(utc('2026-11-06')).toISOString().slice(0, 10)).toBe('2026-11-02'); // Friday
    expect(startOfIsoWeek(utc('2026-11-08')).toISOString().slice(0, 10)).toBe('2026-11-02'); // Sunday
    expect(startOfIsoWeek(utc('2026-11-02')).toISOString().slice(0, 10)).toBe('2026-11-02'); // Monday
  });

  it('numbers weeks per ISO-8601, including year-boundary weeks', () => {
    expect(isoWeekOf(utc('2026-01-01'))).toEqual({ isoYear: 2026, week: 1 }); // Thursday
    expect(isoWeekOf(utc('2027-01-01'))).toEqual({ isoYear: 2026, week: 53 }); // Friday, 2026 has 53 weeks
    expect(isoWeekOf(utc('2024-12-30'))).toEqual({ isoYear: 2025, week: 1 }); // Monday belongs to next ISO year
    expect(isoWeekOf(utc('2026-11-06'))).toEqual({ isoYear: 2026, week: 45 });
  });

  it('computes quarter bounds and labels from an offset', () => {
    const today = utc('2026-11-06');
    expect(quarterLabel(quarterStart(today, 0))).toBe('Q4 2026');
    expect(quarterLabel(quarterStart(today, 1))).toBe('Q1 2027');
    expect(quarterLabel(quarterStart(today, -4))).toBe('Q4 2025');
    expect(quarterEnd(quarterStart(today, 0)).toISOString().slice(0, 10)).toBe('2026-12-31');
  });

  it('lists every ISO week overlapping a quarter, straddling weeks included', () => {
    const columns = isoWeeksBetween('2026-10-01', '2026-12-31');

    expect(columns[0]).toEqual({ key: '2026-W40', isoYear: 2026, week: 40, start: '2026-09-28', end: '2026-10-04' });
    expect(columns.at(-1)!.key).toBe('2026-W53');
    expect(columns).toHaveLength(14);
  });

  it('treats ranges touching a week boundary as overlapping', () => {
    const [week] = isoWeeksBetween('2026-11-02', '2026-11-02');

    expect(overlaps('2026-10-20', '2026-11-02', week)).toBe(true);
    expect(overlaps('2026-11-08', '2026-11-20', week)).toBe(true);
    expect(overlaps('2026-11-09', '2026-11-20', week)).toBe(false);
  });
});
