# Release gate — K4RMA Signal v3

## Verified in this workspace

- `npm test` — 25 Node tests passed (retrieval, Korean detection, source version precedence, provider payloads, strict schema, security limits, translation coverage, transcript overlap).
- `node --check` — all major Node modules and browser JavaScript parse.
- Local HTTP smoke — missing/wrong PIN rejected with 401; a valid PIN obtains source excerpts; config endpoint returns 200.
- Local source-import test — 183 C# chunks and 28 slides successfully reindexed from the archived snapshot (not the user's latest Git checkout).

## Not verified here — mandatory before class / production

- Real Groq Whisper or paid API key requests, token costs, provider quota and structured-output round trips.
- iPhone Safari / Android Chrome microphone recording while foregrounded and screen-awake.
- Browser layout / device viewport screenshots; headless navigation was blocked in this execution environment.
- Spoken Korean technical questions at realistic room distances and with overlapping team speech.
- Actual Redis-backed Vercel rate limits, real deployment domain, custom headers, and cost alerts.
- Complete latest `feat/rising-slash` checkout and live Unity Play Mode.

## Release acceptance plan

1. Run `node tools/sync-source.mjs <actual-Unity-repo>` on the newest reviewed branch, then `npm test` and restart the Node server.
2. Set `APP_PIN` to a long random secret; set Groq transcription key and selected LLM provider key on the server.
3. Local: verify wrong PIN 401, unrelated question returns unsupported, UP04 returns citations to genuine code.
4. On HTTPS phone: start Q&A mode, confirm permission, test 20 spoken Korean questions (including compound/mixed English words); note misses and delays.
5. Test 15 minutes of audio and verify provider dashboard quotas; then test one network drop, a denied microphone, a provider 429, and malformed response.
6. Vercel: create Upstash Redis and set credentials; test bursts exceeding configured per-route limits; confirm blocked requests don't invoke provider API.
7. Manually review responsive layout at 320/390/768/1440px and screen reader keyboard navigation.
8. Obtain appropriate classroom consent before recording or live use.

## Known limits

- `Presenting/Q&A` is phase filtering, **not** professor speaker recognition. Enrollment, diarization, and voice biometric filtering are not implemented.
- Source IDs are validated, but semantic truth of each model claim is not independently proven by a second verifier.
- Audio overlap adds quota usage; asynchronous transcription can lag under poor networks.
- Production Vercel deliberately refuses to process API calls without shared rate limiting. Security settings are not a substitute for a real external penetration test.
