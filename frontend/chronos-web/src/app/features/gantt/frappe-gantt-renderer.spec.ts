import { frappeEndToIsoDate, toNearestIsoDate } from './frappe-gantt-renderer';

// Local-time Date constructors on purpose: Frappe works in the browser's local time.
describe('Frappe date conversion', () => {
  it('maps a midnight start to its own date', () => {
    expect(toNearestIsoDate(new Date(2026, 1, 2, 0, 0, 0))).toBe('2026-02-02');
  });

  it('rounds DST-shifted starts to the nearest midnight', () => {
    expect(toNearestIsoDate(new Date(2026, 1, 1, 23, 0, 0))).toBe('2026-02-02');
    expect(toNearestIsoDate(new Date(2026, 1, 2, 1, 0, 0))).toBe('2026-02-02');
  });

  it('converts Frappe\'s exclusive-end-minus-one-second back to an inclusive end date', () => {
    // A bar covering 2026-02-02..2026-02-06 inclusive ends (exclusively) at 2026-02-07 00:00.
    expect(frappeEndToIsoDate(new Date(2026, 1, 6, 23, 59, 59))).toBe('2026-02-06');
  });

  it('keeps a one-day bar as a single day', () => {
    expect(frappeEndToIsoDate(new Date(2026, 1, 2, 23, 59, 59))).toBe('2026-02-02');
  });
});
