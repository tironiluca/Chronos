import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { KanbanBoardComponent } from './kanban-board.component';
import { BoardService } from '../../core/api/board.service';
import { UserService } from '../../core/api/user.service';
import { BoardDetailDto } from '../../core/api/board.model';
import { ProjectService } from '../../core/api/project.service';
import { createFakeProjectService, makeProject, makeTask } from '../../testing/fake-project-service';
import { createFakeUserService } from '../../testing/fake-user-service';

const board: BoardDetailDto = {
  id: 'board-1',
  organizationId: 'org-1',
  name: 'Sprint board',
  projectId: null,
  columns: [
    { id: 'col-1', boardId: 'board-1', name: 'To do', order: 0, cards: [{ id: 'card-1', columnId: 'col-1', title: 'Task A', assignedUserId: null, ganttTaskId: null }] }
  ]
};

function createFakeBoardService(boardValue: BoardDetailDto = board) {
  const reload = jest.fn();
  return {
    boardDetailResource: () => ({
      value: signal<BoardDetailDto | undefined>(boardValue),
      isLoading: signal(false),
      error: signal(undefined),
      reload
    }),
    createColumn: jest.fn().mockReturnValue(of('col-2')),
    createCard: jest.fn().mockReturnValue(of('card-2')),
    assignCardUser: jest.fn().mockReturnValue(of(undefined)),
    moveCard: jest.fn().mockReturnValue(of(undefined)),
    reloadSpy: reload
  };
}

