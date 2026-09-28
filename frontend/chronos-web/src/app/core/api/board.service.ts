import { Injectable, inject } from '@angular/core';
import { HttpClient, httpResource } from '@angular/common/http';
import {
  AssignKanbanCardUserPayload,
  BoardDetailDto,
  BoardDto,
  CreateBoardPayload,
  CreateKanbanCardPayload,
  CreateKanbanColumnPayload
} from './board.model';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private readonly http = inject(HttpClient);

  // Always the caller's own organization -- same convention as LeaveService/DepartmentService.
  boardsResource() {
    return httpResource<BoardDto[]>(() => '/api/boards');
  }

  // boardId() returning '' means "no board selected" -- httpResource skips the request when its
  // reactive source function returns undefined, so a falsy id is mapped to undefined here.
  boardDetailResource(boardId: () => string | null) {
    return httpResource<BoardDetailDto>(() => {
      const id = boardId();
      return id ? `/api/boards/${id}` : undefined;
    });
  }

  create(payload: CreateBoardPayload) {
    return this.http.post<string>('/api/boards', payload);
  }

  createColumn(boardId: string, payload: CreateKanbanColumnPayload) {
    return this.http.post<string>(`/api/boards/${boardId}/columns`, payload);
  }

  createCard(boardId: string, columnId: string, payload: CreateKanbanCardPayload) {
    return this.http.post<string>(`/api/boards/${boardId}/columns/${columnId}/cards`, payload);
  }

  assignCardUser(boardId: string, columnId: string, cardId: string, payload: AssignKanbanCardUserPayload) {
    return this.http.patch<void>(`/api/boards/${boardId}/columns/${columnId}/cards/${cardId}/assignee`, payload);
  }
}
