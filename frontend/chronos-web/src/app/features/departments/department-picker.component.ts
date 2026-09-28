import { Component, computed, inject, input, output } from '@angular/core';
import { DepartmentService } from '../../core/api/department.service';
import { DepartmentDto } from '../../core/api/department.model';

interface DepartmentOption {
  id: string;
  name: string;
  depth: number;
}

// Flattens the department tree (parentDepartmentId) into a depth-first, indentation-ready list
// for a plain <select> -- reused wherever a department needs picking (department management's
// "parent department" field, the resource-availability calendar's department filter).
function flattenTree(departments: DepartmentDto[]): DepartmentOption[] {
  const byParent = new Map<string | null, DepartmentDto[]>();
  for (const department of departments) {
    const siblings = byParent.get(department.parentDepartmentId) ?? [];
    siblings.push(department);
    byParent.set(department.parentDepartmentId, siblings);
  }

  const options: DepartmentOption[] = [];
  const visit = (parentId: string | null, depth: number) => {
    for (const department of byParent.get(parentId) ?? []) {
      options.push({ id: department.id, name: department.name, depth });
      visit(department.id, depth + 1);
    }
  };
  visit(null, 0);
  return options;
}

@Component({
  selector: 'chronos-department-picker',
  standalone: true,
  template: `
    <select [value]="selectedId() ?? ''" (change)="onChange($any($event.target).value)">
      @if (includeAllOption()) {
        <option value="">{{ allOptionLabel() }}</option>
      }
      @for (option of options(); track option.id) {
        <option [value]="option.id">{{ '  '.repeat(option.depth) }}{{ option.name }}</option>
      }
    </select>
    @if (resource.isLoading()) {
      <span class="empty-state">Loading departments...</span>
    }
    @if (resource.error()) {
      <span class="error">Could not load departments.</span>
    }
  `
})
export class DepartmentPickerComponent {
  selectedId = input<string | null>(null);
  includeAllOption = input(true);
  allOptionLabel = input('All departments');
  selectionChange = output<string | null>();

  private readonly departmentService = inject(DepartmentService);
  protected readonly resource = this.departmentService.departmentsResource();
  protected readonly options = computed(() => flattenTree(this.resource.value() ?? []));

  onChange(value: string): void {
    this.selectionChange.emit(value === '' ? null : value);
  }
}