describe('KanbanBoardComponent', () => {
  function setup(
    fakeBoardService: ReturnType<typeof createFakeBoardService>,
    fakeUserService: Partial<Record<keyof UserService, unknown>> = createFakeUserService(),
    fakeProjectService = createFakeProjectService()
  ) {
    TestBed.configureTestingModule({
      imports: [KanbanBoardComponent],
      providers: [
        { provide: BoardService, useValue: fakeBoardService },
        { provide: UserService, useValue: fakeUserService },
        { provide: ProjectService, useValue: fakeProjectService },
        provideRouter([])
      ]
    });

    const fixture = TestBed.createComponent(KanbanBoardComponent);
    fixture.componentRef.setInput('boardId', 'board-1');
    fixture.detectChanges();
    return fixture;
  }

  it('creates a column with the next order index and reloads', () => {
    const fakeBoardService = createFakeBoardService();
    const fixture = setup(fakeBoardService);
    fixture.componentInstance['newColumnName'].set('Done');

    fixture.componentInstance.onCreateColumn(new Event('submit'), board);

    expect(fakeBoardService.createColumn).toHaveBeenCalledWith('board-1', { name: 'Done', order: 1 });
    expect(fakeBoardService.reloadSpy).toHaveBeenCalled();
  });

  it('offers every directory user in each card\'s assignee picker', () => {
    const fixture = setup(createFakeBoardService());

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select[aria-label="Assignee for Task A"]');
    expect(Array.from(select.options).map((o) => o.textContent?.trim())).toEqual(['Unassigned', 'Anna', 'Bruno']);
  });

  it('assigns the picked user and reloads', () => {
    const fakeBoardService = createFakeBoardService();
    const fixture = setup(fakeBoardService);

    fixture.componentInstance.assign('board-1', 'col-1', 'card-1', 'user-2');

    expect(fakeBoardService.assignCardUser).toHaveBeenCalledWith('board-1', 'col-1', 'card-1', { userId: 'user-2' });
    expect(fakeBoardService.reloadSpy).toHaveBeenCalled();
  });

  it('unassigns a card by sending a null userId', () => {
    const fakeBoardService = createFakeBoardService();
    const fixture = setup(fakeBoardService);

    fixture.componentInstance.assign('board-1', 'col-1', 'card-1', null);

    expect(fakeBoardService.assignCardUser).toHaveBeenCalledWith('board-1', 'col-1', 'card-1', { userId: null });
  });

  it('surfaces the server error when assignment fails', () => {
    const fakeBoardService = createFakeBoardService();
    fakeBoardService.assignCardUser.mockReturnValue(throwError(() => ({ error: "User 'x' was not found." })));
    const fixture = setup(fakeBoardService);

    fixture.componentInstance.assign('board-1', 'col-1', 'card-1', 'user-1');

    expect(fixture.componentInstance['actionError']()).toBe("User 'x' was not found.");
  });

  describe('when the board is linked to a project', () => {
    const linkedTask = makeTask({ id: 'task-1', name: 'Install PLC', startDate: '2026-01-05', endDate: '2026-01-10' });
    const project = makeProject({ id: 'project-1', code: 'L3U', tasks: [linkedTask] });
    const linkedBoard: BoardDetailDto = {
      ...board,
      projectId: 'project-1',
      columns: [
        {
          ...board.columns[0],
          cards: [{ id: 'card-1', columnId: 'col-1', title: 'Task A', assignedUserId: null, ganttTaskId: 'task-1' }]
        }
      ]
    };

    it('links to the project Gantt page and shows the linked task name on the card', () => {
      const fixture = setup(createFakeBoardService(linkedBoard), createFakeUserService(), createFakeProjectService([project]));
      const element: HTMLElement = fixture.nativeElement;

      const links = Array.from(element.querySelectorAll('a')).map((a) => a.textContent?.trim());
      expect(links).toContain('Gantt: L3U ›');
      expect(links).toContain('↳ Install PLC');
    });

    it('creates a card linked to the selected Gantt task', () => {
      const fakeBoardService = createFakeBoardService(linkedBoard);
      const fixture = setup(fakeBoardService, createFakeUserService(), createFakeProjectService([project]));
      fixture.componentInstance.setNewCardTitle('col-1', 'Wire cabinet');
      fixture.componentInstance.setNewCardTask('col-1', 'task-1');

      fixture.componentInstance.onCreateCard(new Event('submit'), 'board-1', 'col-1');

      expect(fakeBoardService.createCard).toHaveBeenCalledWith('board-1', 'col-1', { title: 'Wire cabinet', ganttTaskId: 'task-1' });
      expect(fixture.componentInstance['newCardTaskIds']()['col-1']).toBe('');
    });
  });

  it('does not fetch a project or offer task linking for a standalone board', () => {
    const fakeProjectService = createFakeProjectService();
    const fixture = setup(createFakeBoardService(), createFakeUserService(), fakeProjectService);

    expect(fakeProjectService.requestedIds[0]()).toBeNull();
    expect(fixture.nativeElement.querySelector('select[aria-label="Linked Gantt task"]')).toBeNull();
  });

  it('creates an unlinked card when no task is selected', () => {
    const fakeBoardService = createFakeBoardService();
    const fixture = setup(fakeBoardService);
    fixture.componentInstance.setNewCardTitle('col-1', 'Loose card');

    fixture.componentInstance.onCreateCard(new Event('submit'), 'board-1', 'col-1');

    expect(fakeBoardService.createCard).toHaveBeenCalledWith('board-1', 'col-1', { title: 'Loose card', ganttTaskId: null });
  });

  describe('moving cards', () => {
    const twoColumnBoard: BoardDetailDto = {
      ...board,
      columns: [...board.columns, { id: 'col-2', boardId: 'board-1', name: 'Done', order: 1, cards: [] }]
    };

    function dragEvent(type: string): DragEvent {
      const event = new Event(type, { bubbles: true, cancelable: true }) as DragEvent;
      Object.defineProperty(event, 'dataTransfer', { value: { setData: jest.fn(), effectAllowed: 'none' } });
      return event;
    }

    it('offers only the other columns as move targets', () => {
      const fixture = setup(createFakeBoardService(twoColumnBoard));

      const select: HTMLSelectElement = fixture.nativeElement.querySelector('select[aria-label="Move Task A"]');
      expect(Array.from(select.options).map((o) => o.textContent?.trim())).toEqual(['Move to…', 'Done']);
    });

    it('hides the move picker on a single-column board', () => {
      const fixture = setup(createFakeBoardService());

      expect(fixture.nativeElement.querySelector('select[aria-label="Move Task A"]')).toBeNull();
    });

    it('moves a card picked from the select and reloads', () => {
      const fakeBoardService = createFakeBoardService(twoColumnBoard);
      const fixture = setup(fakeBoardService);
      const select: HTMLSelectElement = fixture.nativeElement.querySelector('select[aria-label="Move Task A"]');

      select.value = 'col-2';
      select.dispatchEvent(new Event('change'));

      expect(fakeBoardService.moveCard).toHaveBeenCalledWith('board-1', 'col-1', 'card-1', { targetColumnId: 'col-2' });
      expect(fakeBoardService.reloadSpy).toHaveBeenCalled();
      expect(select.value).toBe('');
    });

    it('moves a card dropped onto another column', () => {
      const fakeBoardService = createFakeBoardService(twoColumnBoard);
      const fixture = setup(fakeBoardService);
      const element: HTMLElement = fixture.nativeElement;
      const [, doneColumn] = Array.from(element.querySelectorAll<HTMLElement>('.kanban-column'));

      element.querySelector('.kanban-card')!.dispatchEvent(dragEvent('dragstart'));
      const over = dragEvent('dragover');
      doneColumn.dispatchEvent(over);
      fixture.detectChanges();
      expect(over.defaultPrevented).toBe(true);
      expect(doneColumn.classList).toContain('kanban-column-drop');

      doneColumn.dispatchEvent(dragEvent('drop'));

      expect(fakeBoardService.moveCard).toHaveBeenCalledWith('board-1', 'col-1', 'card-1', { targetColumnId: 'col-2' });
    });

    it('does not accept a drop back onto the card\'s own column', () => {
      const fakeBoardService = createFakeBoardService(twoColumnBoard);
      const fixture = setup(fakeBoardService);
      const element: HTMLElement = fixture.nativeElement;
      const [todoColumn] = Array.from(element.querySelectorAll<HTMLElement>('.kanban-column'));

      element.querySelector('.kanban-card')!.dispatchEvent(dragEvent('dragstart'));
      const over = dragEvent('dragover');
      todoColumn.dispatchEvent(over);
      todoColumn.dispatchEvent(dragEvent('drop'));

      expect(over.defaultPrevented).toBe(false);
      expect(fakeBoardService.moveCard).not.toHaveBeenCalled();
    });

    it('surfaces the server error when a move fails', () => {
      const fakeBoardService = createFakeBoardService(twoColumnBoard);
      fakeBoardService.moveCard.mockReturnValue(throwError(() => ({ error: "Column 'x' was not found." })));
      const fixture = setup(fakeBoardService);

      fixture.componentInstance.moveCard('board-1', 'col-1', 'card-1', 'col-2');

      expect(fixture.componentInstance['actionError']()).toBe("Column 'x' was not found.");
    });
  });
});
