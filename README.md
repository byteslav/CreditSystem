# CreditSystem

## Run with Docker

### Prerequisites
- Docker Desktop (or Docker Engine + Docker Compose plugin)

### Start
```bash
docker compose up --build
```

### API URL
- Base URL: `http://localhost:5080`
- Swagger UI (Development mode): `http://localhost:5080/swagger`

### Stop
```bash
docker compose down
```

### Stop and remove database data
```bash
docker compose down -v
```