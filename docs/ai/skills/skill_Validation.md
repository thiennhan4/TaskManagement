# skill_Validation.md — Input Validation Pattern

## Stack: C# .NET 8 + FluentValidation

---

## 1. Nguyên tắc

- Validate input **tại tầng Controller** (trước khi vào Service)
- Dùng **FluentValidation** — KHÔNG dùng DataAnnotations cho business rule
- Service KHÔNG validate format — chỉ validate business logic (e.g., duplicate, permission)
- Validation error trả về `400 Bad Request` với danh sách lỗi cụ thể

---

## 2. Cài đặt

```bash
dotnet add package FluentValidation.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();
```

---

## 3. Validator mẫu cho Task Management

```csharp
// DTOs/Task/CreateTaskDto.cs
public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public int? AssignedUserId { get; set; }
    public int ProjectId { get; set; }
}

// Validators/CreateTaskValidator.cs
public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("ProjectId is required.");
    }
}
```

---

## 4. Custom Validation Response

Override mặc định để trả về `ApiResponse` format chuẩn:

```csharp
// Program.cs
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return new BadRequestObjectResult(
            ApiResponse<object>.FailValidation(errors));
    };
});
```

---

## 5. Validator mẫu các DTO khác

```csharp
// Validators/RegisterValidator.cs
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100);
    }
}

// Validators/UpdateTaskValidator.cs
public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Title != null);

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.DueDate.HasValue);
    }
}
```

---

## 6. Frontend Validation (React)

Validate ở client TRƯỚC khi gọi API để giảm tải server:

```jsx
// Dùng react-hook-form + zod
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const schema = z.object({
    title: z.string().min(1, 'Title is required').max(200),
    dueDate: z.string().refine(val => new Date(val) > new Date(), {
        message: 'Due date must be in the future'
    }).optional(),
    priority: z.enum(['Low', 'Medium', 'High']),
    projectId: z.number().min(1),
});

export function CreateTaskForm() {
    const { register, handleSubmit, formState: { errors } } = useForm({
        resolver: zodResolver(schema)
    });

    const onSubmit = async (data) => {
        await createTask(data);
    };

    return (
        <form onSubmit={handleSubmit(onSubmit)}>
            <input {...register('title')} />
            {errors.title && <span>{errors.title.message}</span>}
            {/* ... */}
        </form>
    );
}
```
