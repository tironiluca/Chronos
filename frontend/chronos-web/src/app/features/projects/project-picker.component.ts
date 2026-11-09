import { Component, inject, input, output } from '@angular/core';
import { ProjectService } from '../../core/api/project.service';

// Plain <select> over the caller's org projects -- reused by the availability calendar's project
// filter and the Kanban board-creation form (optional linked project). Mirrors
// DepartmentPickerComponent's inputs/outputs so the two read the same at call sites.
@Component({
  selector: 'chronos-project-picker',
  standalone: true,
  template: `
    <!-- [selected] per option, not [value] on <select>: the select's value binding is applied
         before @for renders its options, so a preselected id would silently not stick. -->
    <select (change)="onChange($any($event.target).value)">
      @if (includeAllOption()) {
        <option value="" [selected]="!selectedId()">{{ allOptionLabel() }}</option>
      }
      @for (project of resource.value() ?? []; track project.id) {
        <option [value]="project.id" [selected]="project.id === selectedId()">{{ project.code }} — {{ project.name }}</option>
      }
    </select>
    @if (resource.isLoading()) {
      <span class="empty-state">Loading projects...</span>
    }
    @if (resource.error()) {
      <span class="error">Could not load projects.</span>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 4px;
      min-width: 0;
    }

    select {
      width: 100%;
    }
  `
})
export class ProjectPickerComponent {
  selectedId = input<string | null>(null);
  includeAllOption = input(true);
  allOptionLabel = input('All projects');
  selectionChange = output<string | null>();

  private readonly projectService = inject(ProjectService);
  protected readonly resource = this.projectService.projectsResource();

  onChange(value: string): void {
    this.selectionChange.emit(value === '' ? null : value);
  }
}
