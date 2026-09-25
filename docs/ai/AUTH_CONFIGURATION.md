# Authentication configuration after secret remediation

Verified 2026-09-24 (Phase 1 automated checks; live Google/browser checks remain manual).

The API project has UserSecretsId `0a957b98-ee06-4bc5-aa80-ee0f8ad75c5e`.
The Development launch profiles load that local .NET User Secrets store automatically.
Configure `Jwt:Key` there for development, or `Jwt__Key` in deployment configuration.
Use a cryptographically random signing key of at least 32 ASCII bytes; a Base64 encoding
of 64 random bytes is suitable. Generate it once outside application startup and send it
to `dotnet user-secrets set` through stdin, without displaying or writing the value to a file.
Preserve an existing valid key. Never use a hardcoded fallback or commit secrets.

Startup and token generation share validation. Missing, whitespace and undersized keys
fail with a safe configuration error. Production still requires an external signing key.
The observed login/register failure was IdentityModel `IDX10720`: the configured local key
was only 200 bits, so startup succeeded but HS256 signing failed after password verification.
Replacing that invalid development key and validating its size at startup fixes the regression.

`Jwt:Issuer` and `Jwt:Audience` remain `TaskHubServer` and `TaskHubClient`; custom configured
values are supported. Missing or blank values use these same defaults for signing and validation.
Access tokens retain the existing name identifier, email and role claims and one-hour lifetime.
Refresh tokens retain 64 random bytes, seven-day expiry, persistence and rotation.
The current code has no configurable JWT/refresh expiry setting; these durations remain unchanged.

Google login validates a Google ID token against **`Google:ClientId`**. It does not exchange
an authorization code and does not consume **`Google:ClientSecret`**. Keep any Google credentials
in User Secrets or deployment configuration. The frontend requires **`VITE_GOOGLE_CLIENT_ID`**
matching the backend ClientId; never put a client secret in frontend configuration.
Missing backend ClientId returns a controlled 503 response. Invalid Google ID tokens return 401
without logging the credential or the validation exception. Verify a real Google sign-in manually
and configure authorized JavaScript origins in the Google Cloud OAuth client as needed.

Existing `/api/auth/*` and `/api/settings/password` routes remain compatible with the frontend.
Refresh cookies remain HttpOnly, Secure, SameSite=Strict, path `/`. Use HTTPS (or browser-supported
localhost development) when verifying browser cookie behavior. The Axios interceptor excludes
refresh and Google auth errors from automatic refresh, preventing a refresh promise from awaiting itself.

Login revokes existing refresh tokens, password changes revoke all active refresh tokens, and logout
revokes the supplied refresh token. Existing access tokens retain their normal expiry after these events.
AuthService and TokenService now persist through repositories, following AGENTS.md.

Regression coverage includes endpoint lifecycle, duplicate registration, password hashing, JWT signature
and claims, refresh expiry and replay, password change/revocation, logout, missing Google configuration,
and production startup validation. Tests generate ephemeral signing material and credentials in memory;
assertions must never print their values. Run the backend suite with a random process-only `Jwt__Key`,
then `node --test tests/authRefresh.test.js` from `frontend` for interceptor regression coverage.

## Phase 1 identity and response policy

All four auth actions return only the public access token and self-user DTO. The internal
AuthResult carries the refresh secret only as far as the controller, which sets the existing
HttpOnly/Secure/SameSite=Strict cookie. Inactive refresh revokes existing refresh sessions,
returns a controlled error, and issues no replacement cookie. Atomic refresh consumption
and concurrent rotation remain Phase 2 (C02).

The auth limiter is attached to register, login, Google and refresh. Each remote IP/action
has five fixed-window permits per minute; route casing and trailing slash share a bucket.
Rejection uses the standard error envelope with Retry-After. The current implementation
uses the connection IP; proxy deployments must supply a correctly configured trusted-proxy
boundary rather than trusting arbitrary forwarded headers.

GoogleJsonWebSignature validates signature, audience and token validity against ClientId.
The service additionally requires a subject and verified email. This application identifies
accounts by current email: automatic email binding is permitted only for gmail.com, or a
verified Google hosted domain matching the email domain. Other external-email Google
accounts are rejected with a controlled linking-required response; no implicit linking
or new provider-subject persistence was introduced. An explicit linking feature is not
implemented in Phase 1. This is email-based authentication, not a claim of durable provider
identity across address changes or reassignments.

Google documents why third-party email without a hosted-domain claim is not authoritative
even when email_verified is true: [Google backend ID-token verification](https://developers.google.com/identity/sign-in/web/backend-auth).
The restricted policy therefore needs no schema migration. Tests cover verified Gmail,
matching/mismatched hosted domains, missing subject, unverified email, external email,
missing ClientId, malformed tokens and safe successful HTTP serialization. Successful
identity claims are supplied by a test verifier; real Google sign-in remains manual.
