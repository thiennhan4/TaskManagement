# TaskHub

## JWT configuration

The API requires `Jwt:Key` at startup. Missing, empty, or whitespace values cause
startup to fail; there is no default signing key in source control.

For development, configure `Jwt:Key` through .NET User Secrets for
`backend/TaskHub.API/TaskHub.API.csproj`. For deployments, supply `Jwt__Key`
through the deployment environment or secret manager. Keep real secrets out of
tracked configuration files. If the previous signing key was ever used, rotate
it before running the application; existing access tokens will become invalid.

Integration tests require an ephemeral `Jwt__Key` environment variable in the
test process. Use a test-only value of at least 32 ASCII characters, never the
application signing key. Do not save this value in a settings file.
