\# Product Requirements Document: MSMQ Monitor \& Management Tool



\## 1. Executive Summary



\### 1.1 Product Overview

A self-contained Blazor Server application that runs as a Windows service, providing real-time monitoring and management capabilities for Microsoft Message Queuing (MSMQ) on local and remote computers.



\### 1.2 Target Audience

\- Programmers

\- System Operators

\- Power End Users



\### 1.3 Success Criteria

\- Ability to monitor all MSMQ queues (application, system, and journal queues) on local and remote machines

\- Comprehensive message inspection with intelligent deserialization

\- Full queue management operations (view, purge, move, delete, resend, export)

\- Real-time updates with user-controlled refresh capabilities



---



\## 2. Product Goals \& Objectives



\### 2.1 Primary Goals

\- Provide a centralized web-based interface for MSMQ queue monitoring and management

\- Eliminate the need for multiple tools or direct server access for queue operations

\- Enable quick troubleshooting and message inspection capabilities

\- Support both local and remote MSMQ instances



\### 2.2 Non-Goals (Out of Scope for Initial Release)

\- Multi-queue technology support (RabbitMQ, Azure Service Bus, etc.)

\- User permission/role-based access control

\- Audit logging of user actions

\- Queue creation or configuration management



---



\## 3. Technical Architecture



\### 3.1 Technology Stack

\- \*\*Framework\*\*: Blazor Server (.NET 6.0 or later)

\- \*\*Deployment Model\*\*: Self-contained Windows Service

\- \*\*Supported OS\*\*: Windows Server 2019+, Windows 10+

\- \*\*Queue Technology\*\*: MSMQ (Microsoft Message Queuing)

\- \*\*Authentication\*\*: Windows Authentication



\### 3.2 Deployment Architecture

\- Application runs as a Windows service with auto-start capability

\- Hosts internal web server accessible via browser

\- Default web port: 8080 (configurable via configuration file)

\- Self-contained deployment (no external runtime dependencies)



\### 3.3 System Requirements

\- Windows Server 2019+ or Windows 10+

\- MSMQ feature installed and enabled

\- Network connectivity for remote queue access

\- Administrator privileges for service installation



---



\## 4. Functional Requirements



\### 4.1 Queue Discovery \& Display



\#### 4.1.1 Local Queue Discovery

\- \*\*FR-001\*\*: System shall automatically discover all MSMQ queues on the local machine

\- \*\*FR-002\*\*: System shall display queues organized by type:

&nbsp; - Application Queues (private and public)

&nbsp; - System Queues (Dead Letter, Transaction Dead Letter, etc.)

&nbsp; - Journal Queues

\- \*\*FR-003\*\*: System shall display queue metadata:

&nbsp; - Queue name

&nbsp; - Queue path

&nbsp; - Message count

&nbsp; - Queue type (private/public)



\#### 4.1.2 Remote Queue Access

\- \*\*FR-004\*\*: System shall allow users to connect to MSMQ on remote computers

\- \*\*FR-005\*\*: System shall use Windows Authentication for remote connections

\- \*\*FR-006\*\*: System shall display connection status for remote machines

\- \*\*FR-007\*\*: System shall handle connection failures gracefully with appropriate error messages



\### 4.2 Message Viewing \& Inspection



\#### 4.2.1 Message List Display

\- \*\*FR-008\*\*: System shall display all messages in a selected queue in a list/grid format

\- \*\*FR-009\*\*: System shall display the following message properties in the list view:

&nbsp; - Message label

&nbsp; - Sent time

&nbsp; - Priority

&nbsp; - Message ID

&nbsp; - Message size

&nbsp; - Correlation ID (if present)



\#### 4.2.2 Message Detail View

\- \*\*FR-010\*\*: System shall provide a detailed view panel for selected messages

\- \*\*FR-011\*\*: System shall display comprehensive message metadata:

&nbsp; - Label

&nbsp; - Sent time

