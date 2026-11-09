import { Component, input, output } from '@angular/core';
import { UserDirectoryEntryDto } from '../../core/api/user.model';

// Presentational assignee <select>: the parent fetches the directory once (UserService.
// directoryResource) and passes it in, so a board with N cards or a project with N tasks makes one
// request, not N. Emits null for "Unassigned".
@Component({
  selector: 'chronos-user-picker',
  standalone: true,
  template: `
    <!-- [selected] per option, not [value] on <select>: the select's value binding is applied
         before @for renders its options, so a preselected id would silently not stick. -->
    <select [attr.aria-label]="ariaLabel()" (change)="onChange($any($event.target).value)">
      <option value="" [selected]="!selectedId()">{{ emptyLabel() }}</option>
      @for (user of users(); track user.id) {
        <option [value]="user.id" [selected]="user.id === selectedId()">{{ user.displayName }}</option>
      }
    </select>
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }

    select {
      width: 100%;
    }
  `
})
export class UserPickerComponent {
  users = input.required<UserDirectoryEntryDto[]>();
  selectedId = input<string | null>(null);
  emptyLabel = input('Unassigned');
  ariaLabel = input('Assignee');
  selectionChange = output<string | null>();

  onChange(value: string): void {
    this.selectionChange.emit(value === '' ? null : value);
  }
}
