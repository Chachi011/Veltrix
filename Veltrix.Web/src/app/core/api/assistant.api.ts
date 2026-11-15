import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SILENT_ERRORS, SKIP_LOADING } from '../http/http-context';
import { AssistantMessageDto, AssistantReplyDto, AssistantStatusDto } from '../models/api.models';

/** The AI assistant chat (Player only). The answers come from an n8n workflow on the server side. */
@Injectable({ providedIn: 'root' })
export class AssistantApi {
  private readonly http = inject(HttpClient);

  /** Whether the AI is set up at all (an admin connected the n8n workflow). */
  status(): Observable<AssistantStatusDto> {
    return this.http.get<AssistantStatusDto>('/api/assistant/status');
  }

  /** My conversation, oldest first (the latest 50 messages). */
  history(): Observable<AssistantMessageDto[]> {
    return this.http.get<AssistantMessageDto[]>('/api/assistant/messages');
  }

  /**
   * Asks the assistant. It can take several seconds, so the drawer shows its own "thinking" indicator instead of
   * the top loading bar. Errors (503 when the AI is down or not configured, 429 after 10 questions a minute) are
   * shown inside the drawer, next to the input box, so no toast.
   */
  ask(message: string): Observable<AssistantReplyDto> {
    return this.http.post<AssistantReplyDto>(
      '/api/assistant/messages',
      { message },
      { context: new HttpContext().set(SKIP_LOADING, true).set(SILENT_ERRORS, true) }
    );
  }

  /** Deletes my whole conversation. */
  clear(): Observable<void> {
    return this.http.delete<void>('/api/assistant/messages');
  }
}
