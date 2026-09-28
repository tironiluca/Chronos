import { TestBed } from '@angular/core/testing';
import { LeaveGridComponent } from './leave-grid.component';
import { ResourceAvailabilityDto } from '../../core/api/resource-availability.model';

function entry(overrides: Partial<ResourceAvailabilityDto>): ResourceAvailabilityDto {
  return {
    userId: 'user-1',
    displayName: 'Ada Lovelace',
    departmentId: null,
    approvedLeave: [],
    assignedTasks: [],
    assignedCards: [],
    ...overrides
  };
}

describe('LeaveGridComponent', () => {
  function setup(availability: ResourceAvailabilityDto[], monthStart: string) {
    TestBed.configureTestingModule({ imports: [LeaveGridComponent] });
    const fixture = TestBed.createComponent(LeaveGridComponent);
    fixture.componentRef.setInput('availability', availability);
    fixture.componentRef.setInput('monthStart', monthStart);
    fixture.detectChanges();
    return fixture;
  }

  it('renders one column per day of the given month', () => {
    const fixture = setup([], '2026-02-01');

    const headerCells = fixture.nativeElement.querySelectorAll('thead th');
    // 28 days in Feb 2026 (not a leap year) + the leading "Employee" column.
    expect(headerCells.length).toBe(29);
  });

  it('marks only the days covered by an approved leave range', () => {
    const ada = entry({
      userId: 'user-1',
      displayName: 'Ada Lovelace',
      approvedLeave: [{ id: 'leave-1', startDate: '2026-03-10', endDate: '2026-03-12', type: 'Vacation' }]
    });
    const fixture = setup([ada], '2026-03-01');

    const cells: HTMLTableCellElement[] = Array.from(fixture.nativeElement.querySelectorAll('tbody td'));
    const onLeave = cells.filter((cell) => cell.classList.contains('on-leave'));

    expect(onLeave.length).toBe(3);
  });

  it('clips a leave range that extends outside the visible month to only the rendered days', () => {
    const ada = entry({
      approvedLeave: [{ id: 'leave-1', startDate: '2026-02-27', endDate: '2026-03-02', type: 'Vacation' }]
    });
    const fixture = setup([ada], '2026-03-01');

    const cells: HTMLTableCellElement[] = Array.from(fixture.nativeElement.querySelectorAll('tbody td'));
    const onLeave = cells.filter((cell) => cell.classList.contains('on-leave'));

    // Only March 1-2 fall within the rendered March grid; Feb 27-28 are out of range.
    expect(onLeave.length).toBe(2);
  });

  it('shows a placeholder row when no employees are in scope', () => {
    const fixture = setup([], '2026-03-01');

    expect(fixture.nativeElement.textContent).toContain('No employees in scope.');
  });
});
