# K4RMA Live Copilot

Phone-first presentation Q&A grounded in the K4RMA C# source, your later diff exports, and Jeongmin's submitted Week 10 PowerPoint notes.

**Actual working website**. Built with dependency-free Node.js and plain JS/CSS so you can run it immediately without `npm install`.

## Run locally

1. Install Node.js 20 or later.
2. Open this project folder in Terminal / PowerShell.
3. In `cmd` / PowerShell, set `GROQ_API_KEY` on the server:

   PowerShell:
   ```powershell
   $env:GROQ_API_KEY="gsk_YOUR_KEY"
   $env:APP_PIN="choose-a-secret-pin"
   npm start
   ```

   macOS/Linux:
   ```bash
   GROQ_API_KEY="gsk_YOUR_KEY" APP_PIN="choose-a-secret-pin" npm start
   ```
4. Open **http://localhost:3000** in the same computer.
5. On a hosted HTTPS copy, open the web app on your phone; set the PIN under **Security & preferences**.

Without `GROQ_API_KEY`, the app supports manual question search **in evidence-only mode**; it does not make up answers. With only `OPENROUTER_API_KEY`, text answers work but mic transcription still needs Groq.

## Deploy to Vercel (free hosting for experimentation)

1. Create a private repository with these app files. **Review the embedded source corpus before publishing** — the uploaded source may include team/private work. Do not make it public unless your team approves.
2. Import the project into Vercel. Use **Other** / no frontend framework; output directory `public` when requested. The repo has `api/*.js` Vercel functions and static `public/*` assets.
3. Add server environment variables `GROQ_API_KEY`, `APP_PIN`, and optionally `OPENROUTER_API_KEY` / `OPENROUTER_MODEL`.
4. Deploy. Open the generated **https** URL on your phone, and allow microphone access. The phone and laptop can use the same site; neither uploads the full repository per question.
5. If deploying to a platform without serverless handlers, run `node server.mjs` as a long-running process behind HTTPS instead.

**Security:** API keys must remain on the server. Never put them in `public/app.js`, the client form, a committed `.env`, or a public GitHub repo. The optional PIN is a lightweight access gate only, not full authentication. A public deployment without a PIN is unsafe because strangers can spend your API quota. Audio clips are sent to Groq, not saved by this app. Third-party provider handling and retention are controlled by their own policies.

## Use

- Select the active slide and answer language. For Rising Slash use slides **19–23**.
- Tap the microphone to record independent ~9-second Korean audio clips. Whisper transcribes each; a lightweight detector proposes questions and the app answers the detected questions automatically. Always check the transcript. This is *near-live*, not streaming word-by-word.
- Use **Answer latest** if a question was missed, or enter the question manually. This remains the most reliable fallback.
- Click source citations to see excerpts. Each excerpt is labeled **submitted-slide**, **archived-source**, or **later-diff**.
- The PWA can be installed to the home screen from the browser menu when hosted on HTTPS. Screen Wake Lock is requested where supported, but do not lock the phone or switch to the background during listening.

## Data integrity / important limitations

The source archive is **Jeongmin's feat/2d-prototype snapshot (Nov 1)**, *not* your final local `feat/rising-slash` Git HEAD. We additionally indexed the two user-supplied diff exports for the four Rising Slash changes and test harness. These are **later diff excerpts**, not a reconstructed, compiled source tree. The app tells the model to prefer the later excerpts over older code when conflicting. The submitted slide 19 may describe earlier UP01 single-sample tracking; the later diff introduces tracking then locking.

**Do not treat `high confidence` as proof of runtime behavior.** Unity Play Mode and actual professor audio have NOT been tested here. If you want a truly current source corpus, replace `source_snapshot` with the latest code and rerun `python build_knowledge.py` after checking the relevant diff data and presentation path.

## Testing

```bash
npm test
npm start
```

Use manual UI checks in a browser, then test microphone capture on an HTTPS phone and a Korean audio sample from classroom distance. Browsers can suspend microphone recording when backgrounded. For a real class assessment, obtain the professor's permission for live AI assistance and audio capture; otherwise use Rehearsal Mode / manual question practice.

## Source paths

- `lib/core.mjs` — retrieval, API integrations, source-grounding rules, question classification
- `api/answer.js` & `api/transcribe.js` — Vercel functions
- `server.mjs` — zero-dependency Node local server
- `public/app.js` — microphone capture, transcription queue, UI actions
- `knowledge.json` — pre-indexed source, later diffs, 28 slides + notes
- `build_knowledge.py` — local index generator (requires `python-pptx` if rebuilding)

## Model defaults

- `whisper-large-v3-turbo` — Korean STT via Groq (separate 9s clips)
- `openai/gpt-oss-120b` — fast code Q&A via Groq
- `openrouter/free` — optional fallback with routing variability

Groq endpoints: `https://api.groq.com/openai/v1/audio/transcriptions` and `/chat/completions`. This is designed to work with current API contracts but requests cannot be tested without your own private key.