&nbsp; - Arrived time

&nbsp; - Priority

&nbsp; - Correlation ID

&nbsp; - Response queue

&nbsp; - Message ID

&nbsp; - Source machine

&nbsp; - Body type

&nbsp; - Authenticated sender

&nbsp; - Any additional MSMQ properties



\#### 4.2.3 Message Body Deserialization

\- \*\*FR-012\*\*: System shall intelligently deserialize message bodies based on content type

\- \*\*FR-013\*\*: System shall support the following message body formats:

&nbsp; - XML (with syntax highlighting)

&nbsp; - JSON (with syntax highlighting and formatting)

&nbsp; - Plain text

&nbsp; - Binary (hexadecimal display)

\- \*\*FR-014\*\*: System shall auto-detect message format when possible

\- \*\*FR-015\*\*: System shall allow users to manually specify deserialization format if auto-detection fails



\### 4.3 Message Operations



\#### 4.3.1 Individual Message Operations

\- \*\*FR-016\*\*: System shall allow users to delete individual messages from queues

\- \*\*FR-017\*\*: System shall allow users to move messages between queues

\- \*\*FR-018\*\*: System shall allow users to resend messages to their original destination queue

\- \*\*FR-019\*\*: System shall allow users to export individual messages to file (XML/JSON format)

\- \*\*FR-020\*\*: System shall require confirmation before destructive operations (delete, purge)



\#### 4.3.2 Bulk Queue Operations

\- \*\*FR-021\*\*: System shall allow users to purge (delete all messages) from a queue

\- \*\*FR-022\*\*: System shall display confirmation dialog showing message count before purge

\- \*\*FR-023\*\*: System shall allow users to export all messages from a queue to files



\### 4.4 Search \& Filtering



\#### 4.4.1 Message Search

\- \*\*FR-024\*\*: System shall provide search capability across messages in a queue

\- \*\*FR-025\*\*: System shall support searching by:

&nbsp; - Message label

&nbsp; - Message ID

&nbsp; - Correlation ID

&nbsp; - Message body content (text search)

\- \*\*FR-026\*\*: System shall support case-insensitive search

\- \*\*FR-027\*\*: System shall highlight search results in the message list



\#### 4.4.2 Message Filtering

\- \*\*FR-028\*\*: System shall allow filtering messages by:

&nbsp; - Priority level

&nbsp; - Date/time range

&nbsp; - Message size

\- \*\*FR-029\*\*: System shall allow multiple filters to be applied simultaneously

\- \*\*FR-030\*\*: System shall display active filter indicators and allow quick filter removal



\### 4.5 Real-Time Updates \& Refresh



\#### 4.5.1 Auto-Refresh

\- \*\*FR-031\*\*: System shall automatically refresh queue and message data at configurable intervals

\- \*\*FR-032\*\*: System shall default to 5-second refresh interval

\- \*\*FR-033\*\*: System shall display a countdown timer showing seconds until next refresh

\- \*\*FR-034\*\*: System shall allow users to configure refresh interval (1-60 seconds)



\#### 4.5.2 Manual Refresh Controls

\- \*\*FR-035\*\*: System shall provide a "Refresh" button for immediate data update

\- \*\*FR-036\*\*: System shall provide a "Pause" toggle to suspend auto-refresh

\- \*\*FR-037\*\*: System shall preserve pause state during user session

\- \*\*FR-038\*\*: System shall visually indicate when auto-refresh is paused



---



\## 5. User Interface Requirements



\### 5.1 Layout Structure



\#### 5.1.1 Main Layout

\- \*\*UI-001\*\*: Application shall use a three-panel layout:

&nbsp; - Left panel: Queue tree view (resizable, collapsible)

&nbsp; - Middle panel: Message list/grid (primary content area)

&nbsp; - Right panel: Message detail drawer (slides in from right, collapsible)



\#### 5.1.2 Queue Tree View (Left Panel)

