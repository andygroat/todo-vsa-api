# copilot-instructions.md

## Overview
[Todo-Vsa-Api] - Vertical Slice Architecture Web API. Each feature is self-contained (request, handler, validator, endpoint) rather than split across horizontal layers.

## Tech Stack

- **.NET 10** / **ASP.NET Core** (Minimal APIs).
- **Entity Framework Core 10** (InMemory provider by default; SQL Server package available).
- **Serilog** / **Serilog.AspNetCore** for structured logging.
- **OpenAPI** via `AddOpenApi()` and **Scalar.AspNetCore** for the interactive UI.
- **MediatR 14** for commands, queries, and notifications.
- **FluentValidation 12** for validation.
- `Nullable` and `ImplicitUsings` are **enabled** in every project.

## Project Structure

`Todo-Vsa-Api` is an ASP.NET Core Web API built on **.NET 10**, using a vertical slice architecture.

| Project | Purpose |
|---|---|
| `Todo.Vsa.Api` | ASP.NET Core host, features (vertical slices), infrastructure, extensions. |
| `Todo.Vsa.DataAccess` | EF Core `DbContext` (`TodoDbContext`) and persistence concerns. |
| `Todo.Vsa.Model` | Shared domain models / DTOs. |
| `Todo.Vsa` | Shared kernel / cross-cutting types. |
| `Todo.Vsa.Api.Tests` | TUnit tests. `InternalsVisibleTo` is granted from the API project. |

Key infrastructure locations inside `Todo.Vsa.Api`:
- `Infrastructure/Extensions/WebApplicationBuilderExtensions.cs` – DI wire-up (`AddApplicationBuilingBlocks`).
- `Infrastructure/Behaviours/` – MediatR pipeline behaviours (`LoggingBehavior`, `ValidationBehavior`).
- `Infrastructure/Exceptions/` – Global + validation exception handlers.
- `Features/` – Vertical slices, one folder per feature.

## Code Conventions

- Target framework is `net10.0` — use modern C#/.NET 10 language and API features.
- Use `TodoDbContext` (in `Todo.Vsa.DataAccess`) directly from handlers.
- Do not introduce a service/repository layer for features – keep logic inside the slice's handler and use `TodoDbContext` directly.
- CQRS / mediation: One handler per request.
- Validators registered via `FluentValidation.AspNetCore`. Place validators next to the requests they validate.
- File-scoped namespaces.
- Primary constructors for dependency injection.
- Sealed classes for implementations.
- Internal by default, public only for contracts.
- Naming: PascalCase for types/members, camelCase for locals/parameters, `_camelCase` for private fields.
- Prefer `async`/`await` end-to-end; accept and propagate `CancellationToken`.
- Return `Results.*` / `TypedResults.*` from minimal API endpoints; avoid `IActionResult`.

## When Adding Code

When asked to add or modify a feature, place everything under `Todo.Vsa.Api/Features/<FeatureName>/` and prefer a single file per slice containing:

1. **Request** – `public sealed record XyzCommand(...) : IRequest<XyzResponse>;` (or `IRequest`, or `IRequest<Result>`).
2. **Response** – `public sealed record XyzResponse(...);` when applicable.
3. **Validator** – `public sealed class XyzCommandValidator : AbstractValidator<XyzCommand>` (auto-registered from the API assembly).
4. **Handler** – `public sealed class XyzCommandHandler(TodoDbContext db) : IRequestHandler<XyzCommand, XyzResponse>` implementing `Handle`.
5. **Endpoint** – controller action (or minimal endpoint mapper) that resolves `ISender` and calls `Send`.

Do **not** register handlers/validators manually

## Error Handling

- **Exception handling:** Rely on `app.UseExceptionHandler()` with the custom exception handlers already registered — add new handlers there instead of try/catching in endpoints.

## Testing

- Test Framework: **[TUnit](https://github.com/thomhurst/TUnit)** – use `[Test]`, `[Arguments(...)]`, and `await Assert.That(...)`.
- Tests live in `Todo.Vsa.Api.Tests` and can access `internal` members of `Todo.Vsa.Api`.
- Test runner is the Microsoft Testing Platform (`UseMicrosoftTestingPlatformRunner=true`); run with `dotnet test` or `dotnet run` on the test project.
- Aim to test each slice's handler and validator. Bootstrapping code is excluded from coverage.
- Code Coverage: `Microsoft.Testing.Extensions.CodeCoverage` produces Cobertura reports consumed by CI. Do not introduce xUnit/NUnit/MSTest.

## Logging

- Use Serilog via injected `ILogger<T>`.
- Include contextual properties (e.g., `todoId`, `search`) using message templates, not string interpolation.

## Do

- ✅ Do target `.NET 10` idioms (primary constructors, collection expressions, `required` members where sensible).
- ✅ Keep feature code inside its slice folder.
- ✅ Use MediatR, FluentValidation, Serilog, and EF Core idiomatically.
- ✅ Use `sealed record` for requests/responses, `sealed class` for handlers/validators.
- ✅ Propagate `CancellationToken` from controller → `ISender.Send` → handler → EF Core.

## Don't

- ❌ Don't add a Services/Repositories layer or generic CRUD abstractions.
- ❌ Don't call `DbContext` from controllers – go through MediatR.
- ❌ Don't use `Console.WriteLine` or `ILogger` string interpolation – use structured Serilog templates.
- ❌ Don't manually register MediatR handlers or FluentValidation validators.
- ❌ Don't change target frameworks or downgrade package versions.