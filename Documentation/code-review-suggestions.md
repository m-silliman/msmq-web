# MSMQ Manager Code Review and Improvement Suggestions

## 1. Executive Summary

The repository contains a recognizable three-layer Blazor Server application with useful domain models, scoped per-circuit services, queue/message components, destructive-operation dialogs, and Windows Service packaging. The solution builds cleanly and the local MSMQ service is installed and running. The most immediate product concern is initial-load performance on production servers with up to roughly 100 queues and several thousand messages, particularly journal messages and unprocessed messages in stale test queues.

Queue discovery and count collection currently perform synchronous, sequential, full-message scans. The reported symptom—several minutes before queues appear, followed by acceptable performance after initial load—matches this code path. Each private queue is converted by enumerating all of its messages before discovery returns, and connection setup then enumerates each eligible queue journal before publishing the connection. Counts and bounded message lists also call `GetAllMessages()`, materialize message objects/bodies, and leave returned `Message` objects undisposed. This performance path is the top delivery priority.

The application also needs a standard UX theming pass aligned with the company color palette and typography. It already has light/dark CSS variables, but generic Bootstrap colors, hard-coded color values, and a generic Helvetica/Arial font stack are used throughout. Company design tokens should be introduced once the approved palette, type scale, fonts, and accessibility requirements are supplied.

There are confirmed functional defects as well. Bulk delete remains enabled for journal views even though individual journal deletion is blocked; journal counts are constructed inconsistently and one refresh path appends the journal suffix twice; public queues are never enumerated; several public service methods are stubs that report success without doing the work; move/resend/export UI flows are incomplete; and the only automated test has no assertions.

Recommended direction: first make queue discovery progressive, remove eager full-body count scans, and put MSMQ/WMI work behind bounded execution with useful timeouts, cancellation, caching, and instrumentation. Next, fix journal safety/path defects and establish company UX design tokens. Then add focused tests around queue paths, bounded enumeration, concurrency, partial failures, date/time formatting, and theme accessibility. Authentication and identity remain important before broader or untrusted network exposure, but are intentionally classified as future hardening for the current roadmap. Incremental repair is practical; a rewrite is not warranted.

## 2. Application Architecture and Current Behavior

### 2.1 Project organization

- `MsMqApp` hosts the Blazor Server UI, Kestrel/Windows Service startup, page state, dialogs, JavaScript interop, and application-specific state services.
- `MsMqApp.Services` wraps `Experimental.System.Messaging`, performs WMI discovery, formats message bodies, and exposes higher-level message and connection operations.
- `MsMqApp.Models` contains queue/message domain models, configuration objects, DTOs, results, enums, and the queue-tree builder.
- `MsMqApp.Tests` is an xUnit project, currently containing one empty test.

### 2.2 Dependency and request flow

```text
Browser / Blazor Server circuit
        |
        v
Home.razor.cs and child components
        |-------------------------------|
        v                               v
IQueueConnectionManager       IMessageOperationsService
        |                               |
        |---------------|---------------|
                        v
                  IMsmqService
                        |
          Experimental.System.Messaging + WMI
                        |
                 local/remote MSMQ
```

All application and UI services are registered as scoped, which is appropriate for circuit-local state in Blazor Server (`MsMqApp/Program.cs:81-90`). Format handlers are stateless singletons (`MsMqApp/Program.cs:75-79`).

### 2.3 Current workflow behavior

1. **Startup:** `Program.cs` enables Windows Service hosting, binds Kestrel to `ListenAnyIP`, registers Razor components and scoped services, configures console/Event Log logging, and maps the interactive server application (`MsMqApp/Program.cs:9-149`). No authentication or authorization services/middleware are added.
2. **Initial connection:** `HomeBase.OnInitializedAsync` derives a computer name from the browser URL host and calls `QueueConnectionManager.ConnectAsync` (`MsMqApp/Components/Pages/Home.razor.cs:94-100`, `1042-1072`). The connection manager synchronously tests MSMQ, discovers queues, then performs a journal-count call per application queue (`MsMqApp.Services/Implementations/QueueConnectionManager.cs:70-155`).
3. **Queue discovery:** `MsmqService.GetQueuesAsync` enumerates private queues, optionally probes three fixed system queues, and uses WMI for outgoing queues (`MsMqApp.Services/Implementations/MsmqService.cs:26-272`). It does not call a public-queue enumeration API.
4. **Queue selection:** `HomeBase.HandleNodeSelectedAsync` resolves either the queue format name or computed journal path and calls `LoadMessagesAsync` (`MsMqApp/Components/Pages/Home.razor.cs:126-208`).
5. **Message loading:** `LoadMessagesAsync` requests up to 1,000 messages, but the service first calls `GetAllMessages()` and only then applies the 1,000-item limit (`MsMqApp/Components/Pages/Home.razor.cs:649-710`, `MsMqApp.Services/Implementations/MsmqService.cs:475-515`). The UI filters/sorts the in-memory list and uses `Virtualize` for rendering (`MsMqApp/Components/Shared/MessageList.razor:92-121`).
6. **Body display:** `MsmqConverter` reads the entire `BodyStream` into a byte array and also decodes it as UTF-8 (`MsMqApp.Services/Helpers/MsmqConverter.cs:184-235`). `MessageBodyViewer` detects or lets the user override XML/JSON/text/binary format and truncates only the rendered content (`MsMqApp/Components/Shared/MessageBodyViewer.razor.cs:133-185`).
7. **Message operations:** individual delete is confirmed and implemented as `ReceiveById`; purge requires typing the queue name; bulk delete is handled inside `MessageList`. Send is exposed through a dialog. Move is not implemented, while resend and export handlers exist but their detail buttons are commented out (`MsMqApp/Components/Shared/MessageDetail.razor:98-116`).
8. **Refresh:** a one-second `System.Timers.Timer` invokes the parent refresh callback at a configurable countdown (`MsMqApp/Components/Shared/RefreshControl.razor.cs:229-263`). Queue and message refreshes replace current state without request-version or cancellation checks.
9. **Shutdown:** DI disposes scoped services and component disposal stops several component timers. `HomeBase` disposes its .NET reference but does not remove the document listeners it registered (`MsMqApp/Components/Pages/Home.razor.cs:1141-1163`).

## 3. Validation Performed

### 3.1 Guidance and implementation reviewed

The review covered:

- `README.md`, `CLAUDE.md`, `Documentation/prd.md`, `Documentation/coding-standards.md`, and `.editorconfig`.
- `MsMqManager.sln`, every project file, package reference, `Program.cs`, application settings, and launch settings.
- All production `.cs` and `.razor` files in `MsMqApp`, `MsMqApp.Services`, and `MsMqApp.Models`.
- JavaScript and CSS assets, installer sources and PowerShell build/install/uninstall scripts.
- The complete `MsMqApp.Tests` project and existing deployment documentation.
- Searches for `TODO`, `FIXME`, `HACK`, placeholder/sample/template code, debug output, synchronous waits, timers, disposal, date formatting, and authentication/authorization.

No repository `AGENTS.md` file was present. The review followed the task-specific instructions supplied with the request.

### 3.2 Build and tests

| Command | Result |
|---|---|
| `dotnet build MsMqManager.sln --no-restore -v:minimal` | Passed; 0 warnings, 0 errors. |
| `dotnet test MsMqManager.sln --no-build --no-restore -v:normal` | Passed; 1 discovered, 1 passed, 0 failed. The sole test is empty and makes no assertions. |
| `dotnet format MsMqManager.sln --verify-no-changes --no-restore --verbosity minimal` | Failed with exit code 2: 115 formatting diagnostics and 121 analyzer warnings. Main groups were 105 `WHITESPACE`, 78 `IDE0011`, 19 `IDE1006`, and 17 `CA1416`. |

Build environment: Windows 10 build 22000, .NET SDK 10.0.401 selecting installed .NET 9 runtime 9.0.20 for `net9.0` projects. There is no `global.json`, so the repository does not pin an SDK.

### 3.3 Runtime/environment limitations

- The local `MSMQ` Windows service was verified as **Running** with automatic startup.
- A read-only MSMQ management smoke test returned 21 local queues containing 2,543 messages in 89.6 ms using `Get-MsmqQueue`; a separate journal query returned 21 journal queues in 73.7 ms. This is evidence for a management-count spike, not a production/remote benchmark.
- Querying the optional Windows feature state required elevation and was not available in this review session.
- No live queue was created, sent to, received from, purged, or reconfigured. This avoided mutating unknown local queue data.
- Remote MSMQ, Active Directory public queues, transactional queues, access-denied cases, WMI outgoing queues, and installer behavior were not exercised end to end.
- Consequently, findings labeled “confirmed defect” are established by code flow; library/OS behaviors that need real MSMQ validation are explicitly labeled as likely or unverified.

## 4. Positive Findings

