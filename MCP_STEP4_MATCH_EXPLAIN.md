# AI explanations for internship matches (student side)

Needs the CV analysis working (the student's profile needs skills). **No new migration, no new package.**

The ranking is unchanged and stays in code: `GET /api/Matching/student/internships` still returns the same list.
The new endpoint returns the same matches plus an `aiExplanation` in plain words for each, written by the AI.
The AI never decides scores, order or skill lists. If the AI is down, the matches still come back, with
`aiExplanationsAvailable: false` and no `aiExplanation`.

    GET /api/Matching/student/internships/explained?maxResults=5     (Bearer token of a Student)

Example result item (extra fields shortened):

    { "targetId": 12, "title": "Junior Backend Intern", "matchScore": 78,
      "matchingSkills": ["c#", "sql server"], "missingPreferredSkills": ["docker"],
      "explanation": "Matched skills: c#, sql server. Missing preferred skills: docker.",   <- the old template text
      "aiExplanation": "You already have C# and SQL Server, which this role asks for. Learning Docker would make your application stronger." }

How it works: the API calls n8n with only a run token (purpose "match"). The AI's one tool, `get_my_matches`,
calls the API back and gets the matches for that student. The API then keeps only explanations whose
`targetId` is really in the student's list, and cleans the text.

## 1. C# (unzip over the solution root, overwrite)

New files:
- Application/DTOs/Matching/MatchExplanationDtos.cs
- Application/Interfaces/IMatchExplanationService.cs
- Application/Services/MatchExplanationService.cs
- Api/Controllers/MatchExplanationController.cs      (student endpoint)
- Api/Controllers/MatchAgentToolsController.cs       (the AI tool's endpoint, run token only)

Changed file (your current copy plus one registration line, `IMatchExplanationService`):
- Api/Extensions/ServicesCollectionsExtensions.cs

Stop the API, `dotnet build`, start it on port 5003.

## 2. n8n (two imports, nothing existing changes)

1. Import `n8n/match-mcp-tools.workflow.json` ("Internship - MCP tools (match explain)").
   MCP Server Trigger: pick `MCP server key`, Publish. Check the `get_my_matches` node shows
   `reason`, `maxResults` and `runToken` as "By Model (and is required)".
2. Import `n8n/match-explain.workflow.json` ("Internship - Match explain (agent + MCP)").
   Re-pick the credentials on Webhook (same Header Auth), Gemini Chat Model (pick the model you use) and
   MCP Client (`MCP server key`). Publish.

The `js/` files are the same code as inside the workflows, for reading only.

## 3. Test in Insomnia

The list is only filled when there are internships that are Published, Approved by an admin and whose application
deadline is in the future, and the student has skills on the profile (run the CV analysis and confirm first).
Use the company and admin requests from your first .http file to create and approve one if you have none.

1. Log in as the student, copy the token.
2. `GET http://localhost:5003/api/Matching/student/internships/explained?maxResults=5` with that Bearer token.
3. Wait up to 60 s, send once. API console: `Match agent tool my-matches called for user ...`.
4. Each match should have `aiExplanation` that only mentions skills from `matchingSkills` and `missingPreferredSkills`.

## If it fails

- Empty `matches` list: no published and approved internship with a future deadline, or no skills on the profile.
- `aiExplanationsAvailable: false`: the AI part failed (n8n down, workflow not published, Gemini overloaded).
  Look at the run in n8n's Executions tab. The matches in the response are still correct.
- 401 on the tool only: the `runToken` row is missing on the `get_my_matches` node.
- Matching quirk worth knowing: the matcher compares skills by exact term, so "C Sharp" and "C#" do not match.
  That is why the CV analysis normalises spellings with the vocabulary tools.
