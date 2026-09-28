import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { KanbanBoardsPageComponent } from './kanban-boards-page.component';
import { BoardService } from '../../core/api/board.service';
import { BoardDto } from '../../core/api/board.model';

function createFakeBoardService(boards: BoardDto[]) {
  const reload = jest.fn();
  return {
    boardsResource: () => ({
      value: signal<BoardDto[]>(boards),
      isLoading: signal(false),
      error: signal(undefined),
      reload
    }),
    create: jest.fn(),
    // Not exercised directly here (selecting a board would render <chronos-kanban-board>, which
    // needs this), but present so an incidental extra change-detection pass doesn't blow up.
    boardDetailResource: () => ({ value: signal(undefined), isLoading: signal(false), error: signal(undefined), reload: jest.fn() }),
    reloadSpy: reload
  };
}

describe('KanbanBoardsPageComponent', () => {
  function setup(fakeService: ReturnType<typeof createFakeBoardService>) {
    TestBed.configureTestingModule({
      imports: [KanbanBoardsPageComponent],
      providers: [{ provide: BoardService, useValue: fakeService }]
    });

    const fixture = TestBed.createComponent(KanbanBoardsPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('creates a board without a project id, reloads, and selects the new board', () => {
    const fakeService = createFakeBoardService([]);
    fakeService.create.mockReturnValue(of('board-9'));
    const fixture = setup(fakeService);
    fixture.componentInstance['newBoardName'].set('Marketing');

    fixture.componentInstance.onCreateBoard(new Event('submit'));

    expect(fakeService.create).toHaveBeenCalledWith({ name: 'Marketing', projectId: null });
    expect(fakeService.reloadSpy).toHaveBeenCalled();
    expect(fixture.componentInstance['selectedBoardId']()).toBe('board-9');
  });

  it('does not submit an empty board name', () => {
    const fakeService = createFakeBoardService([]);
    const fixture = setup(fakeService);

    fixture.componentInstance.onCreateBoard(new Event('submit'));

    expect(fakeService.create).not.toHaveBeenCalled();
  });

  it('surfaces the server error on failed creation', () => {
    const fakeService = createFakeBoardService([]);
    fakeService.create.mockReturnValue(throwError(() => ({ error: 'Board limit reached' })));
    const fixture = setup(fakeService);
    fixture.componentInstance['newBoardName'].set('Too many');

    fixture.componentInstance.onCreateBoard(new Event('submit'));

    expect(fixture.componentInstance['error']()).toBe('Board limit reached');
  });
});
