import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_LOADING } from '../http/http-context';
import { AssistantMessageDto, AssistantReplyDto } from '../models/api.models';

/** The AI assistant chat (Player only). The answers come from an n8n workflow on the server side. */
@Injectable({ providedIn: 'root' })
export class AssistantApi {
  private readonly http = inject(HttpClient);

  /** My conversation, oldest first (the latest 50 messages). */
  history(): Observable<AssistantMessageDto[]> {
    return this.http.get<AssistantMessageDto[]>('/api/assistant/messages');
  }

  /**
   * Asks the assistant. It can take several seconds, so the drawer shows its own "thinking" indicator instead of
   * the top loading bar. Errors (503 when the AI is down, 429 after 10 questions a minute) still show a toast.
   */
  ask(message: string): Observable<AssistantReplyDto> {
    return this.http.post<AssistantReplyDto>(
      '/api/assistant/messages',
      { message },
      { context: new HttpContext().set(SKIP_LOADING, true) }
    );
  }

  /** Deletes my whole conversation. */
  clear(): Observable<void> {
    return this.http.delete<void>('/api/assistant/messages');
  }
}
