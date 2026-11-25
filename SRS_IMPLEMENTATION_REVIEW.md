# SRS implementation and production-readiness review

**Scope:** Source review of the active API, application/domain, infrastructure, web, and test projects against `Internship_Management_Platform_SRS.docx`. Subscription and payment requirements (FR-24-38, BR-03/17 where they concern billing, and payment-related NFR-14) are intentionally excluded from implementation scoring, per request. This is not a live deployment, penetration test, load test, or legal review.

## Executive assessment

Interlane is substantially beyond the SRS MVP. Most non-billing functional areas have implementation in the application, API, and web layers: recruitment, interviews/offers, placements, progress, extensions, withdrawals/disputes, completion, evaluations, notifications, private documents, reports, moderation, and audit records. Several particularly important invariants are enforced in persistence, including duplicate applications and one placement per accepted application.

That feature coverage does **not** establish production readiness. The checked-in configuration is development-oriented (LocalDB and localhost URLs); uploaded documents have no malware-scanning step; there is no checked-in backup/recovery configuration; and the current test suite has very limited unit coverage and no substantive integration tests. Performance and availability targets are unmeasured. Follow-up development has removed the JWT signing-key fallback, centralized key validation for issuance and validation, added basic liveness/readiness endpoints, made the private-document disk root configurable while retaining the development default, and verified the API build and JWT tests.

Overall: **functional scope is mostly implemented, but production launch readiness is not demonstrated**. Resolve the blocking launch/security and operational items below, then validate the critical workflows end-to-end in a production-like environment.

## Functional requirement traceability

"Implemented" means an implementation path was located. It does not imply complete acceptance evidence for the requirement.

