# Security — SLT.Manage

Findings for this repo, grounded in the code. **Secret values are never reproduced here** — categories
and remediation only. Severities: **CRITICAL** > High > Medium.

---

## CRITICAL — Live secrets committed to source control

Two committed files carry real-looking credentials (not placeholders):

**1. `SLT.Manage/appsettings.json`** (tracked):
- MongoDB **connection string** with username/password + host/port (`MonjoSettings`).
- JWT **`SignatureKey`** and **`EncryptionKey`** (`JwtServiceSettings`).
- JWT **`ClientInfo`** client secrets.
- `ApplicationPoolSettings` per-application **`PreSharedKey`** + **`MasterSignature`**.

**2. A `.env` file is tracked in git** (`git ls-files` lists it). The repo's committed `.gitignore`
historically carried **no** env patterns, so the dotenv file was committed. (The AI-layer setup added
env-ignore patterns to `.gitignore`, but that does **not** untrack an already-committed file.)

`docker-compose.yml` already parameterizes the secret values via `__`-delimited env vars in
production, so the committed `appsettings.json` is a dev/default fallback that should not hold real secrets.

**Remediation (do all):**
1. **Rotate** every leaked key/secret/credential now — assume compromised.
2. **Move** values out of `appsettings.json` to environment variables / a secret store (compose env vars,
   or a vault). Keep only non-secret defaults in the committed file.
3. **Stop tracking** the secret-bearing files: `git rm --cached SLT.Manage/appsettings.json` and
   `git rm --cached .env`, then commit. The env-ignore patterns are now in `.gitignore`, but the
   already-tracked dotenv file must be removed from the index explicitly.
4. Scrub history if these secrets were ever pushed to a shared remote.

---

## High — `MasterSignature` bypass + committed `test` application

A `MasterSignature` fully bypasses the nonce + HMAC checks in `SignatureMiddleware` — any holder skips replay
protection. A `"test"`/`"test"`/`"test"` application is present in the committed application pool.

**Remediation:** remove the `test` app from all non-local configs; reconsider whether `MasterSignature` should
exist at all; if it must, scope it tightly and rotate it.

---

## High — Allow-all firewall default

`FirewallSettings` default rule is `Regex "^(.*)$"`, `IPAddresses ["*"]`, `Policy Allow` — the firewall
middleware is effectively open unless the production environment tightens `FirewallSettings__Rules`.

**Remediation:** ship a restrictive default (deny-by-default + an explicit allow-list); verify prod config
actually overrides it.

---

## High — Sensitive request logging

`SLT.Manage/Utilities/Middlewares/RequestLoggingMiddleware.cs` persists the **full request body and all
headers (including `Authorization`)** to the `RequestLogs` collection for every `/api` call. It runs before
auth, so tokens and credentials land in the database in plaintext.

**Remediation:** redact `Authorization` and other secret headers + sensitive body fields before persisting;
add a retention/rotation policy for `RequestLogs`.

---

## Medium — `NotFoundException(string)` returns HTTP 500

`NotFoundException(string)` leaves the HTTP status at 500 while the `ApiResult.statusCode` enum says
`NotFound`. This can mask real not-found responses as server errors. Only the
`NotFoundException(ApiResultStatusCode, string)` overload sets HTTP 404.

**Remediation:** use the two-arg ctor on not-found paths (or make it the default).

---

## Medium — Silent failures (empty `catch {}`)

The JWT-validation path in the request-logging middleware swallows exceptions with an empty `catch {}`, and
`UserService.CreateAdminAsync` returns `false` on any exception. Failures are invisible.

**Remediation:** log the exception (Sentry is already wired) instead of swallowing it.

---

## Medium — Swagger exposed in all environments

`Program.cs` calls `app.UseSwaggerAndUI()` with no environment argument, and the extension body
(`SwaggerConfigurationExtensions.UseSwaggerAndUI`) has no env gate — so the Swagger JSON and UI are
served in **every** environment, including production. On an admin API this discloses the full endpoint
surface (and the four security header schemes) to anyone who can reach the host.

**Remediation:** gate `UseSwaggerAndUI` behind `app.Environment.IsDevelopment()` (matching how
`UseDeveloperExceptionPage`/`UseHsts` receive `app.Environment`), or protect the Swagger route.

---

## Medium — Short symmetric JWT key

The JWT `EncryptionKey` is documented as "must be 16 character" (AES-128). Key length is minimal.

**Remediation:** use a longer key / stronger algorithm where the token library allows.

---

## Note — no tests / no CI

There is no test project and no CI pipeline, so none of the above is guarded against regression. Recommend a
minimal xUnit project + a build/security check in CI.

## Optional — deep AI security audit (deepsec)

For a periodic *deep* pass beyond the `secret-scan` / `dependency-audit` skills and the
diff-scoped `review` skill, consider [`vercel-labs/deepsec`](https://github.com/vercel-labs/deepsec)
— an AI-agent vulnerability scanner that reasons over the whole repo for auth gaps, broken
access control, crypto misuse (IV reuse, missing constant-time compares, algorithm confusion),
SSRF, and injection in *your own* logic. It has real .NET / ASP.NET Core support and is
host-agnostic (scans a local tree — no GitHub/GitLab dependency).

Use it as a **one-shot (then occasional) audit — NOT standing CI, NOT a wrapped skill** (that
would be over-engineering for these repos). It does **not** replace `secret-scan` (deepsec does
NOT catch secrets committed in `appsettings.json`) or `dependency-audit` (it does no CVE/SCA);
it complements them by finding logic/auth/crypto flaws those miss — high value on the JWE auth
flow and the per-app Signature/Nonce gate (this admin repo has no on-chain code).

```bash
npx deepsec init                      # scaffolds .deepsec/ (config + data/<id>/INFO.md + SETUP.md)
cd .deepsec && pnpm install           # deepsec is a Node tool; this is its own dep install
# add an AI credential per .deepsec/SETUP.md (Vercel AI Gateway key or an Anthropic token)
```

Then fill `.deepsec/data/<id>/INFO.md` (≤100 lines) with the project primitives the regex layer
can't anchor — **the custom JWE auth flow, the per-app Signature/Nonce gate, the admin permission-code
model, and Monjo data access. NOTE: this admin repo has NO blockchain/chain libraries — it reads
chain-derived data only (no hot-wallet, no Nethereum, no Tron)** — then:

```bash
pnpm deepsec scan                     # fast, no AI
pnpm deepsec process --limit 50       # CALIBRATE cost first (Opus ≈ $25–60 / 100 files)
pnpm deepsec process --concurrency 5  # full pass once satisfied
pnpm deepsec revalidate --min-severity HIGH
pnpm deepsec export --format md-dir --out ./findings
```

Caveats: cost scales with file count (always `--limit` first); it runs a coding agent with shell
access (your source is trusted; prefer its sandbox mode if any vendored third-party code is present).
`.deepsec/` is gitignored by this repo's AI-layer setup.