- Scoped DI registrations keep connection and UI state per Blazor circuit rather than making them application-wide (`MsMqApp/Program.cs:81-90`).
- Most Razor components follow the code-behind convention, and the primary message grid uses `Virtualize` (`MsMqApp/Components/Shared/MessageList.razor:114-121`).
- Queue handles created directly in the main services are generally wrapped in `using`, and WMI searchers/results are disposed (`MsMqApp.Services/Implementations/MsmqService.cs:149-155`, `388-400`).
- Individual delete and purge have explicit confirmations; purge additionally requires the exact queue name and displays the anticipated count (`MsMqApp/Components/Shared/PurgeConfirmationDialog.razor:31-109`).
- Normal message browsing explicitly requests peek behavior, and individual journal deletion is blocked in the page handler (`MsMqApp/Components/Pages/Home.razor.cs:683-686`, `794-810`).
- Message content is rendered through normal Razor text expressions, so body content is HTML-encoded rather than inserted as raw markup (`MsMqApp/Components/Shared/MessageBodyViewer.razor:73-77`).
- Binary deserialization is deliberately rejected rather than using `BinaryFormatter` on untrusted queue data (`MsMqApp.Services/FormatHandlers/BinaryFormatHandler.cs:50-67`).
- Destructive operations use structured logging without logging message bodies. Production logging includes Windows Event Log on Windows (`MsMqApp/Program.cs:40-69`).
- The queue-tree model distinguishes private, public, outgoing, system, and journal views, which is a useful UI abstraction even though discovery is incomplete (`MsMqApp.Models/UI/QueueTreeBuilder.cs:32-82`).

## 5. Findings

### 5.1 AI Issue Severity & Priority Matrix (P0-P5)

The supplied matrix is used for finding classification. Because it was written for AI systems while this repository is an MSMQ administration application, its impact concepts are applied by analogy: active catastrophic incidents map to P0, active severe security/harm to P1, systemic degradation to P2, limited/cosmetic defects to P3, optimization/backlog work to P4, and tracking-only items to P5. Stable finding IDs are retained so references do not change when classifications are adjusted.

| Tier | Severity level | Core AI concerns and examples | Target action time |
|---|---|---|---|
| **P0** | **Catastrophic / Critical** | **Existential & Regulatory Emergencies:** Rogue model misalignment, autonomous exploitation of infrastructure, active weaponization vectors, or critical high-risk compliance breaches triggering immediate legal halts (for example, EU AI Act major violations). | **Immediate** — all-hands crisis response; halt model deployment. |
| **P1** | **High** | **Severe Output Harm & Security Exploits:** Models actively serving dangerous or severely harmful content, highly biased or legally exposed discriminatory decisions, mass privacy leaks, or active adversarial evasion/jailbreak injections. | **Urgent** — resolve within 1–4 hours using dedicated response teams. |
| **P2** | **Moderate** | **Systemic Model Drift & Intellectual Property Issues:** Significant degradation in accuracy, copyright/IP infringement risks, or unauthorized tool use/system-note injection that does not pose an immediate physical or infrastructure threat. | **Standard** — strategic planning or routine fix within 24–48 hours. |
| **P3** | **Low** | **Minor Technical Behavior & Edge Cases:** Limited edge-case failures affecting few users, cosmetic user-interface issues regarding AI outputs, or mild sycophancy. | **Normal work week** — handle through normal engineering sprints. |
| **P4** | **Negligible** | **Near-Misses & Backlog Wishlist:** Logged near-miss alignment errors that did not reach production, optimization tasks to reduce standard computational overhead, or minor response latencies. | **Opportunistic** — assign to the general product backlog. |
| **P5** | **Informational** | **Telemetry & Routine Logging:** Tracking baseline model-drift telemetry, non-urgent framework tracking data, or standard model maintenance updates with zero current operational impact. | **Review basis** — monitor through long-term engineering cycles. |

No active P0 or P1 incident was reported or observed in the reviewed environment. The large-server enumeration/count behavior is classified P2 because it is a systemic degradation rather than a minor latency optimization, and it is first in delivery order because it is the stated top product priority. Security exposure is classified P4 while access remains within the currently accepted environment; promote it to P1 immediately if active exploitation is detected, and at least P2 before exposure to untrusted users or networks.

“Type” distinguishes confirmed defects, likely risks, design improvements, and unverified concerns. Matrix tier and delivery order are intentionally separate: the priority field gives the supplied P0-P5 classification, while Section 11 gives the product implementation order.

### Security exposure

#### CR-001 — Network-reachable destructive administration has no authentication or authorization

- **Priority:** P4 — Accepted backlog hardening under current deployment assumptions; promote to P1 for active exploitation and at least P2 before untrusted exposure
- **Category:** Security / deployment / operational safety
- **Type:** Confirmed defect
- **Evidence:** Kestrel binds every interface (`MsMqApp/Program.cs:24-38`); HTTPS redirection is disabled (`MsMqApp/Program.cs:125-126`); the request pipeline contains antiforgery but no authentication or authorization (`MsMqApp/Program.cs:113-132`); the installer creates an inbound firewall rule (`installer/installer.nsi:138-140`); the home page directly exposes send, delete, purge, and queue-property mutation flows (`MsMqApp/Components/Pages/Home.razor:52-60`, `105-147`; `MsMqApp/Components/Shared/QueueProperties.razor.cs:106-173`). The PRD requires Windows Authentication exclusively and authenticated-user MSMQ permissions (`Documentation/prd.md:87`, `541-547`).
- **Current behavior:** Any network client able to reach the configured port receives an interactive management circuit. MSMQ calls execute as the service process identity; install scripts do not configure a lower-privilege service account (`install-service.ps1:118-125`, `install-from-dist.ps1:148-155`).
- **Why it matters:** This combines unauthenticated remote access with irreversible queue operations and, by default, a highly privileged service identity. Antiforgery protects against cross-site request forgery but does not authenticate a user or grant permissions.
- **Realistic failure scenario:** A user on the same network discovers port 9090, opens the site, connects to an accessible host, and purges or injects messages into production queues under the Windows Service account.
- **Recommended change:** Bind to loopback by default until security is configured. Add Windows Authentication at the host/server layer, `AddAuthentication`/`UseAuthentication`, `AddAuthorization`/`UseAuthorization`, a fallback authorization policy, and explicit policies for read versus destructive operations. Decide and document whether MSMQ access uses a fixed least-privilege service account or supported Windows impersonation; do not claim per-user MSMQ authorization unless calls truly run under that identity. Add HTTPS or a documented trusted reverse-proxy/TLS design before remote exposure. Restrict installer firewall rules by profile/address and make remote exposure opt-in.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** HI-001, ME-006; security design must precede broader/untrusted exposure and security-focused remote-access testing.

### Performance, operational safety, and identity

#### HI-001 — Synchronous MSMQ/WMI calls block Blazor circuits and ignore timeout/cancellation contracts

- **Priority:** P2 — Systemic performance and reliability degradation; delivery order 2
- **Category:** Correctness / concurrency / performance / remote reliability
- **Type:** Confirmed defect with one library behavior requiring runtime confirmation
- **Evidence:** Many `IMsmqService` methods perform synchronous queue calls and return `Task.FromResult`, including counts, list retrieval, peek/receive, delete, purge, send, and connection testing (`MsMqApp.Services/Implementations/MsmqService.cs:388-430`, `475-515`, `571-635`, `642-705`, `707-830`, `1019-1039`). `TestConnectionAsync` accepts `timeoutSeconds` but never uses it (`1019-1031`). Cancellation is checked only between already-completed operations. `PeekById`, `ReceiveById`, and delete use overloads with no explicit timeout (`587`, `619-621`, `655`).
- **Current behavior:** Remote WMI/MSMQ calls run on the Blazor circuit synchronization context. A cancellation token cannot interrupt a blocking native call, and the configured `RemoteConnectionTimeoutSeconds` is not injected or enforced.
- **Why it matters:** One slow/offline remote host or a message disappearing during an ID-based operation can make the UI unresponsive. The non-timeout `System.Messaging` ID operations are expected to wait indefinitely; this specific behavior should be confirmed against `Experimental.System.Messaging` in live tests.
- **Realistic failure scenario:** Auto-refresh calls a disconnected remote queue; the circuit thread blocks, the countdown continues to schedule work, and the user cannot cancel or navigate normally. A delete races another consumer and waits for an ID that no longer exists.
- **Recommended change:** Centralize synchronous MSMQ calls behind a bounded worker/execution abstraction. Use explicit MSMQ timeout overloads wherever available, linked cancellation and operation deadlines, and a concurrency limit per connection. Treat cancellation as cancellation rather than success with partial data. Propagate `ApplicationSettings.RemoteConnectionTimeoutSeconds`; do not advertise cancellation on interfaces where the underlying operation cannot honor it.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** HI-002, ME-002, ME-005.

#### HI-002 — Counts enumerate every message; bounded lists materialize bodies and leak returned resources

