# CreditSystem API

CreditSystem is a .NET 8 Web API that manages user credits and task execution, where credits are consumed only when tasks are executed.

---

## Table of Contents

- [1. How to Run the API](#1-how-to-run-the-api)
- [2. Available Endpoints and Usage](#2-available-endpoints-and-usage)
- [3. Assumptions and Design Decisions](#3-assumptions-and-design-decisions)
- [4. Key Trade-offs and Why](#4-key-trade-offs-and-why)

---

## 1. How to Run the API

### Prerequisites

- Docker Desktop (or Docker Engine + Docker Compose plugin)

### Start with Docker

```bash
docker compose up --build
```

### URLs

- API base URL: `http://localhost:5080`
- Swagger UI: `http://localhost:5080/swagger`

### Stop

```bash
docker compose down
```

### Stop and remove DB data volume

```bash
docker compose down -v
```

---

## 2. Available Endpoints and Usage

> All task and user endpoints require `Authorization: <jwt_token>`.

### Authentication

#### `POST /api/auth/register`

Registers a new user.

Request body:

```json
{
	"email": "user@example.com",
	"username": "myuser",
	"password": "Passw0rd!"
}
```

Response (`201 Created`):

```json
{
	"token": "<jwt>",
	"userId": "GUID",
	"email": "user@example.com",
	"username": "myuser",
	"credits": 500
}
```

#### `POST /api/auth/login`

Logs in existing user.

Request body:

```json
{
	"email": "user@example.com",
	"password": "Passw0rd!"
}
```

Response (`200 OK`): same shape as register.

### User Profile

#### `GET /api/users/me`

Returns current authenticated user details and current credit balance.

Response (`200 OK`):

```json
{
	"id": "GUID",
	"email": "user@example.com",
	"username": "myuser",
	"credits": 487,
	"registeredAt": "2026-02-16T12:00:00Z"
}
```

### Tasks

#### `POST /api/tasks`

Creates a task for current user (free operation).

Response (`201 Created`):

```json
{
	"id": "GUID",
	"status": "Created",
	"createdAt": "2026-02-16T12:05:00Z"
}
```

#### `GET /api/tasks`

Returns current user's tasks.

Response (`200 OK`):

```json
[
	{
		"id": "GUID",
		"status": "Succeeded",
		"cost": 12,
		"createdAt": "2026-02-16T12:05:00Z",
		"startedAt": "2026-02-16T12:05:01Z",
		"completedAt": "2026-02-16T12:05:21Z"
	}
]
```

#### `POST /api/tasks/{id}/execute`

Triggers task execution.

Possible behavior:

- If status is `Created`, cost is calculated randomly (`1..15`) and credits are deducted once.
- If insufficient credits, task becomes `Rejected`.
- If already executed (or already in progress), endpoint is idempotent and returns current task data.

Example response (`200 OK`):

```json
{
	"id": "GUID",
	"status": "Running",
	"cost": 7,
	"startedAt": "2026-02-16T12:06:00Z",
	"message": "Task execution started."
}
```

---

## 3. Project Design Decisions

### Key Assumptions
1. **Task execution API is asynchronous from client perspective**: endpoint returns quickly with `Running`, completion happens in background.
2. **User isolation is strict**: users can only read/execute their own tasks and own balance.
3. JWT-based authentication is used for stateless API security.
4. EF Core migrations are applied automatically on startup.
5. Swagger is enabled in development for quick API inspection/testing.

### Project Structure Design

The solution follows a layered architecture for clear separation of concerns:

```text
src/
├── CreditSystem.Api            # HTTP layer: controllers, auth pipeline, swagger, DI bootstrap
├── CreditSystem.Application    # Use-cases/contracts: DTOs, interfaces, service abstractions
├── CreditSystem.Domain         # Core domain model: entities, enums, business state
└── CreditSystem.Infrastructure # Data + external concerns: EF Core, repositories, hosted services
```
---

## 4. Key Trade-offs and Why

This section focuses on choices made in `TaskExecutionService` and `AutoCreditGrantBackgroundService` according to requirements.

### A) `TaskExecutionService` trade-offs

#### 1. Immediate charge + asynchronous completion

- **What was chosen:** Credits are deducted inside a DB transaction before background execution starts.
- **Why:** Ensures credits are consumed exactly once when execution is accepted, and failed tasks are not refunded (except insufficient funds case where no deduction occurs).
- **Trade-off:** Users can be charged even when final status becomes `Failed`; this intentionally matches requirement.

#### 2. Idempotent execute endpoint via status gating

- **What was chosen:** Only tasks in `Created` state can transition to charged/running flow.
- **Why:** Repeated calls return existing state and prevent double-deduction.
- **Trade-off:** Simpler idempotency (state-based) over explicit idempotency keys.

#### 3. Concurrency safety prioritized over minimal locking

- **What was chosen:** Execution-critical state changes and deduction are transaction-scoped.
- **Why:** Protects against parallel execute requests corrupting credits or task state.
- **Trade-off:** Slightly more transaction overhead under high contention.

#### 4. Randomized simulation for cost/duration/failure

- **What was chosen:**
	- Cost: random `1..15`
	- Duration: random `10..40` seconds
	- Outcome: random success/failure
- **Why:** Directly implements specification for simulation.
- **Trade-off:** Non-deterministic behavior makes exact test replay harder without controlled RNG seeding.

### B) `AutoCreditGrantBackgroundService` trade-offs

#### 1. Background polling with durable state in DB

- **What was chosen:** Hosted service periodically checks due users by `LastCreditGrantAt` (or `RegisteredAt` fallback).
- **Why:** Works across restarts because due state is persisted in database, satisfying reliability requirement.
- **Trade-off:** Polling introduces a bounded delay (up to check interval) before grant is applied.

#### 2. Serializable transaction for grant cycles

- **What was chosen:** Grant processing runs in `Serializable` transaction and writes both user balance and credit-transaction records.
- **Why:** Prioritizes consistency and avoids double-grants during concurrent processing windows.
- **Trade-off:** Higher isolation level can reduce throughput, but this path is periodic and correctness-critical.

#### 3. Config-driven cadence and amount

- **What was chosen:** Amount/frequency/check interval come from `AutoGrant` options in config.
- **Why:** Easier tuning without code changes.
- **Trade-off:** Misconfiguration risk if values are set incorrectly in environment files.

---