# K4RMA Signal — Source-grounded presentation copilot

A responsive Korean/English presentation assistant that transcribes Korean speech (Groq Whisper), detects likely questions, retrieves C# source evidence, and generates short technical answers through a configurable answer provider. **Use real-time assistance and capture classroom audio only when permitted.**

## Status (this release)

- Local Node server, Vercel functions, model-provider adapters, evidence retrieval, and automated regression tests are implemented.
- Speech capture uses overlapping approximately 11-second MediaRecorder windows, starting every 9 seconds. This is **chunked near-live transcription**, not true sub-second streaming. The phone must remain awake and in the foreground.
- `Presenting` suppresses auto answers and `Q&A` detects likely questions. **The microphone cannot identify the professor or the four presenters.** No speaker biometrics/diarization are claimed.
- Models and provider APIs have **not** been tested against a real user API account. Classroom recognition, precise latency, mobile devices, paid API billing, and production Vercel deployment require user-side verification.
- The bundled corpus contains the integrated `feat/2d-prototype` snapshot, supplied later diffs, and Jeongmin's 28-slide submitted presentation. It is **not** a complete verified checkout of your latest `feat/rising-slash` branch until you run the sync command below.

## Local start — Windows PowerShell

Install Node.js 20 or later and run from this directory:

```powershell
$env:APP_PIN="a-long-random-private-password"
$env:GROQ_API_KEY="gsk_your_private_key"
$env:ANSWER_PROVIDER="groq"
npm test
npm start
```

Open `http://localhost:3000`. Enter the access secret under Preferences. Without `GROQ_API_KEY` transcription is disabled. Without *any* configured answer API key, manual questions return relevant source excerpts without fabricating a generated answer. If you omit `APP_PIN`, the development server binds only to localhost; **do not** expose that mode publicly.

## Optional model selection

Groq speech transcription always uses `GROQ_API_KEY`. Answer generation can use any configured provider:

| Provider | Environment | Recommended setting |
|---|---|---|
| Groq (free within quota) | `GROQ_API_KEY` | `ANSWER_PROVIDER=groq`, `GROQ_CHAT_MODEL=openai/gpt-oss-120b` |
| OpenAI (paid API) | `OPENAI_API_KEY` | `ANSWER_PROVIDER=openai`, `OPENAI_MODEL=gpt-4.1-mini` |
| Anthropic (paid API) | `ANTHROPIC_API_KEY` | `ANSWER_PROVIDER=anthropic`, `ANTHROPIC_MODEL=claude-haiku-4-5-20251001` |
| OpenRouter (free models only by default) | `OPENROUTER_API_KEY` | `ANSWER_PROVIDER=openrouter`, `OPENROUTER_MODEL=openrouter/free` |

All credentials are server-only. ChatGPT subscription fees do not cover OpenAI API usage. Paid models are **optional**, never bundled or automatically purchased. `ALLOW_PAID_FALLBACK=true` permits cross-provider fallback to *configured* paid providers when the chosen provider fails; the default is `false`. OpenRouter paid IDs are blocked unless `ALLOW_PAID_OPENROUTER=true` is explicitly set. API providers have different usage and retention policies.

Model answers use structured JSON on supported APIs, bounded deadlines, no unbounded retry loop, constrained evidence excerpts, exact source-ID validation, and refusal when citations are absent. Structured JSON does **not** prove factual correctness. Source evidence is part of the prompt, so do not treat user-contributed repository content as trusted instructions.

## Synchronize your actual latest Unity Git branch (high priority)

Run this on the computer that has your complete Unity project:

```powershell
node tools/sync-source.mjs "D:\Workplace\Your-Unity-Repo"
npm test
```

The script reads `Assets/K4RMA/**/*.cs` from that **local** checkout and rewrites `knowledge.json`, replacing the older archived source and partial patch excerpts while retaining all 28 slide notes. It records the branch, commit SHA and dirty/clean status; it never changes the Unity repository. Use the actual `feat/rising-slash` checkout. Commit the regenerated `knowledge.json` to the web app repo, which should remain **private** if team source is not approved for public distribution. Restart or redeploy the app to load the regenerated index.