\- \*\*UI-002\*\*: Tree view shall organize queues hierarchically:

&nbsp; - Computer Name (local or remote)

&nbsp;   - Application Queues

&nbsp;     - Private Queues

&nbsp;     - Public Queues

&nbsp;   - System Queues

&nbsp;     - Dead Letter Queue

&nbsp;     - Transaction Dead Letter Queue

&nbsp;     - Other system queues

&nbsp;   - Journal Queues

\- \*\*UI-003\*\*: Each queue node shall display message count badge

\- \*\*UI-004\*\*: Tree view shall support expand/collapse of nodes

\- \*\*UI-005\*\*: Selected queue shall be visually highlighted

\- \*\*UI-006\*\*: Tree view shall include "Add Remote Computer" button at top



\#### 5.1.3 Message List (Middle Panel)

\- \*\*UI-007\*\*: Message list shall use a data grid/table format

\- \*\*UI-008\*\*: Grid shall support:

&nbsp; - Column sorting

&nbsp; - Column resizing

&nbsp; - Row selection

&nbsp; - Virtualization for large message counts

\- \*\*UI-009\*\*: Grid columns shall include:

&nbsp; - Label

&nbsp; - Priority

&nbsp; - Sent Time

&nbsp; - Message ID (truncated with tooltip)

&nbsp; - Size

\- \*\*UI-010\*\*: Grid toolbar shall include:

&nbsp; - Refresh button

&nbsp; - Pause/Resume toggle

&nbsp; - Refresh countdown timer

&nbsp; - Search box

&nbsp; - Filter button

&nbsp; - Bulk operations dropdown (Purge, Export All)



\#### 5.1.4 Message Detail Drawer (Right Panel)

\- \*\*UI-011\*\*: Drawer shall slide in from the right when a message is selected

\- \*\*UI-012\*\*: Drawer shall be collapsible via close button or clicking outside

\- \*\*UI-013\*\*: Drawer shall display:

&nbsp; - Message properties (top section)

&nbsp; - Message body viewer (bottom section, larger area)

&nbsp; - Operation buttons (Move, Delete, Resend, Export)

\- \*\*UI-014\*\*: Message body viewer shall include:

&nbsp; - Format selector dropdown (Auto, XML, JSON, Text, Binary)

&nbsp; - Syntax highlighting for XML/JSON

&nbsp; - Copy to clipboard button

&nbsp; - Full-screen view option



\### 5.2 Navigation \& Interaction



\#### 5.2.1 Remote Computer Management

\- \*\*UI-015\*\*: "Add Remote Computer" shall open a dialog prompting for:

&nbsp; - Computer name or IP address

&nbsp; - Connection button

\- \*\*UI-016\*\*: System shall validate remote connection and display status

\- \*\*UI-017\*\*: Remote computers shall persist in tree view until removed

\- \*\*UI-018\*\*: Users shall be able to remove remote computers from tree view



\#### 5.2.2 Confirmation Dialogs

\- \*\*UI-019\*\*: Destructive operations shall show confirmation dialogs:

&nbsp; - Delete message: "Delete this message? This action cannot be undone."

&nbsp; - Purge queue: "Delete all X messages from \[queue name]? This action cannot be undone."

\- \*\*UI-020\*\*: Move message dialog shall include:

&nbsp; - Target queue selector (dropdown or tree picker)

&nbsp; - Move button

&nbsp; - Cancel button



\### 5.3 Responsive Behavior

\- \*\*UI-021\*\*: Application shall be optimized for desktop displays (1920x1080 and higher)

\- \*\*UI-022\*\*: Panels shall be resizable via drag handles

\- \*\*UI-023\*\*: Application shall maintain panel size preferences during session

\- \*\*UI-024\*\*: Application shall gracefully handle smaller desktop resolutions (1366x768 minimum)



---



\## 6. Non-Functional Requirements



\### 6.1 Performance

