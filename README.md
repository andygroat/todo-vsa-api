<!-- Improved compatibility of back to top link -->
<a id="readme-top"></a>

# Todo.VSA.API

A sample **Todo** REST API demonstrating a clean, modern **Vertical Slice Architecture (VSA)** built on **ASP.NET Core Minimal APIs** targeting **.NET 10**.

Each feature (create, get, complete, ...) lives in a single self-contained "slice" file that owns its request, validation, handler, and endpoint mapping - making the codebase easy to navigate, evolve, and delete.

## Table of Contents

- [Solution Layout](#solution-layout)
- [Features](#features)
- [Architecture](#architecture)
  - [Vertical Slice Architecture](#vertical-slice-architecture)
  - [Cross-cutting Concerns](#cross-cutting-concerns)
  - [Result Pattern](#result-pattern)
  - [Logging](#logging)
- [Built With](#built-with)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Build & Run](#build--run)
  - [Database](#database)
- [Project Structure](#project-structure)
- [Adding a New Feature Slice](#adding-a-new-feature-slice)

## Solution Layout

The solution (`Todo.VSA.API.slnx`) is composed of four projects:

| Project | Purpose |
| --- | --- |
| `Todo.VSA.Api` | ASP.NET Core Minimal API host. Contains feature slices, cross-cutting infrastructure (MediatR pipeline behaviors, Result helpers, DI wiring), and Serilog configuration. |
| `Todo.VSA.DataAccess` | Entity Framework Core `TodoDbContext`. Registered with an in-memory provider for local development. |
| `Todo.VSA.Model` | Domain model (`TodoItem`, base `BusinessObject`) and shared constants (schemas). |
| `Todo.VSA` | Shared building blocks used across the solution. |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Features

The API currently supports the following todo operations:

| Method | Route                        | Description                                          |
| ------ | ---------------------------- | ---------------------------------------------------- |
| POST   | `/api/todo`                  | Create a new todo item.                              |
| GET    | `/api/todo?search=...`       | List todo items, optionally filtered by description. |
| GET    | `/api/todo/{id}`             | Retrieve a single todo item by id.                   |
| POST   | `/api/todo/{id}/complete`    | Mark a todo item as completed.                       |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Architecture

### Vertical Slice Architecture

Feature code is organized by **capability**, not by technical layer. Every slice under `Todo.VSA.Api/Features/Todos/` follows the same shape:

- `Command` / `Query` - the MediatR request record.
- `Validator` - optional FluentValidation rules.
- `Response` - optional slice-local DTO.
- `Handler` - internal sealed handler with primary-constructor DI.
- `MapXxxEndpoint` - minimal API endpoint extension method.

Current slices:

- `CreateTodo` - `POST /api/todos`
- `GetTodos` - `GET  /api/todos`
- `GetTodoById` - `GET  /api/todos/{id:guid}`
- `CompleteTodo` - `POST /api/todos/{id:guid}/complete`

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Cross-cutting Concerns

Cross-cutting concerns are system-wide functions—such as logging, validation, or security that apply to many different features.

Implemented as MediatR **pipeline behaviors** in `Infrastructure/Behaviours/`:

- `LoggingBehavior<TRequest, TResponse>` - logs start/completion of every request.
- `ValidationBehavior<TRequest, TResponse>` - runs all FluentValidation validators for the request and throws `ValidationException` on failure.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Result Pattern

Handlers return `Result` / `Result<T>` (in `Infrastructure/ResultHelper/`) instead of throwing for expected failure cases. Endpoints translate results into appropriate HTTP responses (`200`, `201`, `204`, `400`, `404`).

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Logging

Structured logging is provided by **Serilog** (`Serilog.AspNetCore`), configured from `appsettings.json` and initialized in `Program.cs` before the host is built so startup failures are captured.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Built With

| Logo | Technology | Purpose |
| :---: | --- | --- |
| ![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white) | **.NET 10 / ASP.NET Core** | ASP.NET Minimal APIs host and runtime |
| ![EF Core](https://img.shields.io/badge/EF_Core_10-512BD4?style=for-the-badge&logo=nuget&logoColor=white) | **Entity Framework Core 10** | Data access (in-memory for dev; SQL Server package included) |
| ![MediatR](https://img.shields.io/badge/MediatR_14-BA0C2F?style=for-the-badge&logo=mediatek&logoColor=white) | **MediatR 14** | Request/response dispatch and pipeline for commands and queries |
| ![FluentValidation](https://img.shields.io/badge/FluentValidation_11-2C8EBB?style=for-the-badge&logo=checkmarx&logoColor=white) | **FluentValidation 11** | Declarative request validation |
| ![Serilog](https://img.shields.io/badge/Serilog-4B8BBE?style=for-the-badge&logo=serilog&logoColor=white) | **Serilog** | Structured logging |
| ![OpenAPI](https://img.shields.io/badge/OpenAPI-6BA539?style=for-the-badge&logo=openapiinitiative&logoColor=white) | **OpenAPI** | API description via `AddOpenApi()` |
| [![Scalar](https://img.shields.io/badge/Scalar-1B1F23?style=for-the-badge&logo=scalar&logoColor=white)](https://scalar.com/) | **Scalar** | Interactive API document UI |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) (Community edition or higher) with the **ASP.NET and web development** workload.

### Build & Run

1. Clone the repository:

   ```powershell
   git clone https://github.com/andygroat/todo-vsa-api.git
   cd todo-vsa-api
   ```
2. Open `Todo.VSA.API/Todo.VSA.API.slnx` in Visual Studio 2026.
3. Restore NuGet packages (Visual Studio does this automatically on load, or run `dotnet restore`).
4. Configure the database connection string in `Todo.VSA.API/Todo.VSA.API/appsettings.json` (used by `TodoDbContext`).
5. Set `Todo.VSA.API` as the startup project and run. The OpenAPI document will be available for exploring the endpoints.

The API starts on the URLs listed in `Todo.VSA.Api/Properties/launchSettings.json`. OpenAPI and Scalar UI are enabled for exploring the endpoints.

### Database

By default the API registers `TodoDbContext` with EF Core's **in-memory** provider (`TodoDb`) for zero-setup local development. To switch to a real provider (e.g., SQL Server), replace the registration in `Infrastructure/Extensions/WebApplicationBuilderExtensions.AddDatabaseContext` and supply a connection string via `appsettings.json` -> `ConnectionStrings`.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Project Structure

```
Todo.VSA.API/
|-- Todo.VSA.API.slnx
|-- Todo.VSA.Api/
|   |-- Features/
|   |   `-- Todos/            # Vertical slices (one file per feature)
|   |-- Infrastructure/
|   |   |-- Behaviours/       # MediatR pipeline behaviors
|   |   |-- Extensions/       # DI + endpoint registration
|   |   `-- ResultHelper/     # Result / Error types
|   `-- Program.cs
|-- Todo.VSA.DataAccess/      # EF Core DbContext
|-- Todo.VSA.Model/           # Domain entities
`-- Todo.VSA/                 # Shared building blocks
```

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Adding a New Feature Slice

1. Create a new file under `Todo.VSA.Api/Features/<Area>/<FeatureName>.cs`.
2. Declare a `Command` or `Query` implementing `IRequest<Result<T>>`.
3. Optionally add a `Validator : AbstractValidator<TRequest>`.
4. Add an `internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<TRequest, TResponse>`.
5. Add a `public static WebApplication MapXxxEndpoint(this WebApplication app)` extension and wire it up in the endpoint registration file.

The `CreateTodo` slice is the canonical reference example.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
