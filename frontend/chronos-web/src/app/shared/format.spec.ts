import { dayCount, formatDateRange } from './format';

describe('format helpers', () => {
  it('counts calendar days inclusively', () => {
    expect(dayCount('2026-01-05', '2026-01-05')).toBe(1);
    expect(dayCount('2026-01-05', '2026-01-09')).toBe(5);
    expect(dayCount('2026-03-27', '2026-03-30')).toBe(4); // across the EU DST switch
  });

  it('shows the year once for same-year ranges and on both ends across years', () => {
    const sameYear = formatDateRange('2026-01-05', '2026-01-09');
    expect(sameYear.match(/2026/g)).toHaveLength(1);
    const crossYear = formatDateRange('2026-12-30', '2027-01-02');
    expect(crossYear).toContain('2026');
    expect(crossYear).toContain('2027');
  });

  it('collapses a one-day range to a single date', () => {
    expect(formatDateRange('2026-01-05', '2026-01-05')).not.toContain('–');
  });
});