The importer indexes only C# source under `Assets/K4RMA`; if you need to include additional documentation or other code folders, extend and test the importer deliberately. It cannot prove Unity Play Mode behavior.

## Deploy to Vercel — secure configuration required

1. Put this project in a private repository; configure Vercel for a static `public/` directory plus the included `api/` Node functions. Set HTTPS domain.
2. Set `APP_PIN` to a long **random** secret of at least eight characters (prefer 16+). A four-digit PIN is rejected in production.
3. Set the chosen server-side provider API keys and `ANSWER_PROVIDER`.
4. Set **both** `UPSTASH_REDIS_REST_URL` and `UPSTASH_REDIS_REST_TOKEN` from an Upstash Redis database (a free tier may be available). Rate limits in the serverless environment use Redis. Public Vercel APIs **fail closed** if this distributed rate limiter is not configured; otherwise any number of serverless instances could bypass a per-instance limiter.
5. Deploy; check `/api/config`, PIN validation, rate limits, provider API calls, and microphone capture with permission on HTTPS.

This is an access-secret gate and per-IP quota defense, **not full user authentication**. A public production service with multiple users needs a proper identity/session system, stronger abuse controls, monitoring, privacy review, and infrastructure-specific security validation. Never commit real API keys. Restrict the hosting project to approved team members; using short publicly shared passwords defeats the protection. Configure billing limits with paid providers and monitor usage. Do not promise uninterrupted free quotas.

## Behavioral limitations and safety

- Speaker identification: **not implemented**. Q&A-mode question classification is based on text and may mistake teammates, classmates, or narration for a professor question.
- Audio quality: a phone near presenters may not capture a distant professor. overlapping chunks improve boundaries but do not guarantee correct transcription; short questions may be missed. When overloaded, the app warns that audio was skipped.
- Answer accuracy: source-ID validation checks that cited documents exist, not that every assertion is true. Reviewer validation is required for technically consequential answers.
- Source age: older integrated code and later excerpts may contradict each other; sync the actual branch before depending on the result.
- Network: live speech and generated answers require connectivity; service worker caches only UI assets and never audio/API responses.
- Privacy: microphone clips are sent to Groq; questions and selected source excerpts are transmitted to the configured answer provider. The app does not persist raw recordings, but providers may process or retain data under their separate policies.

## Engineering reference

- `lib/retrieval.mjs` — indexed source matching, relevance gate, evidence budget.
- `lib/providers.mjs` — Groq/OpenAI/Anthropic/OpenRouter adapters and strict output contracts.
- `lib/security.mjs` — constant-time PIN checks and rate limiting; Redis in serverless deployments.
- `lib/core.mjs` — question handling, evidence-aware answer composition and Whisper transcription.
- `lib/vercel-handler.mjs`, `api/`, `server.mjs` — consistent API protections for both runtimes.
- `public/app.js`, `public/audio-utils.js`, `public/styles.css` — bilingual responsive UI, overlapping audio windows, transcript deduplication.
- `tools/sync-source.mjs` — refresh knowledge index from the local authoritative Unity source.
- `tests/` — unit, retrieval, model-adapter, security and translation checks.

Run `npm test` before committing. Additionally run browser smoke tests, a real HTTPS phone-mic test, and a 15-minute classroom-like rehearsal with permission. Passing the automated suite alone is not a deployment certificate.

Official API documentation: [Groq structured outputs](https://console.groq.com/docs/structured-outputs), [Groq reasoning](https://console.groq.com/docs/reasoning), [Anthropic structured outputs](https://platform.claude.com/docs/en/build-with-claude/structured-outputs), [OpenAI API](https://platform.openai.com/docs/), [OpenRouter API](https://openrouter.ai/docs).
