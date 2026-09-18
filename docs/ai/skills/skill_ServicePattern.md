# Service Pattern

Service responsibilities:
- business logic
- permission check
- db operations

Service must:
- use async methods
- throw typed exceptions
- map entities to DTOs

Service must NOT:
- return IActionResult
- access HttpContext directly
- contain controller logic