- **Priority:** P2 — Systemic queue enumeration/count degradation; delivery order 1
- **Category:** Performance / scalability / resource management
- **Type:** Confirmed defect
- **Evidence:** `GetQueuesAsync` calls `MsmqConverter.ToQueueInfo` for every private queue before it returns (`MsMqApp.Services/Implementations/MsmqService.cs:39-49`), and that converter counts via `GetAllMessages()` (`MsMqApp.Services/Helpers/MsmqConverter.cs:36-45`). `ConnectAsync` then performs a sequential journal-count lookup for every eligible queue before `RefreshQueues`/`MarkConnected` (`MsMqApp.Services/Implementations/QueueConnectionManager.cs:112-145`), and journal counts also use `GetAllMessages()` (`MsMqApp.Services/Implementations/MsmqService.cs:432-454`). List retrieval calls `GetAllMessages()` before applying `maxMessages` (`475-503`, `517-549`); `MessageReadPropertyFilter.SetAll()` requests all properties (`490`, `536`); each selected body is fully copied and decoded (`MsMqApp.Services/Helpers/MsmqConverter.cs:184-235`). Returned `Message[]` elements are not disposed. The configured 1 MB body limit and page size are not used in this path (`MsMqApp.Models/Configuration/ApplicationSettings.cs:13-21`).
- **Current behavior:** Queue identity is coupled to exact counting. The UI receives no private queue list until every base queue has enumerated all messages and every eligible journal has then been scanned sequentially. Requesting at most 1,000 message rows likewise materializes the whole queue first. Virtualization reduces DOM work only; it does not bound MSMQ reads or server allocations.
- **Why it matters:** The initial connection cost scales primarily with the total number of messages across base queues and journals, not merely with approximately 100 queue objects. Large messages multiply allocation pressure, and undisposed `Message`/stream resources can accumulate over repeated refreshes.
- **Realistic failure scenario:** A server with roughly 100 queues has several thousand journal messages plus stale test queues containing many unprocessed messages. Initial load takes minutes even though operators only need queue names first; after the scans complete, the application performs acceptably.
- **Recommended change:** Decouple queue discovery from counts immediately. Publish queue identities as soon as discovery returns, using an explicit unknown/loading count state. For exact base and journal counts, first spike the native MSMQ management API `MQMgmtGetInfo` with `PROPID_MGMT_QUEUE_MESSAGE_COUNT` and `PROPID_MGMT_QUEUE_JOURNAL_MESSAGE_COUNT`, which are designed to return counts without retrieving message bodies. Isolate native interop behind a testable `IMsmqQueueMetricsReader`, handle inactive/access-denied queues as unavailable rather than blocking discovery, and populate counts in the background with bounded concurrency. Use narrow disposable cursor enumeration only as a fallback. Separately implement cursor/page-based bounded message retrieval, lazy selected-body loading, and byte limits.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** HI-001, ME-008; validate exact enumeration semantics with the experimental library.

#### HI-003 — Bulk delete can remove journal messages despite the read-only safety rule

- **Priority:** P2 — High-priority correctness and data-safety work
- **Category:** MSMQ operational safety / correctness
- **Type:** Confirmed defect
- **Evidence:** The bulk selection/delete footer is rendered for every view, including journal messages (`MsMqApp/Components/Shared/MessageList.razor:126-170`). Bulk delete explicitly chooses `SelectedQueue.JournalPath` for a journal view and calls `DeleteMessagesAsync` (`MsMqApp/Components/Shared/MessageList.razor.cs:447-488`). That service loops through `DeleteMessageAsync`, which removes via `ReceiveById` (`MsMqApp.Services/Implementations/MessageOperationsService.cs:615-689`; `MsMqApp.Services/Implementations/MsmqService.cs:642-658`). By contrast, individual delete is explicitly blocked as read-only (`MsMqApp/Components/Pages/Home.razor.cs:794-810`).
- **Current behavior:** The UI presents contradictory policies: individual journal messages cannot be deleted, but selecting the same messages in bulk exposes a confirmed delete path.
- **Why it matters:** Journal data is intended as an archive and the UI promises it is read-only. This bypass can cause irreversible data loss.
- **Realistic failure scenario:** An operator opens a journal view, selects all visible rows, confirms bulk delete, and removes journal evidence they expected could only be cleared by the more explicit purge workflow.
- **Recommended change:** Hide/disable bulk delete for `JournalMessages` and enforce the restriction in the application/service layer, not only the component. If journal deletion is a product requirement, define it explicitly with equivalent or stronger confirmation and authorization. Add component and service tests proving that individual and bulk journal deletes are rejected.
- **Estimated effort:** Small
- **Confidence:** High
- **Related findings or prerequisites:** CR-001, ME-010.

#### HI-004 — Journal path construction is inconsistent and refresh appends the journal suffix twice

- **Priority:** P2 — High-priority correctness work
- **Category:** MSMQ correctness / queue paths
- **Type:** Confirmed defect
- **Evidence:** `GetJournalMessageCountAsync` always derives a journal path by appending `;journal` (`MsMqApp.Services/Implementations/MsmqService.cs:432-454`). Initial connection passes `queue.Path` (`MsMqApp.Services/Implementations/QueueConnectionManager.cs:112-130`), while home refresh passes the already-derived `queue.JournalPath` (`MsMqApp/Components/Pages/Home.razor.cs:338-363`). `QueueInfo.JournalPath` itself has separate format-name and path-rewrite rules (`MsMqApp.Models/Domain/QueueInfo.cs:85-124`). Message loading then consumes `JournalPath` directly (`MsMqApp/Components/Pages/Home.razor.cs:671-677`).
- **Current behavior:** The same service parameter sometimes means “base queue path” and sometimes receives a journal path. During home refresh, a valid format name such as `DIRECT=...;journal` is passed to a method that produces `FormatName:DIRECT=...;journal;journal`.
- **Why it matters:** Counts, badges, and purge availability can be wrong even when journal message loading works. Multiple competing path algorithms make local, remote, private, public, and system behavior hard to reason about.
- **Realistic failure scenario:** A queue has journal messages. Initial or manual refresh reports zero or failure, hides the count, and later operations target a differently constructed path than the one used for display.
- **Recommended change:** Introduce one tested queue-address value/helper that distinguishes base queue, format name, and journal address. Make APIs explicit (`GetJournalMessageCountAsync(baseQueueAddress)` or `GetMessageCountAsync(journalAddress)`) and never derive twice. Add table-driven tests for `.`, machine names, private/public paths, direct format names, machine journal, queue journal, dead-letter, and transactional dead-letter addresses.
- **Estimated effort:** Medium
- **Confidence:** High for the double-suffix defect; Medium for which path variants the experimental package accepts
- **Related findings or prerequisites:** ME-001; requires live local/remote MSMQ path validation.

#### HI-005 — Service identity does not match the PRD's authenticated-user permission model

- **Priority:** P4 — Accepted backlog identity hardening under current deployment assumptions
- **Category:** Security / architecture / remote access
- **Type:** Confirmed architecture mismatch
- **Evidence:** The PRD requires Windows Authentication for remote connections and that the application respect the authenticated user's MSMQ permissions (`Documentation/prd.md:153-159`, `541-547`). `ConnectAsync` accepts a username but only stores it on the model (`MsMqApp.Services/Implementations/QueueConnectionManager.cs:37-68`); all MSMQ/WMI calls are direct process calls. No request user, impersonation token, or authorization context enters the service layer. Install scripts create the service without an explicit credential (`install-service.ps1:118-125`, `install-from-dist.ps1:148-155`).
- **Current behavior:** Local and remote MSMQ access uses the Windows Service process token, not the browser user's token. The optional username parameter has no authentication effect.
- **Why it matters:** Adding a login screen or Windows Authentication alone would not satisfy least privilege. Users could receive the service account's queue privileges, or remote access could unexpectedly use the machine account.
- **Realistic failure scenario:** A read-only domain user signs in after Windows Authentication is added, but a purge succeeds because the backend still executes under a more privileged service identity.
- **Recommended change:** Make an explicit supported identity decision. Prefer a dedicated least-privilege service account plus application authorization and documented fixed privileges, unless tested Windows impersonation across Blazor circuit work and remote MSMQ is a hard requirement. Remove the misleading username parameter or implement it securely without storing passwords. Document double-hop/delegation constraints before promising per-user remote access.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** CR-001.

### Functionality, concurrency, and maintainability

#### ME-001 — Public queues are never discovered although interfaces and requirements say all queues

- **Priority:** P3 — Limited product-scope gap
- **Category:** Functional correctness / PRD compliance
- **Type:** Confirmed missing implementation
- **Evidence:** FR-001/FR-002 require application queues, private and public (`Documentation/prd.md:129-149`). `IMsmqService` says discovery queries both (`MsMqApp.Services/Interfaces/IMsmqService.cs:18-38`), but `GetQueuesAsync` calls only `GetPrivateQueuesByMachine`, fixed system discovery, and outgoing WMI discovery (`MsMqApp.Services/Implementations/MsmqService.cs:26-71`).
- **Current behavior:** Public queues are absent unless accidentally surfaced/misclassified through WMI outgoing discovery.
- **Why it matters:** A core monitoring scope is silently incomplete and the interface documentation overstates actual behavior.
- **Realistic failure scenario:** An operator concludes a public queue does not exist because it never appears in the tree, then investigates the wrong server or path.
- **Recommended change:** Implement supported public-queue discovery with clear Active Directory/domain prerequisites and bounded timeouts, or explicitly remove public queues from the supported scope and correct the PRD/interface documentation.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** HI-001, HI-004.

#### ME-002 — Refresh operations can overlap and stale results can replace a newer selection

- **Priority:** P2 — Systemic refresh reliability/performance degradation
- **Category:** Blazor lifecycle / concurrency / state correctness
- **Type:** Likely risk
- **Evidence:** The timer callback is `async void` and invokes refresh whenever its local `IsRefreshing` snapshot is false (`MsMqApp/Components/Shared/RefreshControl.razor.cs:240-263`). Manual refresh, auto-refresh, queue selection, purge follow-up, and send follow-up all call the same unguarded loaders (`MsMqApp/Components/Pages/Home.razor.cs:126-168`, `649-710`, `757-773`). There is no semaphore, request generation, or per-selection `CancellationTokenSource`.
- **Current behavior:** Multiple loads may run concurrently. Each completion writes the shared `Messages` property without verifying that the selected queue/view is still the request target.
- **Why it matters:** Remote latency makes out-of-order completion realistic; `async void` also makes timer exceptions difficult to observe and coordinate with disposal.
- **Realistic failure scenario:** The user selects queue B while a slow refresh of queue A is running. Queue B loads first; queue A completes later and its messages appear under the queue B header.
- **Recommended change:** Use a cancellable `PeriodicTimer`/component-owned loop or a guarded timer callback, serialize refresh work, cancel superseded loads, and commit results only when a request version still matches the selected queue/view. Catch/log timer callback failures and stop cleanly on disposal.
- **Estimated effort:** Medium
- **Confidence:** High that overlap is possible; Medium on observed frequency
- **Related findings or prerequisites:** HI-001, ME-006.