\- \*\*NFR-001\*\*: Queue list shall load within 2 seconds for local machine

\- \*\*NFR-002\*\*: Message list shall load within 3 seconds for queues with up to 10,000 messages

\- \*\*NFR-003\*\*: Message detail view shall render within 500ms

\- \*\*NFR-004\*\*: Auto-refresh operations shall not block user interactions



\### 6.2 Reliability

\- \*\*NFR-005\*\*: Windows service shall automatically restart on failure

\- \*\*NFR-006\*\*: Application shall handle MSMQ permission errors gracefully

\- \*\*NFR-007\*\*: Application shall maintain connection to remote computers and auto-reconnect on network interruption

\- \*\*NFR-008\*\*: Application shall not crash when accessing corrupted or malformed messages



\### 6.3 Security

\- \*\*NFR-009\*\*: Application shall use Windows Authentication exclusively

\- \*\*NFR-010\*\*: Application shall respect MSMQ permissions of the authenticated user

\- \*\*NFR-011\*\*: Remote connections shall use secure Windows protocols

\- \*\*NFR-012\*\*: Message body content shall only be accessible to authenticated users



\### 6.4 Usability

\- \*\*NFR-013\*\*: Common operations shall be accessible within 2 clicks

\- \*\*NFR-014\*\*: Error messages shall be clear and actionable

\- \*\*NFR-015\*\*: Application shall provide loading indicators for long-running operations

\- \*\*NFR-016\*\*: Application shall use consistent terminology aligned with MSMQ documentation



\### 6.5 Maintainability

\- \*\*NFR-017\*\*: Application shall log errors to Windows Event Log

\- \*\*NFR-018\*\*: Configuration shall be externalized in appsettings.json

\- \*\*NFR-019\*\*: Code shall follow SOLID principles and clean architecture patterns



---



\## 7. Configuration Requirements



\### 7.1 Service Configuration

\- \*\*CFG-001\*\*: Port number (default: 8080)

\- \*\*CFG-002\*\*: Auto-start service on Windows boot (default: true)

\- \*\*CFG-003\*\*: Service display name and description

\- \*\*CFG-004\*\*: Logging level (Information, Warning, Error)



\### 7.2 Application Configuration

\- \*\*CFG-005\*\*: Default refresh interval (default: 5 seconds)

\- \*\*CFG-006\*\*: Maximum message body size to display (default: 1MB)

\- \*\*CFG-007\*\*: Message list page size for virtualization (default: 100)

\- \*\*CFG-008\*\*: Remote connection timeout (default: 30 seconds)



\### 7.3 Configuration File Format

```json

{

&nbsp; "Service": {

&nbsp;   "Port": 8080,

&nbsp;   "ServiceName": "MSMQMonitor",

&nbsp;   "DisplayName": "MSMQ Monitor \& Management Tool"

&nbsp; },

&nbsp; "Application": {

&nbsp;   "DefaultRefreshIntervalSeconds": 5,

&nbsp;   "MaxMessageBodySizeBytes": 1048576,

&nbsp;   "MessageListPageSize": 100,

&nbsp;   "RemoteConnectionTimeoutSeconds": 30

&nbsp; },

&nbsp; "Logging": {

&nbsp;   "LogLevel": {

&nbsp;     "Default": "Information"

&nbsp;   }

&nbsp; }

}

```



---



\## 8. Dependencies \& Integrations



\### 8.1 External Dependencies

\- \*\*.NET Runtime\*\*: 6.0 or later (self-contained deployment)

\- \*\*MSMQ\*\*: Must be installed and enabled on target machine

\- \*\*Windows Authentication\*\*: Requires Active Directory or local Windows accounts



\### 8.2 NuGet Packages (Estimated)

\- Microsoft.AspNetCore.Components.WebAssembly.Server

\- Microsoft.Extensions.Hosting.WindowsServices

\- System.Messaging (for MSMQ API access)

