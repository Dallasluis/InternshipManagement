# Web stage 2: company AI tools

Install web stage 1 first (this uses its IAiApiClient and student-ai.css). Then extract this zip over the solution folder.
Overwrites 2 existing views: Views/Company/CreateInternship.cshtml and Views/Company/ManageInternships.cshtml.
New: Controllers/CompanyAiController.cs, Views/CompanyAi/Candidates.cshtml, wwwroot/js/company-ai.js.
No migration, no API change, no Program.cs change. Build: dotnet build InternshipManagement.Web

Try it (log in as a Company that is verified and subscribed):
1. Post Internship: "Write it with AI" panel at the top. Describe the role, press Generate draft, the form fills in.
   Previously generated drafts appear in "Load a saved draft".
   Start date, deadline and number of positions are never invented by the AI: fill them in yourself.
2. My Internships: Published internships get a "Best candidates" (people icon) button.

Notes
- The API only allows AI drafts for verified, subscribed companies; its message is shown if the company is not.
- Generating a draft waits for the AI in one request. The Web app gives up after 100 seconds (HttpClient default).
