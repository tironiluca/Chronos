import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { KanbanBoardComponent } from './kanban-board.component';
import { BoardService } from '../../core/api/board.service';
import { UserService } from '../../core/api/user.service';
import { BoardDetailDto } from '../../core/api/board.model';
import { UserDto } from '../../core/api/user.model';

const board: BoardDetailDto = {
  id: 'board-1',
  organizationId: 'org-1',
  name: 'Sprint board',
  projectId: null,
  columns: [
    { id: 'col-1', boardId: 'board-1', name: 'To do', order: 0, cards: [{ id: 'card-1', columnId: 'col-1', title: 'Task A', assignedUserId: null, ganttTaskId: null }] }
  ]
};

function createFakeBoardService() {
  const reload = jest.fn();
  return {
    boardDetailResource: () => ({
      value: signal<BoardDetailDto | undefined>(board),
      isLoading: signal(false),
      error: signal(undefined),
      reload
    }),
    createColumn: jest.fn().mockReturnValue(of('col-2')),
    createCard: jest.fn().mockReturnValue(of('card-2')),
    assignCardUser: jest.fn().mockReturnValue(of(undefined)),
    reloadSpy: reload
  };
}

const foundUser: UserDto = { id: 'user-1', email: 'a@org.com', displayName: 'A', role: 'Employee', departmentId: null };

describe('KanbanBoardComponent', () => {
  function setup(fakeBoardService: ReturnType<typeof createFakeBoardService>, fakeUserService: Partial<UserService> = {}) {
    TestBed.configureTestingModule({
      imports: [KanbanBoardComponent],
      providers: [
        { provide: BoardService, useValue: fakeBoardService },
        { provide: UserService, useValue: fakeUserService }
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

  it('resolves the assignee email to a user id before assigning', () => {
    const fakeBoardService = createFakeBoardService();
    const fakeUserService = { getByEmail: jest.fn().mockReturnValue(of(foundUser)) };
    const fixture = setup(fakeBoardService, fakeUserService);
    fixture.componentInstance['assigneeEmail'].set('a@org.com');

    fixture.componentInstance.confirmAssign('board-1', 'col-1', 'card-1');

    expect(fakeUserService.getByEmail).toHaveBeenCalledWith('a@org.com');
    expect(fakeBoardService.assignCardUser).toHaveBeenCalledWith('board-1', 'col-1', 'card-1', { userId: 'user-1' });
  });

  it('surfaces an error when the email does not resolve to a user', () => {
    const fakeBoardService = createFakeBoardService();
    const fakeUserService = { getByEmail: jest.fn().mockReturnValue(throwError(() => ({ error: 'not found' }))) };
    const fixture = setup(fakeBoardService, fakeUserService);
    fixture.componentInstance['assigneeEmail'].set('missing@org.com');

    fixture.componentInstance.confirmAssign('board-1', 'col-1', 'card-1');

    expect(fixture.componentInstance['actionError']()).toBe('not found');
    expect(fakeBoardService.assignCardUser).not.toHaveBeenCalled();
  });

  it('unassigns a card by sending a null userId', () => {
    const fakeBoardService = createFakeBoardService();
    const fixture = setup(fakeBoardService);

    fixture.componentInstance.unassign('board-1', 'col-1', 'card-1');

    expect(fakeBoardService.assignCardUser).toHaveBeenCalledWith('board-1', 'col-1', 'card-1', { userId: null });
  });
});