#### ME-003 — User-visible dates bypass the requested centralized display policy

- **Priority:** P3 — Planned consistency improvement
- **Category:** UX / localization / maintainability
- **Type:** Confirmed requirement gap
- **Evidence:** Detail timestamps use hard-coded `yyyy-MM-dd HH:mm:ss.fff` (`MsMqApp/Components/Shared/MessageDetail.razor:28-32`); row tooltips use another hard-coded format and row text uses server-culture month/time formats (`MsMqApp/Components/Shared/MessageRow.razor:27-28`; `MessageRow.razor.cs:126-162`); send feedback uses `HH:mm:ss` (`MsMqApp/Components/Pages/Home.razor.cs:585`); component demo and template weather use still other formats (`MsMqApp/Components/Pages/ComponentDemo.razor:317`; `Weather.razor:29`). No date/time formatting abstraction exists.
- **Current behavior:** Formatting is scattered, inconsistent, and based on server culture/time rather than a user preference or browser locale. The static filename timestamp is not user-visible display and can remain invariant.
- **Why it matters:** It violates the supplied priority: user-selected preference first, browser convention second, centralized local-display fallback third, consistently across all displayed dates/times. Future policy changes would require editing many components.
- **Realistic failure scenario:** A user expecting day/month order sees ISO in details, English month names in rows, and a 24-hour time in success feedback.
- **Recommended change:** Add an overridable scoped `IDateTimeDisplayFormatter` (or equivalent) with a single `FormatDateTime`/`FormatDate` policy. Resolve a saved user preference first; otherwise use browser `Intl.DateTimeFormat`/locale information; provide a local/default-culture fallback for prerendering or disconnected JS. Apply it to every user-visible date/time. Keep time-zone architecture intentionally small unless a concrete conversion defect emerges.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** Add formatter unit tests and component coverage in ME-011.

#### UX-001 — Visual styling is not yet aligned to the company color and typography system

- **Priority:** P3 — Requested cosmetic/UX standardization work
- **Category:** UX / visual design / accessibility / maintainability
- **Type:** Confirmed requirement gap; final values depend on company design inputs
- **Evidence:** The app defines useful light/dark variables, but the values are the default Bootstrap gray/blue family (`MsMqApp/wwwroot/app.css:32-64`). The global font stack is generic Helvetica/Arial (`MsMqApp/wwwroot/app.css:71-75`). Links, buttons, validation, dialog states, navigation, hover/focus states, and overlays repeat hard-coded hex/RGBA values outside semantic tokens (`MsMqApp/wwwroot/app.css:85-119`, `423-457`, `720-783`, `997-1019`). The PRD mentions dark mode but does not define company palette, typography, spacing, or component standards (`Documentation/prd.md:729-755`).
- **Current behavior:** Light/dark modes work, but the visual language is primarily generic Bootstrap. Component states can drift because the same colors and font decisions are repeated rather than sourced from a complete semantic token layer.
- **Why it matters:** The requested company alignment cannot be applied consistently by changing a small, reviewed set of values. Ad hoc color replacement can also damage contrast, destructive/warning semantics, focus visibility, and dark-mode behavior.
- **Realistic failure scenario:** A company primary color is substituted only for buttons and navigation, while dialogs, links, focus rings, status colors, and dark mode retain Bootstrap blue or become unreadable against their backgrounds.
- **Recommended change:** Obtain the approved company brand inputs: primary/secondary/accent colors, neutral ramp, semantic success/warning/danger/info colors, light/dark variants, font families/files and licensing, type scale/weights, spacing/radius rules, logo/icon guidance, and WCAG target. Map them into semantic CSS custom properties (for example surface, text, border, action, focus, and status tokens), then override Bootstrap variables from those tokens. Migrate components away from raw color/font values, create a representative component/theme page, and verify contrast, focus, disabled, hover, selected, destructive, and dark-mode states. Keep the token layer replaceable so future company-theme revisions do not require component rewrites.
- **Estimated effort:** Medium after brand assets/specification are available; Large if a design system must be derived
- **Confidence:** High that tokenization is needed; Medium on final scope until brand guidance is supplied
- **Related findings or prerequisites:** LO-002, LO-003, ME-011; requires approved company design inputs.

#### ME-004 — Binary bodies are commonly misclassified as UTF-8 text

- **Priority:** P2 — Message-fidelity defect with replay implications
- **Category:** Message decoding / correctness
- **Type:** Confirmed defect
- **Evidence:** Body extraction calls `Encoding.UTF8.GetString(bytes)` and expects an exception to signal binary data (`MsMqApp.Services/Helpers/MsmqConverter.cs:190-222`). The default UTF-8 decoder replaces invalid sequences rather than throwing. `MessageBody.DetectFormat` repeats this assumption and then treats any printable replacement output as text (`MsMqApp.Models/Domain/MessageBody.cs:96-148`).
- **Current behavior:** Arbitrary binary bytes usually become a string containing replacement characters and are detected/displayed as text instead of hex.
- **Why it matters:** Operators may inspect corrupted-looking text and misdiagnose payloads. Re-exports/resends may not preserve byte fidelity if text paths are used.
- **Realistic failure scenario:** A protobuf or compressed message displays as meaningless text, and a resend serializes that replacement text rather than the original byte stream.
- **Recommended change:** Detect strict UTF-8 with `new UTF8Encoding(false, true)`, retain raw bytes as authoritative, and use content/formatter metadata where available. Never discard byte fidelity. Add malformed UTF-8, embedded-null, valid Unicode, and large-binary tests.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** HI-002, ME-009.

#### ME-005 — Connection and message service contracts contain stubs and misleading success results

- **Priority:** P3 — Limited functional contract failures
- **Category:** Architecture / interface correctness / maintainability
- **Type:** Confirmed defect
- **Evidence:** `DisconnectAllAsync`, `RefreshAllConnectionsAsync`, auto-refresh start/stop/pause/resume, persistence clear/save, and health-related methods return success/default values without performing their documented action (`MsMqApp.Services/Implementations/QueueConnectionManager.cs:200-245`, `331-409`). Reconnect and settings update return “Not yet implemented” (`206-213`, `348-358`). `MsmqService.MoveMessageAsync` is also unimplemented (`MsMqApp.Services/Implementations/MsmqService.cs:667-678`). `GetMessagesAsync(peekOnly:false)` still calls `GetAllMessages()` and does not receive/remove anything (`475-503`).
- **Current behavior:** Consumers can receive a successful result for operations that did nothing, and an explicitly destructive `peekOnly:false` contract is silently ignored.
- **Why it matters:** False success is harder to diagnose than a clear unsupported-operation failure. The broad interfaces imply capabilities the implementation does not provide.
- **Realistic failure scenario:** Shutdown or future admin code calls `DisconnectAllAsync`, records success, and assumes resources/state were cleared; they were not.
- **Recommended change:** Shrink interfaces to currently supported use cases or return explicit `NotSupported` failures until implemented. Implement move with documented transaction/partial-failure semantics before exposing it. Split connection persistence/refresh orchestration from live connection lookup if those responsibilities are actually needed.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** ME-009, ME-011.

#### ME-006 — Refresh configuration is process-global, ignores app settings, and violates PRD bounds

- **Priority:** P2 — Systemic refresh performance/configuration degradation
- **Category:** Blazor state / configuration / PRD compliance
- **Type:** Confirmed defect
- **Evidence:** `HomeBase` stores the interval in a static mutable field defaulting to 10 seconds (`MsMqApp/Components/Pages/Home.razor.cs:19-20`, `69-73`). The configured default is 5 seconds (`MsMqApp/appsettings.json:16-20`) and FR-032 also requires 5 seconds (`Documentation/prd.md:311-317`). The input permits 1-3,600 seconds rather than 1-60 (`MsMqApp/Components/Shared/RefreshControl.razor:32-41`). Pause state lives only inside `MessageList` (`MsMqApp/Components/Shared/MessageList.razor.cs:132-140`).
- **Current behavior:** One user's interval change affects every circuit in the process. Configuration is unused, and accepted bounds differ from the PRD.
- **Why it matters:** This is cross-user state leakage and surprising behavior in a multi-user Blazor Server application.
- **Realistic failure scenario:** One operator changes refresh to one hour; every other connected operator silently stops receiving timely updates.
- **Recommended change:** Keep interval/pause state in a scoped per-circuit preference service, initialize it from `IOptions<ApplicationSettings>`, validate 1-60 seconds, and optionally persist the user's choice in browser storage. Do not use a static field for a user preference.
- **Estimated effort:** Small
- **Confidence:** High
- **Related findings or prerequisites:** ME-002, ME-003.

#### ME-007 — Errors are frequently swallowed while debug output leaks internal topology

