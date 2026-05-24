# Exception Rules

## Use typed exceptions only

- NotFoundException
- ForbiddenException
- ValidationException
- UnauthorizedException

## Never:

- throw Exception()
- return null for error cases

## GlobalExceptionMiddleware handles:
- status code
- error response
- logging