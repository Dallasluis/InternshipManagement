using System;
using System.Collections.Generic;

namespace InternshipManagement.Application.Services
{
    /// <summary>One well-known certification the platform can suggest.</summary>
    /// <param name="Id">Stable id. The AI may only refer to ids from this list.</param>
    /// <param name="Topics">Lower-case skill/subject terms this certification covers. Gaps are matched against these.</param>
    /// <param name="Aliases">Other names or exam codes, used to recognise a certification the student already holds.</param>
    internal sealed record CatalogueCertification(
        string Id, string Name, string Provider, string Level, string[] Topics, string[] Aliases);

    /// <summary>
    /// Curated list of recognised entry and intermediate level certifications across several fields.
    /// Deliberately holds NO prices, durations, links or exam details: those change, and a wrong number is worse
    /// than none. The student is told to check the provider for current details. Add or remove entries here
    /// (one line each); keep Topics short and lower case.
    /// </summary>
    internal static class CertificationCatalogue
    {
        public static readonly IReadOnlyList<CatalogueCertification> All = new List<CatalogueCertification>
        {
            // ---- Cloud and infrastructure -------------------------------------------------
            new("az-900", "Microsoft Azure Fundamentals (AZ-900)", "Microsoft", "Beginner",
                new[] { "azure", "cloud", "cloud computing", "microsoft azure" }, new[] { "az-900", "azure fundamentals" }),
            new("aws-ccp", "AWS Certified Cloud Practitioner", "Amazon Web Services", "Beginner",
                new[] { "aws", "amazon web services", "cloud", "cloud computing" }, new[] { "cloud practitioner", "clf-c02", "clf-c01" }),
            new("gcp-cdl", "Google Cloud Digital Leader", "Google Cloud", "Beginner",
                new[] { "google cloud", "gcp", "cloud", "cloud computing" }, new[] { "cloud digital leader" }),
            new("az-204", "Microsoft Azure Developer Associate (AZ-204)", "Microsoft", "Intermediate",
                new[] { "azure", "cloud", "c#", "apis", "api development", "serverless" }, new[] { "az-204", "azure developer associate" }),
            new("terraform-assoc", "HashiCorp Certified: Terraform Associate", "HashiCorp", "Intermediate",
                new[] { "terraform", "infrastructure as code", "devops" }, new[] { "terraform associate" }),
            new("cka", "Certified Kubernetes Administrator (CKA)", "CNCF / Linux Foundation", "Advanced",
                new[] { "kubernetes", "containers", "devops", "docker" }, new[] { "cka", "certified kubernetes administrator" }),

            // ---- Data and AI --------------------------------------------------------------
            new("dp-900", "Microsoft Azure Data Fundamentals (DP-900)", "Microsoft", "Beginner",
                new[] { "databases", "data", "sql", "azure", "data fundamentals" }, new[] { "dp-900", "azure data fundamentals" }),
            new("pl-300", "Microsoft Power BI Data Analyst (PL-300)", "Microsoft", "Intermediate",
                new[] { "power bi", "data analysis", "data visualization", "dashboards", "business intelligence", "reporting" }, new[] { "pl-300", "power bi data analyst" }),
            new("google-data-analytics", "Google Data Analytics Professional Certificate", "Google (Coursera)", "Beginner",
                new[] { "data analysis", "data analytics", "sql", "spreadsheets", "data visualization", "tableau" }, new[] { "google data analytics" }),
            new("ai-900", "Microsoft Azure AI Fundamentals (AI-900)", "Microsoft", "Beginner",
                new[] { "artificial intelligence", "ai", "machine learning" }, new[] { "ai-900", "azure ai fundamentals" }),
            new("ibm-data-science", "IBM Data Science Professional Certificate", "IBM (Coursera)", "Beginner",
                new[] { "data science", "python", "machine learning", "data analysis", "sql" }, new[] { "ibm data science" }),

            // ---- Programming and software -------------------------------------------------
            new("pcep", "PCEP: Certified Entry-Level Python Programmer", "Python Institute", "Beginner",
                new[] { "python", "programming" }, new[] { "pcep" }),
            new("pcap", "PCAP: Certified Associate in Python Programming", "Python Institute", "Intermediate",
                new[] { "python", "programming", "object oriented programming" }, new[] { "pcap" }),
            new("istqb-fl", "ISTQB Certified Tester Foundation Level", "ISTQB", "Beginner",
                new[] { "software testing", "testing", "quality assurance", "qa", "test automation" }, new[] { "istqb", "certified tester foundation" }),
            new("sf-admin", "Salesforce Certified Administrator", "Salesforce", "Intermediate",
                new[] { "salesforce", "crm" }, new[] { "salesforce administrator" }),

            // ---- Networking, support and security -----------------------------------------
            new("comptia-a", "CompTIA A+", "CompTIA", "Beginner",
                new[] { "it support", "help desk", "hardware", "troubleshooting", "operating systems", "technical support" }, new[] { "comptia a+", "a+" }),
            new("comptia-network", "CompTIA Network+", "CompTIA", "Intermediate",
                new[] { "networking", "networks", "tcp/ip", "network administration" }, new[] { "network+", "comptia network+" }),
            new("comptia-security", "CompTIA Security+", "CompTIA", "Intermediate",
                new[] { "cybersecurity", "security", "information security", "network security" }, new[] { "security+", "comptia security+" }),
            new("ccna", "Cisco Certified Network Associate (CCNA)", "Cisco", "Intermediate",
                new[] { "networking", "networks", "cisco", "routing", "switching" }, new[] { "ccna" }),
            new("sc-900", "Microsoft Security, Compliance and Identity Fundamentals (SC-900)", "Microsoft", "Beginner",
                new[] { "security", "cybersecurity", "identity", "compliance" }, new[] { "sc-900" }),
            new("itil4", "ITIL 4 Foundation", "PeopleCert / Axelos", "Beginner",
                new[] { "itil", "it service management", "itsm", "service management" }, new[] { "itil", "itil 4 foundation" }),

            // ---- Project management and ways of working -----------------------------------
            new("capm", "Certified Associate in Project Management (CAPM)", "Project Management Institute", "Beginner",
                new[] { "project management", "project planning", "project coordination" }, new[] { "capm" }),
            new("google-pm", "Google Project Management Professional Certificate", "Google (Coursera)", "Beginner",
                new[] { "project management", "project planning", "agile", "scrum", "stakeholder management" }, new[] { "google project management" }),
            new("prince2-f", "PRINCE2 Foundation", "PeopleCert / Axelos", "Beginner",
                new[] { "project management", "prince2" }, new[] { "prince2", "prince 2" }),
            new("psm1", "Professional Scrum Master I (PSM I)", "Scrum.org", "Beginner",
                new[] { "scrum", "agile", "agile methodologies" }, new[] { "psm i", "psm 1", "professional scrum master" }),
            new("csm", "Certified ScrumMaster (CSM)", "Scrum Alliance", "Beginner",
                new[] { "scrum", "agile", "agile methodologies" }, new[] { "csm", "certified scrummaster" }),
            new("lss-green", "Lean Six Sigma Green Belt", "Various accredited providers", "Intermediate",
                new[] { "lean", "six sigma", "process improvement", "quality management" }, new[] { "six sigma green belt", "lean six sigma" }),

            // ---- Office, finance and accounting -------------------------------------------
            new("mos-excel", "Microsoft Office Specialist: Excel", "Microsoft", "Beginner",
                new[] { "excel", "spreadsheets", "microsoft excel", "microsoft office", "data entry" }, new[] { "mos excel", "microsoft office specialist excel" }),
            new("mos-word", "Microsoft Office Specialist: Word", "Microsoft", "Beginner",
                new[] { "word", "microsoft word", "microsoft office", "document formatting" }, new[] { "mos word" }),
            new("xero-cert", "Xero Advisor Certification", "Xero", "Beginner",
                new[] { "xero", "bookkeeping", "accounting software", "accounting" }, new[] { "xero advisor", "xero certified" }),
            new("qbo-cert", "QuickBooks Online Certification", "Intuit", "Beginner",
                new[] { "quickbooks", "bookkeeping", "accounting software", "accounting" }, new[] { "quickbooks online certification", "quickbooks certified" }),

            // ---- Marketing and communication ----------------------------------------------
            new("google-analytics", "Google Analytics Certification", "Google", "Beginner",
                new[] { "google analytics", "web analytics", "analytics", "digital marketing" }, new[] { "google analytics certification", "gaiq" }),
            new("google-ads", "Google Ads Certification", "Google", "Beginner",
                new[] { "google ads", "online advertising", "paid advertising", "digital marketing", "ppc" }, new[] { "google ads certification" }),
            new("hubspot-inbound", "HubSpot Inbound Marketing Certification", "HubSpot Academy", "Beginner",
                new[] { "inbound marketing", "content marketing", "digital marketing", "marketing", "seo" }, new[] { "inbound marketing certification" }),
            new("hubspot-seo", "HubSpot SEO Certification", "HubSpot Academy", "Beginner",
                new[] { "seo", "search engine optimization", "digital marketing" }, new[] { "hubspot seo" }),
            new("meta-dma", "Meta Certified Digital Marketing Associate", "Meta", "Beginner",
                new[] { "social media", "social media marketing", "digital marketing", "online advertising" }, new[] { "meta certified digital marketing associate" }),

            // ---- Engineering and design ---------------------------------------------------
            new("acu-autocad", "Autodesk Certified User: AutoCAD", "Autodesk", "Beginner",
                new[] { "autocad", "cad", "technical drawing", "drafting" }, new[] { "autocad certified user", "autodesk certified user" }),
            new("acu-revit", "Autodesk Certified User: Revit", "Autodesk", "Beginner",
                new[] { "revit", "bim", "building information modelling", "architectural design" }, new[] { "revit certified user" }),
            new("cswa", "Certified SOLIDWORKS Associate (CSWA)", "Dassault Systèmes", "Beginner",
                new[] { "solidworks", "cad", "3d modelling", "mechanical design" }, new[] { "cswa", "solidworks associate" }),
            new("adobe-cp", "Adobe Certified Professional", "Adobe", "Intermediate",
                new[] { "photoshop", "illustrator", "graphic design", "adobe", "indesign" }, new[] { "adobe certified" }),
        };
    }
}