- **Priority:** P3 — Planned diagnostics improvement
- **Category:** Diagnostics / logging / usability
- **Type:** Confirmed defect
- **Evidence:** Queue refresh catches all exceptions and only marks the connection failed (`MsMqApp/Components/Pages/Home.razor.cs:320-380`); export, resend, delete, and purge handlers contain empty catches/TODO notifications (`837-888`, `926-983`); bulk delete has a TODO instead of user-visible failure handling (`MsMqApp/Components/Shared/MessageList.razor.cs:457-488`). Conversely, queue selection serializes the entire `QueueInfo` to console and numerous production code paths use `Console.WriteLine` debug messages (`MsMqApp/Components/Pages/Home.razor.cs:142-167`, `656-703`), and the UI itself renders a “Debug” line (`MsMqApp/Components/Shared/MessageList.razor:134-137`).
- **Current behavior:** Operators may see stale/empty state without an actionable error, while server/browser diagnostics expose queue paths, machine names, and implementation details outside the structured logging policy.
- **Why it matters:** MSMQ failures are often permission/network specific; suppressing the exception removes the information needed to recover. Ad hoc debug output is noisy and can disclose infrastructure details.
- **Realistic failure scenario:** A remote access-denied error clears the grid, no error is shown, and the user assumes the queue is empty.
- **Recommended change:** Route errors through `ILogger` with stable event IDs and safe structured fields; show sanitized, actionable circuit feedback with retry guidance. Remove debug UI/console serialization. Keep message bodies and credentials out of logs, and define whether queue names/paths are sensitive in the deployment threat model.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** CR-001, HI-001.

#### ME-008 — Search/filter behavior and the 1,000-message cap do not meet the PRD

- **Priority:** P2 — Systemic bounded-retrieval/search degradation
- **Category:** Functional completeness / scalability
- **Type:** Confirmed requirement gap
- **Evidence:** The page hard-caps retrieval at 1,000 (`MsMqApp/Components/Pages/Home.razor.cs:671-686`) while FR-008 says all messages and NFR-002 covers queues up to 10,000 (`Documentation/prd.md:169-183`, `517-523`). Search checks only label, ID, and correlation ID (`MsMqApp/Components/Shared/MessageList.razor.cs:503-537`); it omits body search, highlighting, priority/date/size filters, combined filters, and active-filter indicators required by FR-024 through FR-030 (`Documentation/prd.md:271-301`).
- **Current behavior:** Results beyond the first 1,000 are invisible and unsearchable, with no indication that data is truncated. Virtualization is applied only after loading/filtering all returned rows in memory.
- **Why it matters:** Operators can incorrectly conclude a message is absent. Increasing the cap alone would worsen HI-002.
- **Realistic failure scenario:** A poison message at position 1,500 cannot be found by ID or body content even though the UI reports a normal message list.
- **Recommended change:** Implement explicit server-side/bounded paging or continuation, show loaded/total/truncated state, and execute supported filters against the intended dataset. Prioritize exact ID lookup and metadata filters before expensive body search. Add body search only with body-size and workload limits.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** HI-002, ME-011.

#### ME-009 — Resend/export semantics are lossy and UI workflows are incomplete

- **Priority:** P3 — Limited workflow completion gap
- **Category:** MSMQ correctness / feature completeness
- **Type:** Confirmed design and requirement gap
- **Evidence:** The converter hard-codes `IsTransactional = false` and `Authenticated = false` (`MsMqApp.Services/Helpers/MsmqConverter.cs:78-96`, `173-179`). `ConvertToSystemMessage` restores only a subset of headers and silently ignores invalid response queues (`MsMqApp.Services/Implementations/MsmqService.cs:834-945`). The home “resend” sends to the currently selected queue, not a preserved original destination (`MsMqApp/Components/Pages/Home.razor.cs:865-888`). Export writes into the Windows Service account's Documents folder (`837-862`). Move is a placeholder (`827-832`), and move/export/resend buttons are commented out (`MsMqApp/Components/Shared/MessageDetail.razor:106-116`).
- **Current behavior:** These operations either are not available or do not preserve the semantics implied by “resend original message” and “export to user.”
- **Why it matters:** MSMQ headers, transaction requirements, raw bytes, and destination determine delivery behavior. Silent header loss can create duplicates or messages that consumers treat differently.
- **Realistic failure scenario:** A transactional dead-letter message is resent as non-transactional with changed body/header representation and fails or is processed incorrectly; an export succeeds on the server but the browser user cannot obtain the file.
- **Recommended change:** Define a canonical export/replay envelope containing original metadata and MIME/base64 body as the PRD notes. Preserve raw bytes, explicitly map supported headers, and report unsupported ones. Resolve destination from actual metadata or require operator selection. Deliver exports to the browser. For move, prefer a transaction where MSMQ supports an atomic receive/send across compatible transactional queues; otherwise expose copy-then-delete partial-failure state and idempotency guidance.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** HI-004, ME-004, ME-005.

#### ME-010 — Bulk operations report success after partial failure

- **Priority:** P2 — High-priority destructive-operation correctness
- **Category:** Error handling / operational safety
- **Type:** Confirmed defect
- **Evidence:** Bulk delete collects failed IDs but unconditionally returns `OperationResult.Successful()` (`MsMqApp.Services/Implementations/MessageOperationsService.cs:648-689`). The component treats any success as complete and sends every requested ID to `OnMessagesDeleted` (`MsMqApp/Components/Shared/MessageList.razor.cs:457-480`). Bulk export follows a similar “success with failures in metadata” convention (`MsMqApp.Services/Implementations/MessageOperationsService.cs:465-539`).
- **Current behavior:** Partial success is encoded only in loosely typed metadata and is easy for callers to ignore.
- **Why it matters:** Operators need to know exactly which destructive operations succeeded so retrying does not duplicate work or hide failures.
- **Realistic failure scenario:** Five messages are selected, two race with another consumer, and three are deleted. The UI announces success and temporarily removes all five IDs; no failure list is shown.
- **Recommended change:** Return a typed bulk result with succeeded, failed, and not-found IDs plus per-item errors. Treat partial success as a distinct status and update only succeeded rows. Make retries idempotent where possible.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** HI-003, ME-011.

#### ME-011 — Automated tests provide no behavioral protection

- **Priority:** P3 — Test foundation for the P2 delivery work
- **Category:** Testing / testability
- **Type:** Confirmed gap
- **Evidence:** The only test is an empty method (`MsMqApp.Tests/UnitTest1.cs:1-10`). Test execution reports one passing test solely because it has no assertions. The service layer directly constructs static/concrete MSMQ types, making isolated testing difficult (`MsMqApp.Services/Implementations/MsmqService.cs:39-48`, `388-400`).
- **Current behavior:** None of the destructive, path, transaction, parser, timeout, concurrency, or component behaviors are covered.
- **Why it matters:** The most consequential defects in this review—journal bulk deletion, double journal suffix, ignored `peekOnly`, and false bulk success—are all straightforward regression-test candidates.
- **Realistic failure scenario:** A path or receive change compiles and the single test remains green while browsing begins removing messages or a journal purge targets the wrong path.
- **Recommended change:** Add a narrow adapter around `Experimental.System.Messaging` so path/operation policy can be unit-tested without MSMQ, plus opt-in Windows integration tests against uniquely named disposable queues. Add bUnit tests for confirmation, journal restrictions, stale refresh suppression, disposal, and date formatting. Gate CI on meaningful tests and formatter/analyzer verification.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** All P2 delivery findings and UX-001.

#### ME-012 — Very large classes and one synchronous wait violate the repository's own design rules

- **Priority:** P4 — Maintainability/optimization backlog
- **Category:** Maintainability / Blazor correctness / coding standards
- **Type:** Confirmed defect and design improvement
- **Evidence:** `MsmqService.cs` is 1,093 lines, `MessageOperationsService.cs` 856, `Home.razor.cs` 1,182, `ComponentDemo.razor.cs` 1,151, `SendMessageDialog.razor.cs` 711, and `QueueTreeView.razor.cs` 677. Coding standards set 250 lines for C# files and 300 for classes (`Documentation/coding-standards.md:319-349`), while README/CLAUDE state different but still exceeded limits (`README.md:96-104`; `CLAUDE.md:53-58`). `QueueProperties.OnApply` blocks on `OnSaveAsync().GetAwaiter().GetResult()` (`MsMqApp/Components/Shared/QueueProperties.razor.cs:185-193`). `NavMenu.razor` contains an `@code` block despite the mandatory code-behind rule (`MsMqApp/Components/Layout/NavMenu.razor:33-45`).
- **Current behavior:** Queue discovery, path policy, counting, retrieval, sending, WMI, and system queues are concentrated in one service; home owns most workflows. The Apply button can deadlock a Blazor circuit when the async save yields (`QueueManagementService` uses `Task.Run` at `MsMqApp.Services/Implementations/QueueManagementService.cs:50-90`).
- **Why it matters:** High responsibility density obscures safety invariants and makes testing difficult. Sync-over-async on a circuit is a direct responsiveness risk.
- **Realistic failure scenario:** Clicking Apply waits synchronously for an async continuation that needs the blocked circuit context, freezing that circuit.
- **Recommended change:** Make Apply async end to end. Split by demonstrated responsibilities: queue-address policy, discovery/counts, bounded message reader, message writer, and page workflow/state controller. Move `NavMenu` logic to code-behind. Reconcile and automate one file-size standard rather than performing arbitrary abstraction-only splits.
- **Estimated effort:** Large
- **Confidence:** High
- **Related findings or prerequisites:** HI-001, HI-002, ME-011.

### Quality and accessibility

#### LO-001 — Template/demo/debug artifacts remain in the shipped application

