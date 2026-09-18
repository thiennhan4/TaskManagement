# skill_Testing.md — Testing Pattern

## Stack: C# .NET 8 + xUnit + Moq

---

## 1. Nguyên tắc

- Test **Service layer** — nơi chứa business logic
- Mock dependencies bằng **Moq**
- Follow **AAA pattern** (Arrange-Act-Assert)
- Naming: `MethodName_Scenario_ExpectedResult`

---

## 2. Project Setup

```bash
dotnet new xunit -n TaskHub.Tests
dotnet sln add TaskHub.Tests
dotnet add TaskHub.Tests reference TaskHub
dotnet add TaskHub.Tests package Moq
dotnet add TaskHub.Tests package FluentAssertions  # optional
```

---

## 3. Unit Test — Service

```csharp
public class AuthServiceTests
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _mockTokenService = new Mock<ITokenService>();
        _service = new AuthService(_mockContext.Object, _mockTokenService.Object);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsSuccess()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Password123",
            FullName = "Test User"
        };

        // Act
        var result = await _service.RegisterAsync(dto);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflict()
    {
        // Arrange — setup existing user
        // ...

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(duplicateDto));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto { Email = "test@example.com", Password = "wrong" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync(dto));
    }
}
```

---

## 4. Unit Test — TaskService

```csharp
public class TaskItemServiceTests
{
    private readonly Mock<ITaskItemRepository> _mockRepo;
    private readonly TaskItemService _service;

    public TaskItemServiceTests()
    {
        _mockRepo = new Mock<ITaskItemRepository>();
        _service = new TaskItemService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTask_ReturnsTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, OwnerId = userId, Title = "Test" };

        _mockRepo.Setup(r => r.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        // Act
        var result = await _service.GetByIdAsync(taskId, userId);

        // Assert
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TaskItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsForbidden()
    {
        // Arrange
        var task = new TaskItem { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        _mockRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

        var differentUser = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteAsync(task.Id, differentUser));
    }
}
```

---

## 5. Integration Test

```csharp
public class AuthFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthFlowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Login_Refresh_Logout_Flow()
    {
        // 1. Register
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "flow@test.com", Password = "Test123!", FullName = "Flow" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // 2. Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "flow@test.com", Password = "Test123!" });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginData?.AccessToken);

        // 3. Access protected endpoint
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginData.AccessToken);
        var profileResponse = await _client.GetAsync("/api/settings/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }
}
```

---

## 6. Test Data Builder

```csharp
public static class TestDataBuilder
{
    public static AppUser CreateUser(
        string email = "test@example.com",
        UserRole role = UserRole.User)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FullName = "Test User",
            Role = role
        };
    }

    public static TaskItem CreateTask(Guid ownerId, Guid listId)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            OwnerId = ownerId,
            ListId = listId,
            Status = "Todo"
        };
    }
}
```

---