| SRS area | Status | Implementation evidence and caveats |
|---|---|---|
| FR-01–06 Registration/authentication | Implemented, with production configuration gap | Student/company registration, login/logout, password reset, email verification/change, role authorization, rate limits, account lockout, 2FA support, idle and absolute session expiry exist. JWT signing now requires the configured key, issuer, and audience; email delivery/public URLs require production configuration. |
| FR-07-10 Seeker profile/documents | Implemented | Structured profile, education, skills, experience, projects, certifications, portfolio, CV and private document management. Document access is guarded and audited; ClamAV scanning is integrated but must be enabled and connected to a scanner outside Development. |
| FR-11-19 Company registration/verification | Implemented, policy-dependent | Company registration, evidence upload, admin review/approve/reject/request information, reverification and suspension are represented. Only verified companies can publish. Evidence criteria and review service levels remain stakeholder decisions (review comment 34). |
| FR-20-23 Company profiles | Implemented, multi-representative partial | Company profile and logo, updates, location, website and industry exist. Representative data/relationships exist, but the SRS's multiple authorized representative permissions are not evidenced as a complete user-facing role/permission management flow. |
| FR-39-45 Opportunity management | Implemented | Drafts, create/edit, publish/close, structured requirements and listing moderation exist. Publication is gated by verification. Subscription gates are deliberately excluded from this review. |
| FR-46-51 Search/discovery | Implemented | Browse, search, filters, details, company information, bookmarking, sorting, matching and recommendations. Some advanced criteria may not all be exposed in UI; confirm skills/type/deadline/company filters against acceptance criteria. |
| FR-52-59 Applications | Implemented | Apply with owned private documents, duplicate prevention, application views/status, withdrawal and company review. Unique database index prevents duplicate student/opportunity applications. |
| FR-60-65 Status/shortlisting | Implemented | Status workflow, persisted status history and role-authorized changes, shortlist/reject, notifications. Regression coverage is very thin. |
| FR-66-72 Interviews | Implemented | Schedule, reschedule, cancel, notes/results, student details, notification paths. Confirm time-zone and calendar expectations as a deployment/product decision. |
| FR-73-77 Offers | Implemented | Offer creation/expiry/respond, optional contract attachment, role-gated access, status flow. Verify contract malware scanning and lifecycle edge cases before launch. |
| FR-78-87 Placement and active internship | Implemented | Accepted offer creates a placement; placement retains application link; pending/active/extended/completed/terminated lifecycle, reports and company feedback exist. Unique placement-per-application index protects duplicate creation. |
| FR-88-93 Extensions | Implemented | Company proposal, student response, accepted extension updates current dates while original dates remain persisted. |
| FR-94-103 Withdrawal/termination | Implemented | Student withdrawal request/reason, company decision/effective date, admin dispute and resolution, retained terminated records, next-placement flow and active-placement check exist. Business compatibility rule is currently a strict single-active-placement rule, which is safer but needs stakeholder approval/comment 177. |
| FR-104-111 Completion/evaluations | Implemented | Completion and history, company evaluation ratings and placement association exist. No tests found for these lifecycle operations. |
| FR-112 Notifications | Implemented, delivery dependent | In-app notifications and SMTP event paths exist. Production email host/credentials, domain authentication, delivery monitoring and retry behavior need configuration/evidence. |
| FR-113–117 Documents | Implemented, policy/configuration gap | Private API storage, size/type/signature checks, ownership and role/status-scoped short-lived hashed access links, retention states and audit events exist. The disk root is configurable (`PrivateDocumentStorage:RootPath`, default `App_Data/PrivateDocuments`), and an optional Azure Blob provider uses `DefaultAzureCredential` and rejects containers with public access enabled. A ClamAV-compatible scanner integration now scans uploads before storage and fails closed when enabled; development explicitly disables scanning unless a local daemon is configured. A purge worker permanently removes files and metadata only after an explicitly configured `DocumentRetention:AfterDays`; it is disabled by default pending an approved period. Access policy still needs approval (comment 122). Azure account/container provisioning, managed identity role assignment, backups, and live cloud/scanner verification remain outstanding. |
| FR-118-122 Fraud/content reporting | Implemented | Students can report listings; admin review/moderation, warnings, suspension and removal paths exist. Enforcement criteria/support/dispute policy need approval. |
| FR-123-130 Administration | Implemented | User/company/category/verification/moderation/report management and stats paths exist. |
| FR-131-132 Analytics | Partial | Platform statistics and company application stats exist. Full SRS metric inventory and analytics validation were not established in this source review. |
| FR-133 Audit logging | Implemented, coverage to validate | API audit filter and specific security/document/verification/lifecycle audit paths exist. Confirm coverage for all listed actions, retention, alerting, and log access in deployment. |
| NFR-01 Security | Partial | HTTPS/HSTS in production Web pipeline, Identity password hashing/lockout, API rate limiting, authorization, antiforgery for MVC posts, session controls and private docs exist. Hard-coded signing-key fallback, no malware scanning, and absent production security controls/evidence prevent sign-off. |
| NFR-02 Privacy | Not demonstrated | Authorization controls exist, but no approved jurisdiction, privacy/data-subject workflow, processor/data-residency decisions, or breach process is present in this checkout. |
| NFR-03 Performance | Not validated | SRS p95 targets have no load profile or test evidence. |
| NFR-04 Availability | Not validated | 99.5% objective has no production monitoring or measurement evidence. |
| NFR-05 Reliability / NFR-10 Integrity | Partially supported | Database constraints protect duplicate applications and placements; lifecycle logic and EF migrations exist. Integration/failure/concurrency tests and operational recovery are not evidenced. |
| NFR-06 Scalability | Not demonstrated | Web sessions use in-memory distributed cache and private documents use local disk; this is not sufficient evidence for multi-instance scaling. |
| NFR-07 Usability / NFR-08 Compatibility | Partial | Responsive interface work exists. Browser/version and viewport acceptance matrix from comment 152 is not defined or tested. |
| NFR-09 Maintainability | Partial | Layered solution with API/application/domain/infrastructure/web and migrations. Eleven tests now exist, including SQLite-backed document-upload integration coverage. Critical recruitment, placement, auth, and deletion workflows still lack integration coverage. |
| NFR-11 Backup/recovery | Missing evidence | No checked-in backup schedule, restore process, RPO/RTO, or restore-test evidence located. |
| NFR-12 Retention | Partial | Document retention states and security-event retention options exist, but an approved retention schedule and automated policy for all historical records are not established. |
| NFR-13 File security | Partial | Private storage, file size/type/signature checks, authorization and auditing are implemented. ClamAV streaming integration is implemented and rejects infections or scanner errors when enabled; development settings explicitly disable it pending a local scanner, and no live scanner has been exercised. |

## Reviewer comments in the DOCX

These are reviewer comments, not normative SRS requirements. They should be tracked separately from feature completion.

| Reviewer comment | Assessment |
|---|---|
| General: don't send PDF if contribution is expected | Process feedback. The supplied document is DOCX, so this copy addresses the requested format concern. |
| Application name should sell the idea; be creative | Product/branding feedback. The current product uses **Interlane**; validate name and positioning with stakeholders. |
| Add an Executive Summary | Document-quality comment. The SRS currently starts with Introduction; add an approved one-page summary when maintaining the SRS. |
| Define verified addresses, token validity, resend limits, access before verification, email change | Partially addressed in code: email verification options include 24-hour links, resend caps, unverified-account middleware, and a pending email-change confirmation. Confirm exact policy matches stakeholder intent and document it. |
| Approve company evidence/criteria/statuses/review target/reverification/appeals | Workflow/statuses/history and appeal paths exist; evidence criteria and review-time target remain policy decisions. |
| Define document access matrix | Not resolved by code alone. Current owner and scoped access checks exist; approve matrix by document/category, company, application/placement, admin, and retention status. |
| Make security controls measurable | Partially addressed in code (password minimum/hash iterations, lockout, MFA for some roles, session expiry, rate limits). Encryption-at-rest, vulnerability baseline and security-event alerting need deployment/acceptance definitions. |
| Identify launch jurisdictions and privacy obligations | Not resolved. Requires stakeholder/legal decision before launch. |
| Make performance targets reproducible | Not resolved. Define test workload, data size, region, cache state, duration and measurement boundary, then run load tests. |
| Define browser policy/viewports | Not resolved. Create supported browser/version and representative viewport matrix, then verify. |
| Make concurrent placement rule mandatory and measurable | Current code prevents any second pending/active/extended placement, which is stricter than "incompatible" placements. Stakeholders should approve this rule or define the compatibility exceptions. |

