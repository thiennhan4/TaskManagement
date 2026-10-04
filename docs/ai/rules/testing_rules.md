# 🧪 Testing Rules — TaskHub

## Stack: C# .NET 8 + xUnit + Moq

---

## 1. Testing Strategy

| Level | Framework | Mục đích |
|-------|-----------|----------|
| Unit Test | xUnit + Moq | Test service logic riêng lẻ |
| Integration Test | WebApplicationFactory | Test full API pipeline |
| Manual Test | Postman / Swagger | Test API endpoints |

---

## 2. Unit Test Rules

- ✅ Test Service layer — KHÔNG test Controller trực tiếp
- ✅ Mock tất cả dependencies (Repository, DbContext)
- ✅ Test cả happy path và error path
- ✅ Naming: `MethodName_Scenario_ExpectedResult`

```csharp
public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        // ...

        // Act
        var result = await service.LoginAsync(dto);

        // Assert
        Assert.NotNull(result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorized()
    {
        // Arrange + Act + Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => service.LoginAsync(invalidDto));
    }
}
```

---

## 3. Test Coverage Targets

| Service | Priority | Focus |
|---------|----------|-------|
| AuthService | 🔴 High | Login, Register, Token rotation |
| PermissionService | 🔴 High | Role checks, ownership |
| TaskItemService | 🟡 Medium | CRUD, permission integration |
| BoardService | 🟡 Medium | CRUD, ownership check |

---

## 4. Test Organization

```txt
TaskHub.Tests/
├── Unit/
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   ├── TaskServiceTests.cs
│   │   └── PermissionServiceTests.cs
│   └── Helpers/
│       └── TokenHelperTests.cs
├── Integration/
│   ├── AuthFlowTests.cs
│   ├── TaskFlowTests.cs
│   └── TestWebApplicationFactory.cs
└── Fixtures/
    └── TestDataBuilder.cs
```

---

## 5. AAA Pattern

Mọi test PHẢI follow **Arrange-Act-Assert**:

```csharp
[Fact]
public async Task CreateTask_ValidInput_ReturnsTask()
{
    // Arrange — setup data & mocks
    var dto = new CreateTaskDto { Title = "Test" };

    // Act — call method
    var result = await _service.CreateAsync(dto, userId);

    // Assert — verify result
    Assert.Equal("Test", result.Title);
}
```

---

## 6. Rules

- ❌ KHÔNG test private methods — test qua public API
- ❌ KHÔNG test framework code (EF Core, ASP.NET)
- ✅ Test edge cases (null, empty, max length)
- ✅ Test exception throwing
- ✅ Mỗi test chỉ assert 1 behavior

---