- **Priority:** P4 — Cleanup
- **Category:** Cleanup / documentation / professionalism
- **Type:** Confirmed cleanup issue
- **Evidence:** Template `Counter` and `Weather` pages remain routable (`MsMqApp/Components/Pages/Counter.razor:1-19`; `Weather.razor:1-64`), the 548-line component demo is linked in production navigation (`MsMqApp/Components/Layout/NavMenu.razor:13-19`), and `/settings` is linked without a corresponding page (`NavMenu.razor:24-30`). `App.razor` contains a second debug hamburger handler (`MsMqApp/Components/App.razor:143-171`). The message footer exposes implementation debug state (`MsMqApp/Components/Shared/MessageList.razor:134-137`).
- **Current behavior:** Users see incomplete/demo navigation and duplicate debug behavior.
- **Why it matters:** It creates confusion and increases the shipped/test surface.
- **Realistic failure scenario:** An operator clicks Settings and gets no intended settings experience, or the duplicate hamburger listeners toggle the drawer inconsistently.
- **Recommended change:** Remove template routes and debug output from production, or conditionally expose a development-only component catalog. Implement or remove Settings navigation.
- **Estimated effort:** Small
- **Confidence:** High
- **Related findings or prerequisites:** ME-007, ME-012.

#### LO-002 — JavaScript listeners and high-frequency theme monitoring are not cleanly owned

- **Priority:** P4 — Browser resource optimization backlog
- **Category:** Client performance / lifecycle / maintainability
- **Type:** Confirmed design issue
- **Evidence:** `App.razor` runs a theme check every 25 ms and installs global observers/listeners without teardown (`MsMqApp/Components/App.razor:58-132`). `HomeBase` adds document listeners via `eval` (`MsMqApp/Components/Pages/Home.razor.cs:103-117`) but disposal nulls globals rather than calling `removeEventListener`, and one cleanup name does not match the registered function (`1149-1159`; `MsMqApp/wwwroot/js/homepage.js:16-37`).
- **Current behavior:** Enhanced navigation/component recreation can retain global listeners; the theme checker wakes roughly 40 times per second for the lifetime of the page.
- **Why it matters:** This is avoidable browser work and can lead to callbacks against disposed .NET references.
- **Realistic failure scenario:** Re-entering the home page accumulates resize listeners, producing duplicate JS-to-.NET calls or rejected interop promises.
- **Recommended change:** Replace `eval` with a small JS module that returns an `IJSObjectReference`/disposable registration. Add/remove exact listeners and disconnect observers. Apply the theme once plus on explicit storage/theme events; remove the 25 ms poll.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** ME-002.

#### LO-003 — Message rows and dialogs have keyboard/focus accessibility gaps

- **Priority:** P3 — Planned accessibility improvement
- **Category:** Accessibility / UI quality
- **Type:** Design improvement
- **Evidence:** A message row is a clickable `<div>` without button/row semantics, `tabindex`, or keyboard handler (`MsMqApp/Components/Shared/MessageRow.razor:4-34`). Dialogs provide ARIA labels but do not implement focus trapping, initial focus management consistently, or Escape handling (`MsMqApp/Components/Shared/ConfirmationDialog.razor:4-80`; `PurgeConfirmationDialog.razor:4-112`).
- **Current behavior:** Mouse users can select rows and operate dialogs more easily than keyboard or assistive-technology users.
- **Why it matters:** Queue administration should be operable without a pointing device, especially during incident response.
- **Realistic failure scenario:** A keyboard user cannot focus/select a message row or loses focus behind a modal dialog.
- **Recommended change:** Use semantic table/grid/button patterns or add `role`, `tabindex`, Enter/Space handlers, visible focus, focus trap/restore, and Escape behavior. Verify with keyboard-only and screen-reader checks.
- **Estimated effort:** Medium
- **Confidence:** High
- **Related findings or prerequisites:** None.

#### LO-004 — Formatting/analyzer policy is not enforced by normal builds

- **Priority:** P4 — Tooling cleanup
- **Category:** Tooling / code quality
- **Type:** Confirmed process gap
- **Evidence:** `dotnet build` reports zero warnings, while `dotnet format --verify-no-changes` reports 115 errors and 121 warnings. Examples include whitespace, missing braces, naming violations, unused code, and Windows-only API warnings. `.editorconfig` marks several styles as warnings, but the project files do not enable enforcement during build (`MsMqApp/MsMqApp.csproj:1-17`; `MsMqApp.Services/MsMqApp.Services.csproj:1-20`).
- **Current behavior:** A clean build does not mean repository formatting/analyzer compliance.
- **Why it matters:** The documented standards claim enforcement, but contributors receive a green build for noncompliant changes.
- **Realistic failure scenario:** CI runs only build/test, so formatting and platform diagnostics continue accumulating unnoticed.
- **Recommended change:** Add `dotnet format --verify-no-changes` to CI and deliberately configure analyzer enforcement/severity. Mark Windows-only projects/APIs with supported-platform metadata instead of suppressing useful diagnostics indiscriminately.
- **Estimated effort:** Small
- **Confidence:** High
- **Related findings or prerequisites:** ME-012.

### Documentation

#### IN-001 — Documentation describes several different products and defaults

- **Priority:** P5 — Informational documentation tracking
- **Category:** Documentation accuracy
- **Type:** Confirmed inconsistency
- **Evidence:** `CLAUDE.md` says core MSMQ integration and tests do not exist (`CLAUDE.md:7-9`, `32-35`, `198-207`), while the repository contains both. README marks remote connections and bulk operations as not done although connect, delete, and purge are partly implemented (`README.md:133-146`). PRD/CLAUDE/deployment docs mostly say port 8080 (`Documentation/prd.md:97`, `583`; `CLAUDE.md:111`), while runtime and installer defaults are 9090 (`MsMqApp/appsettings.json:9-14`; `build-installer.ps1:52`; `installer/installer.nsi:185`). File-size limits also disagree (`Documentation/coding-standards.md:319-349`; `README.md:96-104`; `CLAUDE.md:53-58`).
- **Current behavior:** A maintainer cannot tell from one authoritative source which features, port, and standards apply.
- **Why it matters:** Stale operational/security instructions create deployment mistakes and distort review expectations.
- **Realistic failure scenario:** An administrator opens/configures 8080 while the installed service listens on 9090, or assumes Windows Authentication is active because the documentation says it is.
- **Recommended change:** Designate the PRD and one operations guide as authoritative, add an implementation-status matrix, choose one default port, reconcile standards, and update README/CLAUDE after fixes. Clearly label planned versus implemented behavior.
- **Estimated effort:** Small
- **Confidence:** High
- **Related findings or prerequisites:** CR-001, ME-005, ME-012.

## 6. Performance and Scalability

### Confirmed bottlenecks

- **Full scans for counts and lists:** `GetAllMessages()` appears in queue conversion, counts, journal counts, purge pre-counts, and message loading. It fetches bodies even when only a count or first page is needed.
- **N+1 journal requests:** connection and queue refresh scan every application queue and then issue a sequential journal-count request per queue (`QueueConnectionManager.cs:112-137`, `283-308`; `Home.razor.cs:338-365`).
- **Blocking remote calls:** MSMQ and WMI calls occupy the Blazor circuit thread, violating NFR-004's non-blocking requirement.
- **Repeated allocations:** body extraction stores both `byte[]` and decoded `string`; viewer formatting creates additional arrays/strings; XML/JSON formatting reparses content; binary hex rendering expands data substantially.
- **Rendering versus retrieval mismatch:** `Virtualize` limits rendered rows but filtering/sorting copies and processes the full loaded collection (`MessageList.razor.cs:503-537`).
- **Aggressive client polling:** theme state is checked every 25 ms for the entire page lifetime.

### Confirmed production context

- Some target servers have approximately 100 queues.
- Several thousand messages may be concentrated in queue journals. Stale test queues may also contain large numbers of unprocessed messages.
- Initial queue display can take several minutes, while interaction performs well after the initial load completes.
- The desired behavior is to publish the queue list immediately; exact message counts must not gate the first usable tree.

This profile confirms that the main problem is not rendering 100 queue nodes. It is the current O(total base messages + total journal messages) work performed before `ConnectAsync` publishes those nodes.

### Recommended count mechanism spike

