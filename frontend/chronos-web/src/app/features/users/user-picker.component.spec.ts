import { TestBed } from '@angular/core/testing';
import { UserPickerComponent } from './user-picker.component';
import { directoryUsers } from '../../testing/fake-user-service';

describe('UserPickerComponent', () => {
  function setup(selectedId: string | null = null) {
    TestBed.configureTestingModule({ imports: [UserPickerComponent] });
    const fixture = TestBed.createComponent(UserPickerComponent);
    fixture.componentRef.setInput('users', directoryUsers);
    fixture.componentRef.setInput('selectedId', selectedId);
    fixture.detectChanges();
    return fixture;
  }

  it('renders an "Unassigned" option followed by each user, preselecting the current one', () => {
    const fixture = setup('user-2');
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select');

    expect(Array.from(select.options).map((o) => o.textContent?.trim())).toEqual(['Unassigned', 'Anna', 'Bruno']);
    expect(select.value).toBe('user-2');
  });

  it('emits null for "Unassigned" and the id otherwise', () => {
    const fixture = setup();
    const emitted: (string | null)[] = [];
    fixture.componentInstance.selectionChange.subscribe((value) => emitted.push(value));

    fixture.componentInstance.onChange('user-1');
    fixture.componentInstance.onChange('');

    expect(emitted).toEqual(['user-1', null]);
  });
});
