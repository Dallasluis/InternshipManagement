# Step 6: skill gaps and certificate recommendations

For one internship (or the student's top 5 matches) the platform shows what the student's profile does not show yet,
and which recognised certifications could close the gap. **Code decides the gaps and the certifications. The AI only
writes the explanation**, and it can only mention certifications from the list it was given.

Needs steps 1 to 5 working. **No new migration, no new table.**

## What the student gets

- `GET  /api/students/skill-gaps?internshipId=12` : instant (database and code only, no AI). `internshipId` is optional:
  without it, the top 5 matches are compared.
- `POST /api/students/skill-gaps?internshipId=12` : a background job (like step 5). Returns 202 + job id,
  poll `GET /api/ai-jobs/{id}`. The result is the same JSON plus `aiSummary` and a `why` on each certification.
  If the AI is down, the result still comes back, with `aiAvailable: false`.

Response parts: `targets` (per internship: missing required/preferred skills and qualifications),
`gaps` (all missing terms, required first, with how many internships miss each), `recommendations`
(certifications from the catalogue with the gaps they `covers`), `notes`.

## How it decides

1. Missing = a skill or qualification the internship lists that is not on the profile. It uses exactly the same term
   rules as the matching score, so a gap here is a term that earned nothing there.
2. Each gap is compared with the `Topics` of every certification in `CertificationCatalogue.cs` (whole-word match:
   gap "cloud" matches topic "cloud computing"). Certifications the student already holds (by name or alias) are skipped.
3. Ranking: gaps that are Required count 3, Preferred 1, times the number of internships missing it. Top 6 are returned.
4. The catalogue holds **no prices, durations or links**, on purpose. The notes tell the student to check the provider.

## 1. C# (unzip over the solution root, overwrite)

New: Application/Services/CertificationCatalogue.cs, Application/Services/SkillGapService.cs,
Application/DTOs/Student/SkillGapDtos.cs, Application/Interfaces/ISkillGapService.cs,
Api/Controllers/SkillGapController.cs, InternshipManagement.Api.gaps.http.

Changed (copies of the step 5 versions with a small edit): Domain/Entities/AiJob.cs (new job type name),
Application/Interfaces/IAiJobService.cs and Application/Services/AiJobService.cs (start method),
Api/Extensions/ServicesCollectionsExtensions.cs (one registration).
**Apply step 5 first, then this zip.**

## 2. n8n: one workflow to import

Import `n8n/gap-explain.workflow.json` ("Internship - Skill gaps explain (agent, no tools)"), webhook path `skills/gaps`.
Unlike the CV and match workflows it has **no MCP tools**: the data arrives in the request, so there is no tools
workflow and no MCP Client node. Re-pick the credentials on two nodes: Webhook (same Header Auth) and Gemini Chat Model.
Publish.

## 3. Test (InternshipManagement.Api.gaps.http)

Prerequisite: the student has a few skills, and at least one Published + Approved internship with a future deadline
lists required or preferred skills the student lacks (for example Azure, Scrum, Excel).

1. Request 2 or 3 (GET): instant JSON. Check `gaps` are really missing from the profile and `recommendations` fit them.
2. Add a skill or certification on the profile that closes a gap, repeat: the gap (or the held certification) disappears.
3. Request 4 then 5: 202, then Done with `aiSummary` and `why` texts. With n8n stopped, the job is still Done with `aiAvailable: false`.
4. A closed or unknown internshipId gives 400.

## Limits to know about

- **Exact terms.** "SQL Server" does not match a profile skill "SQL", and "C Sharp" is not "C#". Such differences show up
  as gaps. The notes in the response say so; the CV analysis step helps because it normalises spellings to your skill list.
- **The catalogue is a fixed list in code** (about 40 certifications across IT, data, project management, finance,
  marketing, engineering and design). Change it by editing `CertificationCatalogue.cs` and deploying. If you want
  an admin screen to maintain it, that would be a database table (and a migration): say so and I will build it.
- Certification names and providers are well-known ones, but I could not check them against the providers' sites from
  here, and programmes get renamed or retired. Review the list once before launch.
- Soft skills ("communication") and degrees do not map to certifications, so they only appear as gaps.
- Not compiled here (no .NET SDK). Brackets balanced and the n8n scripts run on sample data; you are the first to run `dotnet build`.
