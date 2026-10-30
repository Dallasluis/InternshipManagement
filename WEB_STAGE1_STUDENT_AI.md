# Web stage 1: student AI pages

Extract over the solution folder (the files go inside InternshipManagement.Web). Overwrites 2 existing files:
- Program.cs (one line added: registers IAiApiClient)
- Views/Shared/_Sidebar.cshtml (four student links added)

New files: Services/IAiApiClient.cs, Services/AiApiClient.cs, Controllers/StudentAiController.cs,
Views/StudentAi/{Matches,CvAnalysis,SkillGaps,Assistant}.cshtml, wwwroot/js/student-ai.js, wwwroot/css/student-ai.css.

No migration, no API change, no n8n change. Build: dotnet build InternshipManagement.Web

Try it (log in as a Student, API and n8n running):
1. Sidebar > Recommended for me: the list appears at once; "Explain my matches with AI" adds the AI text.
2. CV Analysis: upload a PDF, tick items, "Add selected to my profile".
3. Skill Gaps: table appears at once; "Add an AI summary" adds the summary.
4. Career Assistant: the answer streams in. Streaming needs the n8n "Career assistant chat" workflow
   Webhook set to "Respond = Streaming" and the AI Agent's "Enable Streaming" on.

Notes
- The browser never sees the API token. The pages call StudentAiController, which calls the API with the session token.
- An unverified email gets "Verify your email address before using this feature." from the API (its middleware).
- Slow AI runs are polled for up to 5 minutes, then the page says it is taking too long.
