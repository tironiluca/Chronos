export interface BoardDto {
  id: string;
  organizationId: string;
  name: string;
  projectId: string | null;
}

export interface KanbanCardDto {
  id: string;
  columnId: string;
  title: string;
  assignedUserId: string | null;
  ganttTaskId: string | null;
}

export interface KanbanColumnDto {
  id: string;
  boardId: string;
  name: string;
  order: number;
  cards: KanbanCardDto[];
}

export interface BoardDetailDto {
  id: string;
  organizationId: string;
  name: string;
  projectId: string | null;
  columns: KanbanColumnDto[];
}

// organizationId is NOT sent -- the server derives it from the caller's JWT, same convention
// as everywhere else (see BoardEndpoints/CreateBoardCommand).
export interface CreateBoardPayload {
  name: string;
  projectId: string | null;
}

export interface CreateKanbanColumnPayload {
  name: string;
  order: number;
}

export interface CreateKanbanCardPayload {
  title: string;
  ganttTaskId: string | null;
}

export interface AssignKanbanCardUserPayload {
  userId: string | null;
}
