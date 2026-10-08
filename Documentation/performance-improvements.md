# HI-001 / HI-002: first performance implementation

## Delivered

- Private queue discovery returns identities without reading queue properties, bodies, or counts. Connecting no longer performs a duplicate connection test first. Queue properties load when selected.
- Connection and refresh publish the private queue list before background system/outgoing discovery and count enrichment. Paired base/journal counts use `MQMgmtGetInfo`, with two requests at a time. When an inactive queue has no management count, a single zero-timeout metadata-only peek confirms emptiness and opens the queue for a management retry. Empty queues and journals report zero. There is no full-message scan fallback; genuine access/transport failures are not converted to zero. Probes do not follow outgoing queue addresses to a different destination computer.
- All `IMsmqService` operations run behind a shared executor with two workers per normalized server name and eight globally. `Application:RemoteConnectionTimeoutSeconds` bounds admission and caller waiting; connection tests can override it. MSMQ ID operations use zero-timeout overloads so a disappeared ID does not wait for a future message.
- Timed-out or canceled native calls retain their worker slots until they actually return. Cancellation cannot abort native RPC or undo a mutation. A timeout on a mutation reports an unknown outcome; refresh before retrying. Cooperative enumeration throws cancellation instead of returning partial success.
- Queue and journal lists use disposable cursors and request only row metadata. The UI uses `Application:MessageListPageSize` (100 by default) and explicitly labels the loaded view as limited. Search and sorting apply to loaded rows only. Selected bodies load separately; body-size sorting is no longer offered on metadata rows.
- Selected messages load their complete bodies. The old 1 MiB retrieval cap and 512 KiB detail-view truncation have been removed, along with the `MaxMessageBodySizeBytes` application setting. Text, XML, JSON, and hex views format the complete body; Copy uses the complete formatted content. Queue lists remain metadata-only.
- Messages, discovery queue objects, cursors, and WMI result objects are disposed. Purging no longer scans the queue to report a speculative deletion count.

The synchronous implementation is `NativeMsmqService`; application callers should inject `IMsmqService` to receive the bounded asynchronous wrapper.

## Verification

Regression fixed after the first run: private queue discovery can return an already-prefixed
`FormatName:DIRECT=OS:...` path. Adding another `DIRECT=OS:` prefix produced an invalid address,
which left counts unknown and message lists empty. Discovery now preserves existing format
names. The native integration test follows the discovered address through counts, base-message
reads, and journal reads. A read-only check of the existing `hotmill_pdi` queue returned a count
of four and four metadata rows. The full suite passed 20 tests with native checks enabled.
Message-read failures are now shown in the UI instead of appearing only as an empty list.
Empty queues and journals show explicit zero badges. The earlier label-only change did not
address inactive empty queues; the read-only probe now resolves those counts. Verification
checked all 21 existing local private queues and their journals: 13 empty base queues reported
zero, and `hotmill_pdi` retained its count of four. A regression test creates a fresh, inactive
empty queue and checks both zero counts before any message traffic; a separate test ensures
that a nonexistent queue is not reported as empty.

To run the optional read-only check against an existing local queue, set `MSMQ_READONLY_QUEUE`
to its queue name. Optionally set `MSMQ_READONLY_EXPECTED_COUNT` to assert a known stable count.
This check neither receives nor sends messages and does not modify the queue.

Run the unit tests:

```powershell
dotnet test MsMqApp.Tests/MsMqApp.Tests.csproj
```

To enable the native integration test on Windows with MSMQ running:

```powershell
$env:MSMQ_INTEGRATION_TESTS = '1'
dotnet test MsMqApp.Tests/MsMqApp.Tests.csproj --logger 'console;verbosity=detailed'
```

The integration test creates a unique `codex-perf-<guid>` private queue, seeds 2,000 messages, receives five into its journal, validates counts and bounded retrieval, then deletes only that queue. It verifies lazy full-body retrieval, discovery without counts, and missing-ID reads/deletes. A separate disposable-queue test retrieves all 4,192,266 bytes of a nearly 4 MiB body by both ID and lookup ID, verifying that the message remains in the queue. Viewer tests render nearly 4 MiB of text through the actual Razor component and validate complete large XML, JSON, binary, and whitespace bodies. Unit tests also cover worker capacity after timeout, queued cancellation, early connection publication, unknown-count cloning, server grouping, and late results after disconnect.

Observed on the local Windows x64 host on 2026-10-08, with 1,995 base messages and five journal messages (1 KiB bodies):

| Operation | Single-run elapsed time |
| --- | ---: |
| Paired management counts | 3.80 ms |
| Ten metadata rows | 4.72 ms |
| Previous `GetAllMessages()` read with all properties | 46.66 ms |

These are a smoke-test timing sample, not a controlled benchmark or a prediction for remote hosts. The startup smoke test returned HTTP 200 and discovered 21 existing local private queues. Interactive browser verification was unavailable because the browser automation process failed to start.

## Follow-up validation and remaining work

- Profile the approximately 100-queue remote server, including time to first queue list, time to counts, message-list latency, and repeated-refresh allocations/handles.
- Exercise offline/RPC-stalled and access-denied remote servers, and verify x86 interop if that architecture is supported. Local x64 native behavior and simulated blocked-worker behavior have been tested.
- Add continuation-based page navigation beyond the configured first-page limit. This delivery bounds the existing list rather than implementing whole-queue browsing/search.
- Process isolation would be needed to forcibly terminate a permanently hung native call.

## Native API references

- [Message count management property](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms703184(v=vs.85)) describes `VT_UI4` and the inactive-queue limitation.
- [MQMGMTPROPS](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms703985(v=vs.85)) documents the management property arrays, including the unused per-property status array.
- [PROPVARIANT layout](https://learn.microsoft.com/en-us/windows/win32/api/propidlbase/ns-propidlbase-propvariant) defines the architecture-dependent union used by the native count reader.