1. **Preferred exact-count path:** call the native MSMQ management API `MQMgmtGetInfo` once per application queue and request both `PROPID_MGMT_QUEUE_MESSAGE_COUNT` and `PROPID_MGMT_QUEUE_JOURNAL_MESSAGE_COUNT` in the same property set. Microsoft documents these as read-only management counts, including the journal count; the call accepts local, UNC, DNS, or IP machine names and `QUEUE=<formatname>` as its queue object. See [MQMgmtGetInfo](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms700118(v=vs.85)), [queue management properties](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms705994(v=vs.85)), and [management property identifiers](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-mqmr/22829c12-78a1-4896-8d99-0a1513965425).
2. **Interop boundary:** the installed `Experimental.System.Messaging` 1.1.0 public `MessageQueue` API exposes `GetAllMessages()` and `GetMessageEnumerator2()` but no public count method. Implement a small, Windows-only, well-tested native adapter rather than invoking package internals through reflection or launching PowerShell from the web application. Keep the existing library for normal message operations.
3. **Progressive fallback:** always publish queue names first. If management count fails because a queue is inactive, unavailable, or access is denied, show `—`/unknown and retry on selection or explicit refresh. Microsoft notes that management properties can fail for inactive queues, so one failure must not fail the server connection.
4. **Queue-type policy:** use exact management counts for ordinary application queues where validated. For system queues/subqueues without a fast direct count, cap manual enumeration at a small threshold and display “10+” or simply non-empty. Cogin documents a similar policy in [QueueExplorer 4](https://www.cogin.com/QueueExplorer/4.0/): it manually checks only the first 10 system/subqueue messages for performance and allows work to begin as initial messages arrive.
5. **Bounded scheduling:** load selected/visible queue counts first through a small per-server concurrency limit. Do not issue 200 unrestricted operations for 100 base queues plus journals, and do not wait for all count work before setting the connection to usable.
6. **Local proof of concept:** on the review workstation, the official read-only `Get-MsmqQueue | Select QueueName, MessageCount` path returned 21 queues containing 2,543 messages in 89.6 ms; the journal query returned 21 journal queues in 73.7 ms. This is not a production or remote benchmark, but it shows that the Windows management surface can report counts without the application's current full enumeration behavior. Microsoft documents this `MessageCount` usage in [Get-MsmqQueue](https://learn.microsoft.com/en-us/powershell/module/msmq/get-msmqqueue?view=windowsserver2025-ps).

The spike should compare native `MQMgmtGetInfo`, a bulk WMI/performance-counter query where supported, and the current enumeration implementation on a representative 100-queue server. The acceptance decision should consider correctness, remote permissions, inactive/empty queues, journals, system queues, latency, and failure isolation—not only the fastest local result.

### Measurement recommendations

Start with the confirmed production profile: approximately 100 queues, several thousand messages concentrated in journals, and stale test queues with substantial backlogs. Add smaller baselines plus a stress case above the current production maximum. Include bodies at 1 KB, 100 KB, and the configured maximum. Record time to first queue name, time to usable tree, time to all counts, first-message-page latency, bytes allocated, working set, Gen 2/LOH collections, handle count, circuit responsiveness, network transfer, timeout rate, and refresh overlap. Include inaccessible and disconnected remote hosts.

Define target service-level objectives before implementation. At minimum, distinguish **queue discovery latency** from **count completion latency** so an expensive count cannot block the initial queue tree. Capture the actual production distribution of queue count, queue depth, journal usage, body size, local versus remote access, and acceptable count staleness.

### Proposed performance sequence

1. **Render queue identity first:** enumerate queue names/paths/metadata and publish the tree without waiting for message or journal counts. Display counts as pending, unavailable, or stale instead of blocking the entire connection.
2. **Eliminate message enumeration from counting:** spike `MQMgmtGetInfo` for base and journal counts first. If exact management counts are unavailable for a queue type, use a capped narrow-property cursor only for that type and make the distinction visible.
3. **Bound count concurrency:** execute count jobs through a small per-server worker pool rather than sequentially or with unbounded `Task.WhenAll`. Prioritize the selected/visible queue, enforce operation deadlines, cancel superseded generations, and isolate a slow/inaccessible queue from the rest of the tree.
4. **Cache with freshness semantics:** retain counts with a visible “as of” time and configurable TTL. Refresh visible/selected queues first; do not automatically rescan every queue and journal on a short timer.
5. **Bound message retrieval:** fetch a header-only first page, apply the maximum while enumerating, dispose each native message, and load the selected body separately under a byte limit.
6. **Instrument the stages:** log/measure discovery, base-queue counts, journal counts, first-page load, timeouts, cancellations, and allocation/handle growth separately so optimization is evidence-driven.
7. **Remove secondary overhead:** eliminate the 25 ms browser theme poll and ad hoc global listeners after the MSMQ critical path is bounded.

## 7. Security and Operational Safety

- **Current prioritization:** security is not the current delivery focus and is tracked as P4 under the accepted deployment assumptions. This prioritization does not reduce the potential impact. Promote the work to P1 for evidence of active exploitation and at least P2 before exposing the service to untrusted users or networks.
- **Authentication/authorization:** currently absent. Windows Authentication must eventually be paired with application authorization policies; authentication alone will not constrain queue operations.
- **Identity:** MSMQ operations currently run as the service account. Document fixed-identity least privilege or implement and test impersonation; do not assume the web user's permissions automatically flow to MSMQ/WMI.
- **Network exposure:** `ListenAnyIP`, `AllowedHosts: "*"`, HTTP-only hosting, and automatic firewall opening create a broad attack surface. Default to loopback/private exposure and require an explicit secure remote configuration.
- **Destructive actions:** purge confirmation is strong, and individual delete is confirmed. Service-layer authorization and journal restrictions are still required because UI confirmation alone is bypassable and bulk delete currently contradicts journal policy.
- **Remote target input:** the connection dialog accepts an arbitrary host/IP and makes server-side MSMQ/WMI calls. Once authenticated, restrict or authorize target hosts to prevent the management service from becoming a network-probing proxy.
- **Untrusted content:** Razor encoding prevents direct HTML injection. Keep raw-content rendering encoded. Set explicit secure XML reader settings (DTD prohibited, resolver null, document/entity limits) rather than relying on runtime defaults, and apply JSON/XML depth/size limits before parsing.
- **Sensitive data:** current structured logs avoid bodies, which should remain policy. Remove `Console.WriteLine` topology dumps and decide whether queue paths and machine names require redaction.
- **Service account:** installers should support/configure a dedicated account, grant only required MSMQ/WMI/Event Log rights, quote executable paths robustly, and document remote delegation behavior.
- **Auditability:** the PRD lists audit logging out of scope, but once destructive remote access is supported, at least security audit events for user, target queue, action, outcome, and correlation ID are strongly advisable. Do not log body content.

## 8. Testing Assessment

### Current state

The test project and runner are configured correctly enough to discover xUnit tests, but the only test is empty. Current green test output provides no confidence. Direct construction of static `MessageQueue` objects also prevents fast isolated service tests.

### Prioritized test plan

1. **Safety contract tests:** normal browsing always peeks; `peekOnly:false` either truly receives or is removed; individual/bulk journal delete is rejected; purge requires the correct queue and confirmation.
2. **Queue-address matrix:** local/remote private, public, machine journal, queue journal, dead-letter, transactional dead-letter, outgoing, `FormatName:`, and `DIRECT=` variants; specifically prevent `;journal;journal`.
3. **Bounded retrieval:** a request for N messages never enumerates N+1 bodies; body-size caps are enforced before allocation; all returned message/stream objects are disposed.
4. **Timeout/cancellation:** offline remote host, missing message ID, cancellation during enumeration, and disposal during an in-flight refresh.
5. **Transactions and replay:** send to transactional/non-transactional queues, incorrect transaction selection, raw-byte preservation, header mapping, copy-then-delete partial failure, and duplicate/idempotency behavior.
6. **Bulk result semantics:** mixed success/not-found/access-denied returns a typed partial result and updates only succeeded rows.
7. **Blazor concurrency:** queue A response cannot overwrite queue B after selection changes; manual and auto-refresh do not overlap; timers stop on disposal; disconnected JS calls are handled.
8. **Malformed/untrusted bodies:** invalid UTF-8, binary, oversized XML/JSON, deep documents, DTD/entity input, empty streams, non-seekable/partial-read streams, and unknown formatter payloads.
9. **Date/time policy:** saved user preference wins; browser locale is used otherwise; local fallback works without JS; every component calls the central formatter.
10. **Theme contract:** company tokens render consistently in light/dark modes; text, controls, status states, selected rows, focus rings, and disabled/destructive controls meet the chosen WCAG contrast target.
11. **Security integration (when promoted):** anonymous requests rejected, read-only users cannot mutate, target-host restrictions work, and the effective MSMQ identity matches the documented model.

Use a small `IMsmqClient`/factory abstraction for deterministic unit tests. Keep destructive Windows integration tests opt-in, create uniquely named queues, verify target paths before mutation, and clean up in `finally`. Add bUnit for component policy and Playwright only for the few browser/JS behaviors that require it.

## 9. Requirements and Documentation Gaps

| Area | Requirement/documentation | Implementation status |
|---|---|---|
| Authentication | Windows Authentication exclusively; respect authenticated user's MSMQ rights | Not configured; operations use process identity. |
| Queue discovery | All local/remote private, public, system, and journal queues | Private plus selected system/outgoing only; no public enumeration. |
| Message list | All messages; up to 10,000 within 3 seconds | Silently limited to 1,000 after full materialization. |
| Move/resend/export | Full individual operations | Move unimplemented; replay lossy; export is server-side; UI buttons hidden. |
| Search/filter | Body search, highlight, priority/date/size combined filters | Label/ID/correlation substring search only. |
| Refresh | 5-second configurable 1-60, session pause persistence | Static process-wide 10-second default; 1-3,600 input; configuration unused. |
| Time display | User preference, then browser convention, then centralized local fallback | Scattered hard-coded/server-culture formatting. |
| Company UX theme | Standard company colors, typography, and component states | Light/dark support exists, but generic Bootstrap colors, generic fonts, and hard-coded values are not mapped to an approved company design-token system. |
| Error recovery | Graceful remote failures and auto-reconnect | Many UI errors swallowed; reconnect/health methods are stubs. |
| Port | PRD/most docs: 8080 | Application/generated installer: 9090; other scripts still use 8080. |
| Project status | CLAUDE says scaffolding/no tests; README says various items complete/incomplete | Material implementation exists, but several advertised features are partial. |
| Coding standards | Mandatory code-behind and size/enforcement rules | Large violations, one Razor `@code` block, formatter verification fails. |

Documentation should also distinguish Windows Authentication of the web application from the Windows identity used for backend MSMQ calls; those are not automatically the same in Blazor Server or a Windows Service.

## 10. Recommended Roadmap

### Delivery phase 1 — large-server performance (P2 findings; top product priority)

1. **HI-002:** separate queue discovery from counts so queue names render immediately; spike `MQMgmtGetInfo` for paired base/journal counts; fall back to explicit unknown/capped states rather than blocking discovery; dispose all messages in any fallback enumeration.
2. **HI-001:** move synchronous MSMQ/WMI work behind a bounded per-server executor with explicit deadlines and honest cancellation semantics.
3. **ME-002/ME-006:** serialize/coalesce refreshes, cancel stale generations, make settings circuit-local, and stop rescanning every queue and journal at a short global interval.
4. **ME-008:** implement header-first bounded paging and lazy selected-body retrieval; make truncation/pending/stale states explicit.
5. Add discovery/count stage metrics and benchmark large local/remote servers before and after each change. The first success criterion is time to a usable queue tree, not time to every exact count.

### Delivery phase 2 — correctness and requested UX

1. **HI-003:** block journal bulk delete in the component and service/application layers.
2. **HI-004:** centralize and test queue/journal address construction; fix the double suffix before relying on journal counts or purge.
3. **ME-010:** return typed partial results for destructive bulk work and update only successfully processed rows.
4. **UX-001:** obtain the company palette/typography specification, introduce semantic design tokens, map Bootstrap and component states to them, and verify light/dark accessibility.
5. **ME-011:** build tests for bounded enumeration, queue paths, refresh concurrency, destructive safety, and theme/date formatting.

### Delivery phase 3 — planned quality and product completion

1. **ME-003:** introduce the centralized overridable date/time display formatter and migrate all user-visible values.
2. **ME-004:** preserve raw bytes and use strict text decoding.
3. **ME-007:** replace swallowed exceptions/debug output with structured logs and actionable UI feedback.
4. **ME-001/ME-005/ME-009:** decide supported public-queue and move/resend/export scope, then implement or remove misleading contracts.
5. **LO-003:** complete keyboard, focus, and dialog accessibility while applying the company theme.
6. Split the low-level MSMQ adapter from address policy, discovery, bounded reads, and writes; decompose large page/services along those boundaries.

### Delivery phase 4 — future security hardening under current priorities

1. **CR-001/HI-005:** before broader or untrusted access, make hosting private-by-default, implement authentication/authorization, and define a least-privilege backend identity model.
2. Restrict remote target selection and installer firewall scope.
3. Add security audit events for destructive operations without logging message bodies.
4. Promote this phase immediately if exploitation, unauthorized access, regulatory scope, or a change in network trust is reported.

No full rewrite is recommended. Progressive discovery, bounded MSMQ access, count caching, design tokens, path policy, and tests can be introduced behind the existing interfaces and components in stages.

## 11. Prioritized Summary Table

| ID | Recommendation | P0-P5 tier | Impact | Effort | Confidence | Delivery order |
|---|---|---|---|---|---|---:|
| HI-002 | Publish queue names immediately; use MSMQ management counts instead of full scans; bound bodies/pages | **P2** | Directly addresses multi-minute initial load on ~100-queue servers | Large | High | 1 |
| HI-001 | Bound MSMQ/WMI calls with worker execution and deadlines | **P2** | Prevents circuit stalls on slow/local/remote operations | Large | High | 2 |
| ME-002 | Serialize/cancel refresh and reject stale responses | **P2** | Prevents overlapping load and wrong-queue state | Medium | High | 3 |
| ME-006 | Make refresh preferences scoped/configured and stop broad rapid rescans | **P2** | Reduces repeated server work and cross-user interference | Small | High | 4 |
| ME-008 | Add bounded header paging, lazy bodies, and required search/filter behavior | **P2** | Makes large queues responsive and trustworthy | Large | High | 5 |
| HI-004 | Centralize/fix queue and journal paths | **P2** | Restores correct journal counts and targets | Medium | High | 6 |
| HI-003 | Block journal bulk deletion at multiple layers | **P2** | Prevents unintended archive data loss | Small | High | 7 |
| ME-010 | Model and display partial bulk outcomes | **P2** | Makes destructive retries safe | Medium | High | 8 |
| UX-001 | Introduce company design tokens for color, typography, and component states | **P3** | Aligns the product with company UX standards | Medium/Large | Medium | 9 |
| ME-011 | Build meaningful unit/component/integration coverage | **P3** | Protects performance and correctness changes | Large | High | 10 |
| ME-004 | Preserve bytes and use strict format detection | **P2** | Corrects binary inspection and replay fidelity | Medium | High | 11 |
| ME-003 | Centralize all user-visible date/time formatting | **P3** | Meets locale/preference requirement | Medium | High | 12 |
| ME-007 | Surface errors and remove debug leakage | **P3** | Improves recovery and diagnostics | Medium | High | 13 |
| LO-003 | Add keyboard/focus semantics | **P3** | Improves accessibility | Medium | High | 14 |
| ME-001 | Add or explicitly de-scope public queues | **P3** | Completes or clarifies discovery scope | Medium | High | 15 |
| ME-005 | Remove false-success stubs or implement contracts | **P3** | Makes service APIs reliable | Medium | High | 16 |
| ME-009 | Define safe replay/move/export semantics and UX | **P3** | Completes operator workflows | Large | High | 17 |
| ME-012 | Remove sync wait and decompose oversized responsibilities | **P4** | Improves maintainability and testability | Large | High | 18 |
| LO-002 | Own/tear down JS resources and remove theme polling | **P4** | Reduces client leaks and incidental work | Medium | High | 19 |
| LO-004 | Enforce formatter/analyzers in CI | **P4** | Aligns builds with standards | Small | High | 20 |
| LO-001 | Remove template/demo/debug production artifacts | **P4** | Reduces confusion and surface area | Small | High | 21 |
| IN-001 | Reconcile status, port, and standards documentation | **P5** | Prevents documentation drift from misleading maintainers | Small | High | 22 |
| CR-001 | Secure network exposure and authorize all operations when exposure assumptions change | **P4** | Prevents unauthenticated queue compromise | Large | High | 23 |
| HI-005 | Define and enforce backend Windows identity/least privilege | **P4** | Prevents privilege expansion | Large | High | 24 |

## 12. Open Questions and Runtime Validation

### Answers incorporated on 2026-10-01

1. **Production size/profile:** some servers have approximately 100 queues. Several thousand messages may be contained mostly in journals, and stale test queues may hold numerous unprocessed messages. Exact journal-enabled percentages, body-size distribution, and remote-latency distribution remain to be measured.
2. **First-load objective:** populate the queue list immediately. Counts are secondary and must not block a usable tree. A numeric target for “immediately,” acceptable count staleness, and time to all exact counts still need production baselines, but the architecture should publish identity independently of those values.
3. **Current and candidate count mechanisms:** the current application does not use a fast count. It calls `GetAllMessages()` for every private queue during discovery and again for journals during connection. The best first candidate is native `MQMgmtGetInfo` using base and journal message-count properties; Windows also exposes `MessageCount` through the MSMQ PowerShell/management layer. This must be benchmarked and validated for remote, inactive, empty, inaccessible, system, and journal queues before selection. Capped/manual counts should be the fallback for queue types without a reliable fast count, not the default for ordinary application queues.
4. **Company theme inputs:** no design-system answer was supplied in this performance-focused pass. UX-001 remains valid but is deferred until the performance work is underway and the approved brand inputs are available.

### Remaining validation questions

5. What measurable targets should define “immediate” queue identity and a usable tree on the representative 100-queue server? Candidate measurements are first queue name, complete name-only tree, selected-queue first page, first selected-queue count, and all background counts.
6. How stale may a base or journal count be, and should automatic refresh prioritize only the selected/visible queues while other counts remain cached?
7. Does native `MQMgmtGetInfo` return base and journal counts reliably under the application's actual local/remote identities, firewall configuration, direct format names, and empty/inactive queue states? Compare one two-property call per application queue with a bulk WMI/performance-counter query and the current enumeration baseline.
8. Confirm the exact accepted path/format-name syntax in `Experimental.System.Messaging` and `MQMgmtGetInfo` for private queue journals, machine journals, dead-letter queues, transactional dead-letter queues, and remote direct format names.
9. Verify whether the package's parameterless `PeekById`/`ReceiveById` overloads wait indefinitely and which timeout overloads are reliable for local and remote queues.
10. Is Active Directory public-queue discovery required in the actual deployment domains, and what should happen on workgroup machines?
11. Test transactional send, resend, and any future move across transactional/non-transactional and local/remote queue combinations. Determine whether atomic move is achievable for the supported cases.
12. Verify body fidelity for `ActiveXMessageFormatter`, raw `BodyStream`, non-UTF encodings, formatter headers, malformed messages, and non-seekable/partial streams.
13. Validate WMI outgoing queue discovery on supported Windows client/server versions, remotely, with WMI firewall/permissions restrictions, and confirm it does not misclassify normal direct-format queues.
14. Exercise a disappearing-message race during peek/delete/export and a queue deletion/recreation during refresh.
15. Decide whether journal purge is intentionally supported. If yes, document it distinctly from per-message journal deletion and require the stronger confirmation/authorization path already present.
16. When security hardening is promoted, which identity should own MSMQ access: a fixed service account or authenticated browser user? If per-user, are Kerberos delegation/double-hop and Blazor circuit impersonation explicit requirements?
17. When security hardening is promoted, should remote targets come from an administrator allowlist, and what firewall profiles, TLS/reverse-proxy arrangement, service account, and audit events are required?