## Production launch blockers and priority

### P0 - Resolve before any real user data or public production launch

1. **Validate JWT configuration in the active environment.** The silent signing-key fallback has been removed; token issuance and validation use one shared validator that requires a 32-byte key, issuer, and audience. Confirm your Development User Secrets load, and configure the production secret separately before deployment. Rotate any key that may have been used outside local development.
2. **Create a production configuration and secret inventory.** Replace LocalDB/localhost URLs with production database, API/Web origins, SMTP, notification callback, public verification URLs, CORS origins, and trusted proxy addresses. Keep credentials out of source control.
3. **Finish safe durable private-document storage.** Local disk remains the default (`PrivateDocumentStorage:Provider=Local`) and supports `PrivateDocumentStorage:RootPath` (`PrivateDocumentStorage__RootPath`). An optional Azure Blob provider is available with `PrivateDocumentStorage:Provider=AzureBlob`, `PrivateDocumentStorage:BlobServiceUri`, and optionally `PrivateDocumentStorage:ContainerName` / `ManagedIdentityClientId`. It creates a private container when needed and authenticates through `DefaultAzureCredential` (local developer sign-in or Azure managed identity), without account keys in configuration. An automated purge worker deletes files and records after `DocumentRetention:AfterDays`, but remains disabled until a retention period is approved and configured. ClamAV scanning is integrated; development has it explicitly disabled, and production must point `MalwareScanning:Host` at a reachable scanner. Before production, provision the Azure storage account/container and grant the app identity the minimum Blob data role; decide backup/restore policy, migrate existing files if changing providers, and verify scanner operation. The cloud provider and scanner have not been exercised against live services.
4. **Approve privacy, document-access, retention, and company-verification policies.** These shape lawful data handling and authorization, so code cannot infer them safely.
5. **Run security review and deployment verification.** Validate HTTPS end-to-end, forwarded-header trust, secrets, authorization boundaries, file handling, dependency vulnerabilities, and production logs/alerts.

### P1 - Required to claim reliable production operation

1. Add integration tests around registration/verification, company publication gates, application/status transitions, duplicate applications, offer/placement idempotency, placement conflicts, document access, and termination/dispute flows.
2. Add automated backup and restore with approved RPO/RTO; prove restoration in a non-production environment.
3. Add health/readiness checks, centralized structured logs, exception alerts, uptime/availability measurements, and SMTP/storage/database failure alerts.
4. Define and run performance/load tests for the SRS p95 targets; tune queries/indexes based on real evidence.
5. Define browser/device support and complete responsive/accessibility checks.
6. Expand audit-log coverage and retention/alerting acceptance for each listed sensitive event.

## Verification performed

- Read DOCX body text and 13 anchored reviewer comments from the Word package.
- Traced the active API, application, domain, infrastructure, Web, persistence mappings, and test projects. Excluded generated `bin`/`obj` output from source conclusions.
- Added a ClamAV INSTREAM integration, fail-closed upload scanning when enabled, development override, and tests for clean, infected, and explicitly disabled scanner outcomes.
- Added SQLite-backed document upload integration tests verifying clean files are scanned, stored, persisted, and audited, while scanner-rejected files are neither stored nor persisted.
- Added a configurable purge worker for documents marked `Deleted`; it is off until a positive `DocumentRetention:AfterDays` is configured.
- Updated `System.Security.Cryptography.Pkcs` from vulnerable 9.0.11 to patched 9.0.21. The security advisory lists 9.0.21 as the fixed 9.x version ([Microsoft advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx)). Existing legacy .NET Framework package compatibility, nullable, and analyzer warnings remain.
- Ran the focused API build with output redirected to a temporary directory: **build succeeded, 0 errors**.
- Ran `dotnet test` with project references built into a temporary directory: **11 passed, 0 failed**. Coverage includes JWT config, scanner protocol, and document-upload persistence paths, but not the broader recruitment and placement workflows.
- Added API `/health/live` and `/health/ready` endpoints. The latter probes database connectivity and returns only a generic 503 when unavailable.
- Made the API private-document root configurable; relative paths resolve from the API content root, and paths inside the public web root are rejected. This does not move existing files or make local storage durable by itself.
- Confirmed `InternshipManagement.IntegrationTests` contains no substantive test cases; broad workflow integration tests and database-backed retention tests remain outstanding.
- Did not run production, load, penetration, accessibility, backup/restore, or browser-matrix testing.

## Launch decision

The product has a broad implemented feature set and is beyond MVP, but this repository alone does **not** demonstrate production readiness. Close P0 items and obtain the policy/operations decisions first; then use P1 tests and operational evidence as launch acceptance gates. Subscription and payment implementation were not assessed, as requested.

