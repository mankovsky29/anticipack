1. Agent Mode & Communication Protocol
Ask Before Acting: If a request is ambiguous, lacks context, or has multiple interpretations, do not make assumptions.

Mandatory Follow-up: Ask clarifying questions before generating code or running commands.

Confirm Scope: Briefly restate the task and ask for confirmation for significant changes (e.g., refactoring, file deletion).

Mandatory Thinking Process: Before implementation, explicitly state:

Domain Analysis: Identify Aggregates, Ubiquitous Language, and invariants.

Architecture Review: Assign responsibilities to layers (Domain, Application, Infrastructure).

Implementation Plan: List modified files and test cases.

2. C# 14 & General Coding Standards
Version: Always use C# 14 features (e.g., enhanced params, field-backed properties if applicable).

Naming Conventions:

PascalCase for types, methods, and public members.

camelCase for private fields (prefixed with _) and local variables.

I prefix for interfaces (e.g., IRepository).

Formatting:

Newline before opening curly braces.

File-scoped namespaces and single-line usings.

Use nameof() instead of magic strings.

Use pattern matching and switch expressions for readability.

Nullability: Use Nullable Reference Types. Use is null or is not null. Trust the type system; don't over-validate non-nullable types.

3. Domain-Driven Design (DDD) & Architecture
Layer Responsibilities
Domain Layer: Pure logic. Contains Aggregates (consistency boundaries), Value Objects (immutable, use record), Domain Services, and Domain Events.

Application Layer: Orchestration. Contains Application Services, DTOs, and MediatR Commands/Queries. Validates input here.

Infrastructure Layer: Implementation of abstractions (EF Core, Message Bus, External APIs).

Security & Compliance
Implement authorization at the Aggregate level.

Financial Precision: Use decimal for all monetary values. Use currency-aware Value Objects.

Audit Trails: Use Domain Events to create an immutable history of state changes (PCI-DSS/SOX compliance).

4. Data Access & Performance
EF Core: Use the Repository Pattern where beneficial, but don't over-abstract if simple DBContext usage suffices.

Concurrency: Implement optimistic concurrency checks on Aggregates.

Async: Use async/await for all I/O-bound operations. Avoid Task.Result or .Wait().

Optimization: Use Pagination, Filtering, and Compiled Queries for large datasets.

5. Validation & Error Handling
Validation: Use FluentValidation for complex logic and Data Annotations for simple DTO schemas.

Exceptions: Use a global exception handler (Middleware) to return Problem Details (RFC 9457).

Resilience: Use Microsoft.Extensions.Resilience (Polly) for retries, circuit breakers, and timeouts when calling external services.

6. Testing Standards
Naming: MethodName_Condition_ExpectedResult().

Categories: Unit (Domain logic), Integration (Persistence/API), and Acceptance (End-to-end).

Guidelines: Do not use "Arrange/Act/Assert" comments. Maintain 85% coverage for Domain/Application layers. Mock external dependencies.

7. Deployment & DevOps
Containerization: Prefer .NET's built-in container support: dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer.

Observability: Use structured logging (Serilog) and OpenTelemetry for traces and metrics.

Health Checks: Implement /health/ready and /health/live endpoints.

8. Mandatory Verification Checklist
Before delivering code, you must confirm:

[ ] "I have verified that Aggregates model business concepts and encapsulate logic."

[ ] "I have followed SOLID principles and the specified layer boundaries."

[ ] "I have used decimal for financial values and verified transaction integrity."

[ ] "I have written tests following the MethodName_Condition_ExpectedResult pattern."

[ ] "I have used C# 14 features and followed the .editorconfig style."

Follow-up Questions
To ensure this is perfectly tailored to your environment:

Do you have a preferred library for Mediator patterns (e.g., MediatR)?

Should I prioritize Minimal APIs or Controller-based APIs for your standard templates?

Are there specific Financial Regulations (e.g., specific rounding rules) I should bake into the domain service logic by default?