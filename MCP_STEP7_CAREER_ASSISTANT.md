# Step 7: career assistant chatbot (streaming, read-only)

A student chats with an assistant that can read their profile, matches and skill gaps and talk about them.
**It can only read and talk.** It cannot apply, edit the profile, upload or send anything. The reply streams word by
word (Server-Sent Events) so the student does not wait for the whole answer.

Needs steps 1 to 6 working. **No new migration, no new table. Nothing is stored:** the app sends the earlier turns
with every message, so no chat history sits in your database.

## The endpoint

`POST /api/students/career-assistant/chat` (student login). Body:

    { "message": "What should I learn first?", "history": [ { "role": "user", "content": "..." }, { "role": "assistant", "content": "..." } ] }

Message: 1 to 1000 characters. History: the last 10 turns are used (roles `user` / `assistant`, 1500 characters each,
6000 in total). Before the reply starts you can get **400** (empty message, no profile) or **429** (see limits).

Once it starts, the response is `text/event-stream`:

    data: {"text":"You are missing "}
    data: {"text":"Azure and Scrum..."}
    event: done
    data: {}

A failure mid-reply sends `event: error` with `data: {"message":"..."}`; what already arrived stays valid.
In a browser or app use `fetch` and read the stream (POST is needed, so the built-in `EventSource` does not fit).

## What the assistant can see (three read-only tools)

| tool                     | gives                                                              | API endpoint (run token, purpose "chat") |
|--------------------------|--------------------------------------------------------------------|-------------------------------------------|
| get_my_profile_summary   | programme, year, skills, education, certifications, jobs, project titles. **No name, contact details, links or documents.** | /api/agent-tools/chat/profile-summary |
| get_my_matches           | best open internships with score and matching skills                | /api/agent-tools/chat/my-matches |
| get_my_skill_gaps        | gaps for one internship (or the top matches) and catalogue certifications | /api/agent-tools/chat/skill-gaps |

The student always comes from the run token, never from the request. The token is made per message and lasts a few minutes.

## 1. C# (unzip over the solution root, overwrite)

New: Application/DTOs/Student/CareerChatDtos.cs, Application/Interfaces/ICareerChatService.cs,
Application/Services/CareerChatService.cs, Application/Services/CareerAgentToolsService.cs,
Infrastructure/Integration/N8n/N8nStreamClient.cs, Api/Controllers/CareerAssistantController.cs,
Api/Controllers/ChatAgentToolsController.cs, InternshipManagement.Api.chat.http.

Changed (copies of your current files with a small edit): Infrastructure/Integration/N8n/N8nServiceCollectionExtensions.cs
(a second HttpClient for streaming, no overall timeout), Api/Extensions/ServicesCollectionsExtensions.cs (2 registrations).
**Apply step 6 first, then this zip.** Then `dotnet build InternshipManagement.Api`.

## 2. n8n: two workflows to import

1. `n8n/chat-mcp-tools.workflow.json` ("MCP tools (career assistant)", MCP path `chat-tools`, three tools).
   Pick the `MCP server key` credential on the MCP Server Trigger, publish. Check every tool node shows its parameters
   (reason, runToken, plus maxResults or internshipId) as "By Model (and is required)", and Send Headers stays OFF.
2. `n8n/career-chat.workflow.json` ("Career assistant chat (streaming, agent + MCP)", webhook path `career/chat`).
   Re-pick credentials: Webhook (same Header Auth), Gemini Chat Model, MCP Client (`MCP server key`). Publish.

**Streaming needs both of these, and they must match** (otherwise n8n falls back to one reply at the end):
- Webhook node: **Respond = Streaming**.
- AI Agent node: **Options > Enable Streaming = on**.
I set both in the file, but n8n versions differ: after import open both nodes and check the setting is there and on.
If the option is missing, update n8n. This workflow has no "Respond to Webhook" node on purpose: the agent streams straight back.

## 3. Test

Insomnia (`InternshipManagement.Api.chat.http`, requests 1 to 5) works, but may show the reply only when it is complete.
To watch it stream, use PowerShell (replace TOKEN with the login token):

    curl.exe -N -X POST http://localhost:5003/api/students/career-assistant/chat -H "Authorization: Bearer TOKEN" -H "Content-Type: application/json" -d "{\"message\":\"What should I learn first?\"}"

You should see `data: {"text":"..."}` lines appear one after another, then `event: done`. API console: three or fewer
`Chat tool ... called for user N` lines (only when the question needed them).

Checks: empty message gives 400; no token 401; a company login 403; send 21 messages inside 10 minutes and the 21st gives 429
with a Retry-After header; send a second message while the first is still streaming and it gets 429 "Wait for the previous reply".
Ask something off topic ("write my maths homework"): it should decline briefly.

## Limits and safety

- **Per student:** 20 messages per 10 minutes, one reply at a time. **Whole API:** 8 replies at once. These counters are per API
  instance and reset on restart, like the job queue. Reply cap: 4000 characters, 120 seconds.
- Streamed text cannot be checked before the student sees it. The prompt forbids links, invented internships and promises, and
  the API stops the stream if the run token ever appears, but a model can still say something odd. Treat it as a helper,
  not an authority. The matches and gaps it quotes come from your own code through the tools.
- What the student types, and the profile summary, go to Gemini. Contact details, name, documents and ID are never in the summary.
  Messages are not stored or logged by the API. n8n execution data is the same setting as before:
  with `EXECUTIONS_DATA_SAVE_ON_SUCCESS` on, n8n keeps every chat it receives. Set it to `none` before real students use this.
- The tool endpoints accept only a chat run token, so a login token or a token from the CV or match features cannot call them.
- If the workflow is not set to stream, the API still works: it takes the single answer and sends it as one piece.
- Not compiled here (no .NET SDK) and not run against a live n8n. Brackets balanced, the prompt builder ran on sample data.
  The stream parser follows the format n8n documents for streaming (one JSON object per line: begin, item with content, end),
  so if your n8n version differs, the symptom is an empty or one-piece reply: send me one raw response from `curl.exe -N`.
