import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'projects/:projectId/gantt',
    loadComponent: () =>
      import('./features/gantt/gantt-chart.component').then((m) => m.GanttChartComponent)
  }
];
