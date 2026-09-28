import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'leave'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: 'register-organization',
    loadComponent: () =>
      import('./features/organizations/register-organization.component').then(
        (m) => m.RegisterOrganizationComponent
      )
  },
  {
    path: 'leave',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/leave/leave-requests-page.component').then((m) => m.LeaveRequestsPageComponent)
  },
  {
    path: 'projects/:projectId/gantt',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/gantt/gantt-chart.component').then((m) => m.GanttChartComponent)
  },
  {
    path: 'departments',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/departments/department-management-page.component').then(
        (m) => m.DepartmentManagementPageComponent
      )
  },
  {
    path: 'boards',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/kanban/kanban-boards-page.component').then((m) => m.KanbanBoardsPageComponent)
  },
  {
    path: 'availability',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/availability/availability-calendar-page.component').then(
        (m) => m.AvailabilityCalendarPageComponent
      )
  },
  {
    path: '**',
    redirectTo: 'leave'
  }
];
