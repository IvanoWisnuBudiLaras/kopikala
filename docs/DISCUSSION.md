**NOTE: Architectural and security design evaluations are COMPLETE and ready for coding. DISCUSSION.md remains a living document for project refinements.**

# ARCHITECTURAL\_DECISION\_LOG (DISCUSSION.md)

---

version: "1.0-ai-optimized"\<line-break/\>project: "KopiKala Reservation \&amp; Order System"\<line-break/\>target\_environment: "C\# .NET 10 | Visual Studio 2026"\<line-break/\>last\_updated: "2026-09-04"\<line-break/\>status: "PRD.md (Official Release 1.0) GENERATED; DISCUSSION.md remains dynamic open living document"\<line-break/\>User/Auth/Access Control: "COMPLETED"  
---

---

## 1\. CONTEXT\_AND\_GOALS

* SYSTEM\_TYPE: Web-based Table Reservation & F\&B Pre-order System  
* SCOPE: PKL (Internship) Mini-Project  
* TARGET\_COMPLEXITY: Small footprint, complete end-to-end lifecycle, standard industry patterns (no over-engineering).

---

## 2\. DECISION\_MATRIX\_AND\_RATIONALE

*   
* 

| Topic | User Concern / Input | Options Evaluated | Final Decision | AI Semantic Rationale Database Engine Pointed out that Microsoft SQL Server has expensive production licensing costs. 1\. SQL Server (paid in production)\<line-break/\>2. SQLite (no enterprise rowversion)\<line-break/\>3. PostgreSQL (100% free open-source, enterprise grade) PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL). Zero license cost for production, unlimited scaling, full EF Core support, native xmin system column for Optimistic Concurrency Control, and standard SQL unique constraints.  |
| :---- | :---- | :---- | :---- | :---- |
| Frontend Framework | Decided to use Blazor for the UI. | 1\. ASP.NET Core MVC (Razor)2\. Blazor Server3\. SPA (React/Vue) | Blazor Web App (Interactive Server / C\# Full-Stack). | Pure C\# across both UI and backend, bidirectional SignalR real-time updates for table statuses, seamless component reusability, zero JavaScript dependency. |
| Project Architecture | Wants fast, stable development in Visual Studio 2026\. | 1\. Monolith 1-Project2\. .NET Aspire Multi-Project3\. Clean Architecture (N-tier) | Monolith Klasik (1 Project) | Maximum VS scaffolding support (Add Controller with Views), zero inter-service network overhead, single F5 debug. |
| Access Control | Needs multi-role without unnecessary complexity. | 1\. RBAC (Role-based)2\. PBAC (Policy/Claims)3\. ABAC (Attribute-based) | Controlled Dynamic PBAC (5 Core Operational Permissions). | Bounded to 5 clear operational domains (Meja, Pembayaran, Dapur, Laporan, Sistem). Allows multi-tasking staff (e.g. Kasir acting as Barista) without code changes, while preventing arbitrary over-engineering. |
| Layering Demystification | Confused by "Domain" vs "Service" jargon. | 1\. Complex DDD (Aggregates, Invariants)2\. Layered Architecture (Controller-Service-Model) | Controller \-\> Service \-\> Model | "Domain" simplified to Model (DB entities). Service \= business logic engine. Controller \= traffic router. |
| DTO vs Model | Wondered if both create tables. | 1\. Direct Model binding2\. DTO layer separation | DTO (No DB Table) | Models create physical tables. DTOs are transient memory containers for form inputs/responses; prevents Mass-Assignment attacks. |
| Helper Design | Asks if helpers require inheritance. | 1\. Base class inheritance (BaseController)2\. Static extension methods | Static Extension Methods | public static class with this parameters (e.g. .ToRupiah()). Zero inheritance to avoid God Object anti-pattern. |
| Worker Specialization Operational Scope Boundaries (F\&B Operations vs ERP Inventory) Questioned the real purpose of Barista/Kitchen roles and warned against over-engineering raw ingredient/warehouse inventory (meat/vegetable logistics). 1\. Full ERP warehouse tracking\<line-break/\>2. Streamlined KDS and menu sold-out switches. Streamlined KDS \&amp; Sold-Out Switch only. Barista role strictly handles KDS ticket status (Mulai/Selesai) and Menu Availability toggle. Raw warehouse ERP inventory is strictly excluded. Prevents scope creep, keeps the project laser-focused on Table Booking \&amp; F\&B pre-order within realistic PKL boundaries.  | Avoid simplistic 1-minute loops; requested background jobs, schedulers, cron jobs. | 1\. Single generic timer2\. Tri-tier worker architecture | Tri-Tier Worker Engine | 1\. Background Job: PDF/Notif queue. 2\. Scheduler: H-1 reminder & 20m grace period. 3\. Cron: 00:00 Daily reconciliation. |
| Comprehensive Security Audit | Inquired on CORS & Antiforgery needs. | 1\. Dedicated Security layer2\. Framework middleware / attributes | 5-Pillar Pragmatic Security Suite:1\. Native Antiforgery (Auto CSRF tokens in Razor forms).2\. Native Anti-SQL Injection & Anti-XSS (EF Core parameterized queries \+ Razor HTML encoding).3\. Strict File Upload Sanitation (Whitelist .jpg/.png only, magic bytes validation, max 2MB, random GUID filename).4\. Ownership Verification / IDOR Protection (Ensure customers only access their own booking invoices).5\. CORS Disabled (Unneeded for single same-origin monolith). UI Three-Portal Topology Wanted exactly 3 portals: Customer, Worker (Staff), SuperAdmin. Questioned whether Admin should be merged with Worker or separate. Separate portal for each role vs 3 unified areas with Dynamic PBAC visibility. 3 Unified Portals: Customer Area, Staff Area (Merges Cashier, Barista, Waiter, and Manager/Admin via dynamic PBAC tab rendering), and SuperAdmin Area (System Owner). Maximum UI reuse, zero code redundancy, perfectly leverages PBAC claims in Razor layouts.  | Closes all realistic attack vectors without introducing third-party overhead. Standard User Login Enhancements Accepted common login improvements. Standard basic login vs complete operational suite. Adopted 4 operational features: Auto-onboarding on first Google OAuth login (Just-In-Time account creation with default Customer role). Persistent vs Transient Cookie (\&apos;Remember Me\&apos; toggle for personal mobile vs shared POS terminal). Remote Session Revocation / Force Logout by SuperAdmin (Security stamp update). Staff Login Audit Logging (User, timestamp, IP tracking). Essential for practical store-floor operations and accountability.  |

* 

| Customer Booking Wizard & Invoice Engine (TICKET-4) | Core transaction execution covering duration-based stay (1-3 hours), 2D table layout, F\&B pre-order, sticky bottom summary bar, and invoice issuance (transfer bank vs pay-on-site). | 1\. Traditional multi-page form\<line-break/\>2. Interactive 4-step MudStepper wizard | MudStepper 4-step wizard integrated with IBookingService, optimistic concurrency (xmin) anti-double booking, Microsoft Playwright with Live Headed Browser Mode (Login validation, 403 route interception, and Forgot Password flow), Monkey Testing, and FakeTimeProvider 15-minute expiration testing. | Encapsulates all customer-facing transactional logic into a single cohesive, highly testable vertical slice, verified via automated E2E journey testing and SignalR circuit resilience checks. |
| :---- | :---- | :---- | :---- | :---- |
| UI Styling Framework | Evaluated MudBlazor vs Microsoft Fluent UI. User officially selected MudBlazor. | 1\. MudBlazor2\. Microsoft Fluent UI3\. Tailwind | MudBlazor Component Library. E2E UI Testing (TICKET-3) Needs automated verification of the entire user journey (Landing Page \-\&gt; Login \-\&gt; Dynamic Navbar \-\&gt; Logout). 1\. Manual Testing2\. Microsoft Playwright (Automated E2E) Adopted Microsoft Playwright with Live Headed Browser Mode (Headless \= false with slowMo) and Monkey Testing (Chaos UI Testing). Live browser allows real-time visual inspection by the developer without file screenshot overhead; Monkey testing actively verifies Blazor Server SignalR circuit resilience against chaotic rapid clicks and erratic user interactions.  | 100% C\# (zero JavaScript/Node.js tooling overhead), Material Design components ideal for warm coffee/food aesthetics, built-in MudStepper, MudCard, MudDialog, MudSnackbar, and full real-time SignalR integration. |

| OAuth 2.1 Provider Architecture | Approved Google OAuth integration and building OAuth 2.1 server capabilities. | 1\. External IdP (Google OAuth 2.1 PKCE) for customers2\. Internal OpenIddict for local/staff issuance. | Hybrid Auth (Google OAuth 2.1 for seamless customer login \+ Local credentials for staff/superadmin). | Zero friction for customers, high security, full PKCE compliance. OpenIddict reference documented for internal identity server extension. |
| :---- | :---- | :---- | :---- | :---- |

| Operational Table Dynamics & In-Store Cashier Adjustments | Customer must specify duration of stay (how many hours); cashier can extend timeslot; cashier can handle offline walk-in guests; cashier can add F\&B orders to seated tables for clean financial audit; table booked under representative's name. Rejected refund policy, table switching, and SignalR. | Fixed rigid timeslot vs Dynamic booking duration with in-store cashier extensibility. | Adopted 6 user operational rules: Customer chooses duration of stay (hours) at booking. Add-on F\&B orders at seated tables (tracked as AdditionalOrder in BookingDetails). Cashier can extend timeslot/duration if next slot is unreserved. Walk-in / offline customer management by cashier. Booking registered under Representative Name for clear audit. Item Replacement / Switch Menu Feature (Fitur Ganti Item di Kasir): Allows cashier to swap a pre-ordered F\&B item at check-in if kitchen runs out of stock or guest requests change. Automatically calculates price differences (equal, upgrade with added bill, downgrade with credit/refund) and updates BookingDetails and total\_amount without voiding the table reservation. | Perfectly mimics physical cafe operations, eliminates table squatting, and ensures complete financial and inventory auditability. |
| :---- | :---- | :---- | :---- | :---- |

| QR Code Check-in Scope | Decided QRCoder is not needed. | 1\. QRCoder (QR Generation)\<line-break/\>2. Manual Search (Staff Portal) | Excluded QRCoder dependency. Check-in validation is conducted via Representative Name or Invoice Code search in the Cashier portal. | Streamlines dependencies, eliminates unnecessary third-party libraries, and aligns with standard personal coffee shop hospitality where cashiers greet guests by Representative Name. |
| :---- | :---- | :---- | :---- | :---- |

| Final Deliverables Scope 7 Production-Ready Pillars & End-to-End Delivery Mandated complete checklist: Docker, Testing with percentage & logs, structured Tickets, 4 Roles (SuperAdmin, Admin, Staff, User), comprehensive Security coverage, Business Reports, and full E2E production readiness. 1\. Minimal student project2\. 7-Pillar Production Standard Adopted the 7-Pillar Production Standard: Full multi-stage Dockerfile \+ Docker Compose, Unit (80% coverage) & API tests (FakeTimeProvider), 8-ticket execution roadmap, 4 distinct roles with Dynamic PBAC, 5-pillar security \+ audit logs, automated Cron 00:00 business reports, and zero-downtime E2E transaction flow on PostgreSQL. Elevates a standard student internship project into an enterprise-grade, auditable, production-deployable commercial software suite.  | Finalizing official project output components. | Standard project vs Complete production-ready suite. | 1 Visual Studio Solution (KopiKala.sln), Blazor Web App with MudBlazor, .NET Aspire dashboard, PostgreSQL auto-migration with seed data, 3 unified portals, tri-tier worker engine, and complete documentation suite. | Ensures consistent environment orchestration and full operational readiness across all project tiers. |
| :---- | :---- | :---- | :---- | :---- |

---

## 3\. USER\_DEFINED\_BUSINESS\_PIPELINE

Linear 6-phase state machine formulated by user:

1. **PHASE\_1\_BROWSE\_TABLE**: Customer selects table number and timeslot.  
2. **PHASE\_2\_PREORDER\_FNB**: Customer adds coffee/food to cart.  
3. **PHASE\_3\_INVOICE\_GENERATION**: System issues invoice containing Cafe Bank Account & Upload Form.  
4. **PHASE\_4\_PAYMENT\_DISPATCH**:  
   * Path A: Transfer bank \-\> Upload slip on web \-\> Staff verifies.  
   * Path B: Pay-on-site (Cash/QRIS at counter).  
5. **PHASE\_5\_CHECKIN\_AND\_OCCUPANCY**: Customer arrives \-\> Cashier sets Seated \-\> Active table countdown starts (1.5 \- 2h).  
6. **PHASE\_6\_AUTO\_RELEASE**: Worker detects timer expiration \-\> Status set to Completed \-\> Table status auto-resets to Available.

## 4\. DESIGN\_ANTI\_PATTERNS\_EXCLUDED

* NO\_GOD\_OBJECTS: No monolithic base controllers/services.  
* NO\_OVER\_POSTING: Never expose entity models directly to form bindings.  
* NO\_UNNECESSARY\_CORS: Do not add CORS headers to a same-origin monolith.  
* NO\_EMPTY\_WORKERS: Workers must have explicit, distinct functional responsibilities.  
* NO\_ERP\_SCOPE\_CREEP: Do not track raw ingredient grammage (meat, vegetables, logistics); only track finished menu item availability and daily portion counts.

