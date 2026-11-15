# AI assistant with n8n

Players can ask an AI assistant about the game, VLX, provably fair rounds and the responsible play tools
(the ✨ button in the top bar, players only). VELTRIX does not call an AI model itself: it posts each question to an
**n8n workflow**, and the workflow talks to the model.

```
Angular drawer ──POST /api/assistant/messages──▶ VELTRIX API ──POST webhook (X-Veltrix-Secret)──▶ n8n ──▶ AI model
       ▲                                         │  builds the rules (system prompt),                    │
       └──────── question + answer ◀─────────────┘  saves question + answer in SQL Server ◀── { reply } ─┘
```

## Why this split (exam)

- **The rules stay in the code.** `AssistantPrompts.cs` writes the instructions for the model: only VELTRIX topics,
  no betting strategies or crash-point predictions, never an invitation to play more, and nothing that invites play
  while the player cannot bet (`canPlayNow` from the limits, the same rule as the rest of the app). They are tested
  (`AssistantClientTests`, `AssistantServiceTests`), so nobody can weaken them by editing the workflow.
- **The AI provider is replaceable without code.** OpenAI, Anthropic or a local Ollama model: change one node in n8n.
- **The database is the source of truth.** The API sends the recent conversation with every question, so the
  workflow keeps no state, and a player sees the same chat after a reload or on another device.
- **Failures are safe.** If n8n is down, slow (timeout) or answers badly, the player gets one friendly message (503)
  in the chat panel, nothing is saved and the question stays in the input box.
- **Cost and abuse:** players only, 10 questions per minute per player (rate limit `assistant`), 500 characters per question.

## Setup (once per machine)

### 1. Run n8n

Either with Node.js 20+:

```bash
npx n8n
```

or with Docker:

```bash
docker run -it --rm -p 5678:5678 -v n8n_data:/home/node/.n8n docker.n8n.io/n8nio/n8n
```

Open http://localhost:5678 and create the owner account.

### 2. Import the workflow

**Workflows → Create → ⋯ → Import from File** → `docs/n8n/veltrix-assistant.json`.

It has five nodes: *Question from VELTRIX* (Webhook) → *Build the prompt* (Code) → *AI Agent* (with *Chat model*) →
*Answer to VELTRIX* (Respond to Webhook).

### 3. Credentials

1. **Question from VELTRIX** → *Credential for Header Auth* → *Create new*:
   - Name: `X-Veltrix-Secret`
   - Value: a long random string (for example the output of `openssl rand -hex 32`). You need it again in step 5.
2. **Chat model** → add your OpenAI API key.
   To use another provider, delete *Chat model* and click **+ Chat Model** under the AI Agent:
   *Anthropic Chat Model* (Claude, API key from console.anthropic.com) or *Ollama Chat Model* (free, local; run
   `ollama pull llama3.1` first). Keep the temperature low (0.2–0.4) so answers stay factual.

### 4. Activate

Turn the workflow **Active** (top right). The Production URL of the Webhook node is now
`http://localhost:5678/webhook/veltrix-assistant`.

### 5. Tell VELTRIX where the workflow is

The URL and the secret are machine-specific, so they go in user-secrets (never in Git):

```bash
dotnet user-secrets set "Assistant:WebhookUrl" "http://localhost:5678/webhook/veltrix-assistant" --project Veltrix.API
dotnet user-secrets set "Assistant:Secret" "<the value from step 3>" --project Veltrix.API
```

Optional (`appsettings.json` → `Assistant`): `TimeoutSeconds` (default 30) and `HistoryMessages` (how many earlier
messages go with a question, default 10).

Without a `WebhookUrl` the app still runs; the chat panel then says "The assistant is not configured" and its input
box is disabled (`GET /api/assistant/status` returns `{ "configured": false }`).

## The contract

VELTRIX → n8n (`POST`, header `X-Veltrix-Secret`):

```json
{
  "sessionId": "veltrix-user-42",
  "language": "ka",
  "systemPrompt": "You are the help assistant of VELTRIX ...",
  "history": [
    { "role": "user", "text": "What is provably fair?" },
    { "role": "assistant", "text": "Before betting the server publishes ..." }
  ],
  "message": "And how do I check a round?"
}
```

n8n → VELTRIX: `{ "reply": "..." }` (the client also accepts the AI Agent's own `{ "output": "..." }`, an array of
those, or plain text).

## Test it

1. Log in as `player1@veltrix.dev` / `Player123!`, click ✨ in the top bar and ask *"How does Crash work?"*.
2. Ask *"Give me a strategy to win"*: the assistant must refuse and explain the 3% house edge.
3. On **Responsible play**, start a 24-hour self-exclusion, then ask *"When can I play again?"*: the answer must not
   invite you to play.
4. Stop n8n and ask again: a red note in the chat panel says the assistant is not available, and your question stays
   in the box.
5. In n8n, **Executions** shows every call (useful in the demo to show what the model received).
