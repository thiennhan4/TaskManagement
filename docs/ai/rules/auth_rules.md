# 🔐 Auth Rules — TaskHub

## Stack: C# .NET 8 + JWT + BCrypt + SQL Server

---

## 1. Authentication Flow

```txt
Register → Hash password (BCrypt) → Save user → Return success
Login → Verify password → Generate JWT + Refresh Token → Return tokens
Refresh → Validate refresh token → Generate new JWT → Rotate refresh token
Logout → Revoke refresh token
```

---

## 2. JWT Configuration

```csharp
// appsettings.json
{
    "Jwt": {
        "Key": "...",          // ⚠️ Move to env var in production!
        "Issuer": "TaskHubServer",
        "Audience": "TaskHubClient"
    }
}
```

### Token Validation Rules

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,           // ✅ Validate issuer
    ValidateAudience = true,         // ✅ Validate audience
    ValidateLifetime = true,         // ✅ Check expiration
    ValidateIssuerSigningKey = true,  // ✅ Verify signing key
    ValidIssuer = "TaskHubServer",
    ValidAudience = "TaskHubClient",
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ClockSkew = TimeSpan.Zero        // ✅ No grace period
};
```

---

## 3. JWT Claims

Khi generate JWT token, PHẢI include các claims sau:

```csharp
new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, user.Role.ToString()),
    new Claim(ClaimTypes.Name, user.FullName)
}
```

### Lấy userId trong Controller

```csharp
private Guid GetUserId() =>
    Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

---

## 4. Authorization Rules (RBAC)

### 4.1 Global Roles

| Role    | Enum Value       | Quyền               |
| ------- | ---------------- | -------------------- |
| Admin   | `UserRole.Admin` | Toàn quyền hệ thống |
| Manager | `UserRole.Manager` | Quản lý (reserved) |
| User    | `UserRole.User`  | Người dùng thường    |

### 4.2 Role Enum

```csharp
public enum UserRole
{
    Admin,
    Manager,
    User
}
```

- ❌ Do NOT hardcode role strings — use `UserRole` enum
- ✅ Always check role via enum: `UserRole.Admin`, `UserRole.User`
- ✅ Store as string in DB (HasConversion<string>)

---

## 5. Authorization Decision Tree

```pseudo
function checkPermission(user, resource):
    IF user.role == Admin → ALLOW

    IF resource is personal:
        IF resource.ownerId == user.id → ALLOW
        ELSE → DENY

    IF resource is team-based (Phase 2):
        member = getTeamMember(user.id, resource.teamId)
        IF member == null → DENY
        IF member.role == Owner → ALLOW
        IF member.role == Manager → ALLOW
        IF member.role == Member:
            IF resource.assignedTo == user.id → ALLOW
            ELSE → DENY (view only)
```

---

## 6. Password Rules

```csharp
// ✅ Hash with BCrypt
var hash = BCrypt.Net.BCrypt.HashPassword(password);

// ✅ Verify
var isValid = BCrypt.Net.BCrypt.Verify(password, hash);

// ❌ NEVER store plain text password
// ❌ NEVER log passwords
// ❌ NEVER return password hash in response
```

---

## 7. Refresh Token Rules

- Refresh token = random string (128-bit)
- Store in DB with `ExpiresAt`, `IsRevoked` fields
- On refresh: revoke old token → generate new token pair
- On logout: revoke all user tokens
- ⚠️ TODO: Hash refresh token with SHA256 before storing

---

## 8. Security Checklist

- [x] JWT validation (iss, aud, exp, signing key)
- [x] Password hashing (BCrypt)
- [x] Rate limiting on auth endpoints (5/min)
- [x] CORS configured (localhost:5173)
- [x] ClockSkew = Zero (no grace period)
- [x] Refresh token rotation
- [ ] Store JWT secret in environment variable (production)
- [ ] HTTPS enforcement (production)
- [ ] Refresh token hashing (SHA256)
- [ ] Account lockout after failed attempts

---

## 9. Auth Endpoints

| Method | Endpoint              | Auth Required | Rate Limited |
| ------ | --------------------- | ------------- | ------------ |
| POST   | `/api/auth/register`  | ❌            | ✅ (5/min)   |
| POST   | `/api/auth/login`     | ❌            | ✅ (5/min)   |
| POST   | `/api/auth/refresh`   | ❌            | ✅ (5/min)   |
| POST   | `/api/auth/logout`    | ✅            | ❌           |

---
