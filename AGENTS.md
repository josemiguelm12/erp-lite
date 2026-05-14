## Global Engineering Rules

## General Objective

Always generate clean, scalable, secure and maintainable code.

The code must follow professional software engineering practices and be suitable for real-world production systems.

---

## Code Quality

- Write clean, readable and maintainable code.
- Follow SOLID principles.
- Apply Single Responsibility Principle strictly.
- Avoid large classes, large methods and duplicated logic.
- Prefer composition over inheritance when possible.
- Use dependency injection where applicable.
- Keep business logic separated from controllers, routes or UI components.
- Use meaningful names for variables, methods, classes and files.
- Avoid hardcoded values when they should be configurable.
- Use async/await for I/O operations when supported.

---

## Architecture

- Keep responsibilities clearly separated.
- Avoid mixing infrastructure, business logic and presentation concerns.
- Prefer modular and scalable structures.
- Keep code organized by layers, features or modules depending on the project architecture.
- Respect the existing architecture of the project before generating or modifying code.
- Do not introduce unnecessary complexity.
- Prefer explicit and predictable code over clever or obscure solutions.

---

## Security

- Never expose API keys, secrets, passwords, tokens or connection strings in the source code.
- Use environment variables, secret managers or secure configuration providers.
- Do not commit sensitive values to repositories.
- Access third-party API keys securely through backend services, server-side functions or edge functions.
- Never expose private API keys directly to the frontend.
- Validate and sanitize all user input.
- Use parameterized queries or ORM-safe query methods to prevent SQL injection.
- Never concatenate raw user input into SQL queries.
- Use secure password hashing algorithms.
- Do not log passwords, tokens, refresh tokens, API keys or sensitive user data.
- Return safe and generic error messages to clients.
- Log detailed errors only internally.

---

## Input Validation & Sanitization

- Validate all incoming request data.
- Sanitize inputs when needed to prevent injection attacks.
- Validate:
  - Required fields
  - Data types
  - String length
  - Email formats
  - Numeric ranges
  - Dates
  - IDs
  - Enum values
- Never trust data coming from the frontend.
- Use validation libraries appropriate to the stack.

---

## SQL Injection Protection

- Always use parameterized queries.
- Prefer ORM query builders or safe query APIs.
- Do not build SQL queries using string concatenation with user input.
- If raw SQL is necessary, use parameters explicitly.
- Review all database access code for injection risks.

---

## API Security

- Protect API routes with authentication when required.
- Apply authorization checks for sensitive actions.
- Use role-based or permission-based authorization when applicable.
- Add rate limiting to API routes, especially:
  - Login
  - Register
  - Password reset
  - Token refresh
  - Public endpoints
  - Expensive queries
- Use HTTPS in production.
- Apply secure CORS policies.
- Do not allow unrestricted origins in production.
- Use antiforgery/CSRF protection when the application uses cookies or browser-based authenticated sessions.
- For stateless JWT APIs, prioritize Authorization headers, short-lived tokens and refresh token protection.

---

## Rate Limiting

- Add rate limiting to all API routes when possible.
- Use stricter limits for authentication endpoints.
- Return proper HTTP status codes such as 429 Too Many Requests.
- Avoid exposing implementation details in rate limit responses.

---

## Error Handling

- Use centralized error handling where possible.
- Do not expose stack traces or internal exceptions to clients.
- Use consistent error response formats.
- Log errors with enough context for debugging.
- Avoid swallowing exceptions silently.

---

## Logging & Auditing

- Add structured logging for important actions.
- Log security-relevant events such as:
  - Failed login attempts
  - Permission denied actions
  - Token refresh failures
  - Sensitive data changes
- Do not log secrets or sensitive payloads.
- Add audit logs when the project requires traceability.

---

## Frontend & UI

- Use the configured Stitch MCP server for graphical interface design when available.
- Prefer using the UI/design MCP tool instead of manually inventing interface layouts when creating screens.
- Generate clean, responsive and accessible interfaces.
- Prefer reusable components.
- Keep UI logic separated from business logic.
- Follow the project’s chosen UI framework and design system.
- Maintain consistency across pages, forms, buttons, tables, dialogs and layouts.
- Validate forms both on the frontend and backend.
- Never rely only on frontend validation.

---

## API Consumption from Frontend

- Do not expose private API keys in frontend code.
- If the frontend needs third-party data, call the project backend or edge functions.
- Store tokens securely according to the project strategy.
- Use HTTP interceptors or centralized API clients when supported by the framework.
- Handle 401, 403, 404 and 500 responses properly.

---

## Testing

- Prefer automated tests for critical logic.
- Add tests for:
  - Authentication
  - Authorization
  - Input validation
  - Business rules
  - Error handling
  - Security-sensitive flows
- For APIs, include tests for:
  - Missing token
  - Invalid token
  - Expired token
  - Missing permission
  - Invalid input
  - Rate limit behavior

---

## Documentation

