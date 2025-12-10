This file defines mandatory coding rules for the Product Data Management Web API built with .NET 8, CQRS, multi-tenancy and the Repository pattern. All generated or modified code must strictly follow these rules.

The project structure is fixed and consists of Business (DTOs with *Web suffix, interfaces, helpers, services, exceptions), CQRS (Commands, Queries, Handlers, Validators), Entities (domain entities, DbContext, EF Core configuration), Repositories (Repository pattern) and WebApi (controllers, middleware, authorization, extensions).

The system is strictly multi-tenant. Every tenant-related entity must have a TenantId of type Guid. Every database query must always filter by TenantId and by !IsDeleted if the entity supports soft delete. Every Command and Query Handler must validate tenant isolation using ICurrentUser. Accessing or modifying data belonging to another tenant is forbidden and must throw ForbiddenApiException with an English message.

CQRS separation is mandatory. Commands must implement IRequestCommand<TResponse>, Queries must implement IRequestQuery<TResponse>. Direct usage of IRequest<T> from MediatR is not allowed. Commands and Queries must be declared as immutable records using init-only properties and follow the {Feature}{Action}Command or {Feature}{Action}Query naming convention.

Commands are responsible only for state changes. They may read data from the database solely for business validation or for loading entities that will be modified. Commands return only Unit, Guid or a simple result object and must never build complex DTO projections. SaveChangesAsync is handled automatically by TransactionBehavior and must not be called explicitly, except when a database-generated Id is required for a foreign key in a subsequent operation. SaveChangesAsync must never be called in Queries.

Queries are read-only. They must not modify state and must not call SaveChangesAsync. Queries return API-facing DTOs only, always using the *Web suffix. Mapping from entities to DTOs is performed inside Query Handlers.

CQRS code is organized per feature and action. Each action has its own Command or Query, Handler and Validator, placed in a dedicated folder and named consistently.

FluentValidation Validators are responsible for validating request structure, required fields, formats, lengths and other simple rules that do not depend on entity state. If database access is used only to verify that an entity exists, this check must be implemented in the Validator using MustAsync. Handlers contain business rules requiring entity properties, navigation properties, authorization checks and tenant isolation.

Only custom API exceptions are allowed in handlers. These include ValidationApiException, NotFoundApiException, UnauthorizedApiException, ForbiddenApiException and ConflictApiException. Exception messages must always be written in English. Throwing generic Exception or standard .NET exceptions is forbidden. ApiExceptionMiddleware maps these exceptions to HTTP responses.

Authorization is policy-based only. Role-based Authorize attributes are not allowed. Controllers apply policies such as TenantMember, TenantAdmin, ProjectMember or SystemAdmin. Handlers must always revalidate tenant isolation even if a policy is applied at controller level.

Controllers must remain thin. They handle routing, authorization and model binding only. All business logic belongs in Handlers. Route parameters such as tenantId and projectId must be injected into Commands and Queries using immutable with expressions. Routing follows REST conventions like api/tenants/{tenantId}/projects/{projectId}/[controller]. Public endpoints must include XML documentation comments.

All database access must go through repositories. Navigation properties must be explicitly loaded using Include. Soft delete must be implemented using IsDeleted and DeletedAt instead of physical deletes where applicable, and all queries must filter out soft-deleted entities.

All I/O operations must be asynchronous. CancellationToken must be passed through all layers. Blocking calls such as .Result, .Wait() or .GetAwaiter().GetResult() are forbidden. Mixing synchronous and asynchronous code is not allowed.

Structured logging must be used consistently. Log important state changes, critical operations, warnings and errors. Do not log sensitive data, credentials, tokens or complete domain objects. Always use parameterized logging instead of string interpolation.

Domain entities must inherit from BaseEntity with an automatically generated Guid Id. Manual creation of entity Guid values is not allowed. DateTime.UtcNow must be used consistently instead of DateTime.Now. Navigation collections must be initialized. Nullable reference types must be used correctly, and null checks must be performed unless guaranteed by validation.

ICurrentUser is the source of authentication and authorization context. Handlers must validate that request TenantId matches currentUser.ActiveTenantId and use currentUser.Id for audit fields. ICurrentUser is scoped per request and may cache database data.

Comments must be minimal and explain only non-obvious decisions or business rules. Code should be self-explanatory wherever possible. Shared logic must be extracted into helpers, domain services or reusable validators to avoid duplication.

Before generating or modifying any code, always ensure correct CQRS separation, enforced multi-tenancy, correct exception usage, clear Validator vs Handler responsibilities, full async compliance, thin controllers and DTO-based API output.
