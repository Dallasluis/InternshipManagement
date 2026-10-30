# Web stage 4: connection fixes found by checking the Web app against the API

Install stages 1, 2 and 3 first, then extract this zip over the solution folder.
Overwrites (cumulative versions): Program.cs, Controllers/AccountController1.cs (only Logout changed),
Controllers/AccountRecoveryController.cs and Views/Shared/_VerifyEmailBanner.cshtml (from stage 3),
Views/Company/CreateInternship.cshtml (stage 2 version + fix), Views/Company/EditInternship.cshtml,
Models/Internship/CreateInternshipRequest.cs, Models/Internship/UpdateInternshipRequest.cs.
New: Services/TokenRefreshCoordinator.cs, Services/ClientIpForwardingHandler.cs,
Middleware/SessionTokenMiddleware.cs, ModelBinding/CommaSeparatedListBinder.cs.
No migration. Build: dotnet build InternshipManagement.Web

## What was wrong, and what this fixes

1. Company and admin users were signed out after 30 minutes even when active (students after 2 hours).
   The API's access token lasts only as long as the idle limit, and the Web app stored the refresh token but never used it.
   Now the Web app renews the token about a minute before it runs out and keeps the session going while the user is active.
   If the API says the session is over (idle too long, signed out elsewhere, account suspended) the user is sent to the login page
   with a message, instead of an error page.
2. Logout only cleared the Web session; the API refresh token stayed valid. Logout now ends the session on the API too.
3. The API's login limit (20 per minute per address) and its security log saw only the Web server's address, so one limit
   was shared by every user. The Web app now forwards the visitor's address (X-Forwarded-For). SEE THE API EDIT BELOW.
4. "Eligible programmes" was saved as one list item ("A, B") instead of two. It is now split at commas.
5. The verification banner now has "I have verified", which refreshes the signed-in details without logging out.

## API edit for item 3 (InternshipManagement.Api/Program.cs, by hand)
Add near the other builder.Services lines:

    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;
        options.ForwardLimit = 1;
        foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
            options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    });

Add as the FIRST middleware, before app.UseMiddleware<ExceptionHandlingMiddleware>():

    app.UseForwardedHeaders();

On your computer the Web and API both run on localhost, which is trusted by default, so nothing else is needed.
When deployed, add the Web server's address to appsettings.json, for example:

    "ForwardedHeaders": { "KnownProxies": [ "10.0.0.5" ] }

Only addresses listed there are believed. If the list is empty in production the header is ignored, which is safe but
brings back the shared limit.

## Also checked and found fine
All Web-to-API addresses and HTTP verbs match the API once the two route lines from stage 3 are added; request and response
models match the API's classes; every link in the views points to an existing action; document upload, download and
access links match the API.

## Still open (nothing is broken, these have no screen yet)
- Admin: "trigger re-verification" and the verification-time (SLA) report exist in the API but have no admin page.
- Company: its own verification history (GET api/Companies/verification/history) is not shown on the company page.
- Subscriptions (post-MVP, as agreed).