- Document important architectural decisions.
- Update README files when adding major features.
- Keep setup instructions clear.
- Include environment variables needed by the project.
- Document API authentication flow when applicable.
- Keep documentation practical and useful.

---

## Forbidden Practices

- Do not expose secrets in code.
- Do not put business logic inside controllers or routes.
- Do not trust frontend-provided security values.
- Do not concatenate user input into SQL.
- Do not skip validation.
- Do not duplicate logic unnecessarily.
- Do not ignore existing project conventions.
- Do not introduce libraries without a clear reason.
- Do not generate code that breaks the current architecture.

# ERP-Lite Rules

## Project Overview

This project is a multi-tenant ERP-lite SaaS system.

The system must support:
- Multiple tenants (companies)
- Isolated data per tenant
- Authentication with JWT
- Role-based and permission-based authorization
- Scalable and maintainable backend architecture

---

## Architecture

- Use Clean Architecture
- Respect strict layer separation:

API → Application → Domain  
Infrastructure → implements external concerns

- Do not violate layer boundaries
- Domain layer must not depend on Infrastructure
- Controllers must not contain business logic

---

## Multi-Tenancy (CRITICAL)

- Every business entity MUST include TenantId
- TenantId must ALWAYS be obtained from the authenticated user (JWT claims)
- Never accept TenantId from frontend requests
- All queries MUST filter by TenantId
- No cross-tenant data access is allowed

If a user attempts to access data from another tenant:
- Return 403 Forbidden or 404 Not Found

---

## Authentication & Authorization

- Use JWT Access Token + Refresh Token
- JWT must include:
  - userId
  - tenantId
  - roles

- All protected endpoints must use [Authorize]

- Use RBAC + permissions

Example permissions:

- customers.read
- customers.create
- customers.update
- customers.delete
- products.read
- products.create
- products.update
- products.delete

- Every endpoint must validate permissions explicitly

---

## Domain Rules

- Domain entities must be clean and independent
- No EF Core attributes in Domain layer
- No infrastructure dependencies in Domain

- Entities must enforce business rules when possible
- Use value objects when applicable

---

## Application Layer

- Use UseCases or Services for business logic
- Do not place logic in controllers
- Use DTOs for all requests/responses
- Never expose Domain entities directly

- Use FluentValidation for input validation

---

## Infrastructure Layer

- Use EF Core for data access
- Use PostgreSQL
- Keep EF configurations separate from entities

- Use parameterized queries or ORM-safe queries
- Add indexes for:
  - TenantId
  - TenantId + IsDeleted

---

## Database Rules

All business tables must include:

- TenantId
- CreatedAt
- CreatedBy
- UpdatedAt
- UpdatedBy
- IsDeleted (soft delete)

---

## Soft Delete

- Do not physically delete records
- Use IsDeleted = true
- All queries must ignore deleted records by default

---

## Auditing

- Track:
  - CreatedAt
  - CreatedBy
  - UpdatedAt
  - UpdatedBy

- Log critical actions such as:
  - User creation
  - Role changes
  - Data modifications
  - Sensitive operations

---

## API Design

- Use RESTful conventions
- Use proper HTTP status codes:

200 OK  
201 Created  
400 Bad Request  
401 Unauthorized  
403 Forbidden  
404 Not Found  

- Controllers must be thin
- Business logic must live in Application layer

---

## Validation

- Validate all incoming data using FluentValidation
- Validate:
  - Required fields
  - String lengths
  - Email formats
  - Numeric ranges

- Never trust frontend validation

---

## Security Rules

- Never expose sensitive data in responses
- Never log tokens, passwords or secrets
- Always validate ownership (TenantId)

- Prevent:
  - SQL Injection (use EF Core safe queries)
  - Broken access control
  - Data leaks across tenants

---

## Performance

- Use pagination in list endpoints
- Avoid loading unnecessary data
- Use async operations
- Add indexes where necessary

---

## API Behavior

All endpoints must:

- Require authentication unless explicitly public
- Extract TenantId from JWT
- Validate permissions
- Filter data by TenantId
- Apply soft delete rules

---

## Testing Expectations

The system must support testing for:

- Authentication (login/register)
- Authorization (roles/permissions)
- Multi-tenant isolation
- CRUD operations
- Validation errors
- Unauthorized access
- Forbidden access
- Soft delete behavior

---

## Forbidden Practices

- Do not expose TenantId in requests
- Do not trust client-provided IDs blindly
- Do not bypass authorization checks
- Do not place business logic in controllers
- Do not directly use DbContext in controllers
- Do not allow cross-tenant queries
- Do not skip validation
- Do not physically delete data

---

## Frontend Integration
- The frontend will be built with Angular
- APIs must be consistent and predictable
- Use DTOs designed for frontend consumption
- Support pagination and filtering
- For UI/UX design and component layout, always consult the Google Stitch MCP server before manually creating interfaces
- Prefer Stitch-generated designs over manually invented layouts

---

## Goal

The system must be production-ready, scalable, secure, and demonstrate professional backend architecture suitable for SaaS applications.