\- Blazor UI component library (e.g., MudBlazor, Radzen, or custom components)



---



\## 9. Installation \& Deployment



\### 9.1 Installation Process

1\. Copy self-contained application files to target directory (e.g., `C:\\Program Files\\MSMQMonitor`)

2\. Run installation script/command to register Windows service

3\. Configure appsettings.json as needed

4\. Start the service

5\. Access web interface via `http://localhost:8080` (or configured port)



\### 9.2 Uninstallation Process

1\. Stop the Windows service

2\. Unregister the service

3\. Delete application files



\### 9.3 Update Process

1\. Stop the service

2\. Backup configuration file

3\. Replace application files

4\. Restore configuration file

5\. Start the service



---



\## 10. Future Enhancements (Post-MVP)



\### 10.1 Potential Future Features

\- Role-based access control (RBAC)

\- Audit logging of all user actions

\- Message import capability

\- Scheduled message operations

\- Dashboard with queue health metrics

\- Alerting/notifications for queue thresholds

\- Support for additional queue technologies (RabbitMQ, Azure Service Bus)

\- Queue creation and configuration management

\- Export to additional formats (CSV, Excel)

\- Message replay/reprocessing workflows

\- Dark mode theme



---



\## 11. Acceptance Criteria



\### 11.1 MVP Acceptance Criteria

\- ✅ Application installs as Windows service and auto-starts

\- ✅ Web interface accessible on configured port

\- ✅ All local MSMQ queues are discoverable and displayable

\- ✅ Remote computers can be added and queues accessed

\- ✅ All message properties are viewable

\- ✅ Message bodies correctly deserialize in XML, JSON, text, and binary formats

\- ✅ All CRUD operations work (view, delete, move, resend, export, purge)

\- ✅ Search and filtering functions correctly

\- ✅ Auto-refresh works with configurable interval and pause capability

\- ✅ UI matches the three-panel layout specification

\- ✅ Application handles errors gracefully without crashing

\- ✅ Performance meets NFR requirements for typical queue sizes (<10,000 messages)



---



\## 12. Open Questions \& Risks



\### 12.1 Open Questions

\- Should the application support HTTPS/SSL for the web interface? Initially it will not but can be eanabled;

\- Should there be a maximum limit on the number of remote computers that can be connected simultaneously?  Only limit by memory;

\- What should happen to messages when moving between different message body types? Not valid

\- Should exported messages include all metadata or just body content? Yes should be a standard json format with the message being mime-encoded



\### 12.2 Known Risks

\- \*\*Risk\*\*: MSMQ API limitations may impact real-time update capabilities

&nbsp; - \*\*Mitigation\*\*: Use polling mechanism with configurable intervals

\- \*\*Risk\*\*: Large message bodies (>10MB) may cause UI performance issues

&nbsp; - \*\*Mitigation\*\*: Implement lazy loading and size limits with warnings

\- \*\*Risk\*\*: Remote MSMQ access may be blocked by firewalls

&nbsp; - \*\*Mitigation\*\*: Document required firewall rules and ports in user guide

\- \*\*Risk\*\*: Windows service may require elevated privileges

&nbsp; - \*\*Mitigation\*\*: Document installation requirements and privilege needs



---



\## 13. Success Metrics



\### 13.1 Technical Metrics

\- Service uptime: >99.5%

\- Average page load time: <2 seconds

\- Zero critical security vulnerabilities

\- Successful remote connection rate: >95%



\### 13.2 User Satisfaction Metrics

\- Reduced time to identify queue issues (target: 50% reduction vs. manual methods)

\- User adoption rate among target audience

\- Support ticket reduction for MSMQ-related issues



---



\## Document Control



\- \*\*Version\*\*: 1.0

\- \*\*Date\*\*: October 18, 2025

\- \*\*Status\*\*: Draft for Review

\- \*\*Author\*\*: Product Team

\- \*\*Stakeholders\*\*: Development Team, Operations Team, End Users

