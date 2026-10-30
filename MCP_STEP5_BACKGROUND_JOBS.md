# Step 5: background jobs for the AI features

CV analysis and match explanations no longer make the request wait for n8n/Gemini.
`POST` returns **202 Accepted** within milliseconds with a job id; a background worker does the slow part;
the app polls `GET /api/ai-jobs/{id}` until `status` is `Done` (result inside) or `Failed` (message inside).
The n8n workflows do not change.

## Flow

1. `POST /api/students/cv-analysis` (PDF) : the fast checks run immediately (not a PDF, too big, no text, no profile
   give a normal **400** right away), contact details are removed, then the job is queued. Returns 202 `{id, status:"Pending"}`.
2. `POST /api/Matching/student/internships/explained?maxResults=5` : replaces the old synchronous GET. 202 + job id.
3. `GET /api/ai-jobs/{id}` : `Pending` / `Running` / `Done` / `Failed`. `result` is exactly what the old synchronous
   endpoint returned. Only the owner can read a job (anyone else gets 404). Poll every 2 to 3 seconds.
4. `POST /api/students/cv-analysis/confirm` : unchanged.

Other responses: **409** when the student already has a job of that type running, **503** (Retry-After 60) when the
queue is full, **429** from the existing "ai-generation" limit (10 per hour per user, on the start endpoints only).

## What is in the box

- New table `AiJobs` (entity `AiJob`): owner, type, status, result JSON, short error, timestamps.
  **The CV file and CV text are never stored.** The text lives only in memory while the job waits, and the result
  (suggestions) is deleted 6 hours after creation.
- Bounded in-memory queue (50) + `AiJobWorker` (2 jobs at a time, which also protects your Gemini quota).
- Per job: 4 minute limit, one automatic retry after 3 s when n8n/Gemini fails (for example the 503 "high demand"),
  friendly error messages (no stack traces, no CV text).
- On startup, jobs left Pending/Running by a previous run are marked Failed. Jobs stuck for more than 6 minutes are
  also reported as Failed when read.

## 1. C# (unzip over the solution root, overwrite)

New: Domain/Entities/AiJob.cs, Infrastructure/Persistence/Configurations/AiJobConfiguration.cs,
Application/DTOs/AiJobs/AiJobResponse.cs, Application/Interfaces/IAiJobService.cs,
Application/Services/AiJobService.cs, Api/Jobs/AiJobQueue.cs, Api/Jobs/AiJobWorker.cs, Api/Controllers/AiJobsController.cs,
InternshipManagement.Api.jobs.http (+ copy sample-cv.pdf from step 3 next to it, it is already in your Api folder).

Changed (copies of your current files with small edits): ApplicationDbContext.cs (AiJobs DbSet, configuration, query filter),
IApplicationDbContext.cs (AiJobs), ICvAnalysisService.cs + CvAnalysisService.cs (analysis split into a fast `PrepareAsync`
and a slow `AnalyzeTextAsync`; `AnalyzeAsync` still exists), CvAnalysisController.cs and MatchExplanationController.cs
(now start jobs), ServicesCollectionsExtensions.cs (4 registrations).

## 2. Migration (required, this step adds a table)

Stop the API, then from the solution folder:

    dotnet ef migrations add AddAiJobs --project InternshipManagement.Infrastructure --startup-project InternshipManagement.Api

Check the generated file only creates `AiJobs` with its two indexes, then start the API. It applies itself at startup.

## 3. Test (Insomnia or InternshipManagement.Api.jobs.http)

1. Log in as the student. Start the CV job: you should get **202 in well under a second**, body `{"id":1,"status":"Pending",...}`.
2. `GET /api/ai-jobs/1` repeatedly: Pending/Running, then Done with `result` (skills, education, notes...).
3. Start it twice quickly: the second gets 409. Upload a non-PDF: 400 immediately. Log in as another student and read job 1: 404.
4. Stop n8n and start a job: after the retry it ends `Failed` with "The AI service is unavailable...".
5. Start a match job, poll it: `result.matches[].aiExplanation`.

The old `N8n:TimeoutSeconds` of 120 is still a good value: the timeout now only limits the worker, not the user.

## Limits to know about (read before production)

- **One API instance only.** The queue is in memory. With two instances each has its own queue, and the
  "mark interrupted jobs Failed at startup" step would also fail the other instance's running jobs.
  Scaling out needs an external queue (a database-claimed job table, Azure Service Bus, RabbitMQ).
- A restart or deploy loses jobs that were waiting or running; the student sees "server restarted, try again".
  Nothing is lost that cannot be redone, since nothing was saved.
- The PDF is not kept, so a retry means uploading it again.
- Not compiled here (no .NET SDK in my environment). I checked bracket balance and read each file against your
  existing code, but you are the first to run `dotnet build`. The worker lives in the Api project on purpose
  (it needs `BackgroundService`, which the Web SDK already includes).
- No push notification when a job finishes: the app polls. SignalR could replace polling later.
