# EduGuard - Student Dropout Analysis & Retention Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/Tests-7%20Passed-success.svg)]()

> **Problem Statement**: High dropout rates in schools hinder educational progress, especially in marginalized and rural communities. **EduGuard** is an Early Warning & Retention System (EWIS) engineered in C# (.NET 10) to proactively identify at-risk students, diagnose root causes (attendance patterns, academic struggles, socioeconomic hardship), and orchestrate targeted interventions before students disengage permanently.

---

## 🏛 Clean Architecture Overview

The solution is structured into four decoupled layers following Clean Architecture and Domain-Driven Design (DDD) principles:

```
EduGuard/
├── EduGuard.slnx
├── src/
│   ├── EduGuard.Domain/            # Entities, Value Objects, Enums, Core Domain Rules
│   ├── EduGuard.Application/       # DTOs, Service Interfaces, CQRS, Risk Engine
│   ├── EduGuard.Infrastructure/    # Persistence, Security, Token Services, External Adapters
│   └── EduGuard.WebApi/            # Controllers, Middleware, Auth Policies, App Entrypoint
├── tests/
│   └── EduGuard.UnitTests/         # Unit and Integration test suite
```

---

## 🚀 Module 1: Authentication & User Management (Implemented)

### Features
- **Authentication**: Secure credential verification with PBKDF2 (SHA-256, 100,000 iterations) + random 128-bit salt, account lockout after 5 consecutive failed attempts, JWT access token generation, and Refresh Token rotation.
- **Roles**: Multi-tier hierarchy (`SuperAdmin`, `DistrictAdmin`, `SchoolPrincipal`, `Teacher`, `Counselor`, `StudentParent`).
- **Permissions**: Claim-based permission enforcement with `[RequirePermission(...)]` filter.
- **Profiles**: Profile management, user activation/deactivation, staff registration.

### Default Seed Account
- **Username**: `admin`
- **Email**: `admin@eduguard.org`
- **Password**: `AdminPassword123!`
- **Role**: `SuperAdmin` (Full system permissions)

### API Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Authenticates user & issues JWT + Refresh token | No |
| `POST` | `/api/v1/auth/refresh-token` | Rotates refresh token & issues new access token | No |
| `POST` | `/api/v1/auth/revoke-token` | Revokes specific refresh token | Yes |
| `POST` | `/api/v1/auth/revoke-all` | Revokes all active sessions for current user | Yes |
| `POST` | `/api/v1/auth/register-staff` | Registers staff member (Teacher/Counselor/Principal) | Yes (`users.manage`) |
| `POST` | `/api/v1/auth/change-password` | Updates user password | Yes |
| `GET` | `/api/v1/roles` | Retrieves all roles and mapped permissions | Yes (`roles.manage`) |
| `GET` | `/api/v1/roles/{roleName}` | Retrieves specific role details | Yes (`roles.manage`) |
| `POST` | `/api/v1/roles` | Creates a custom role | Yes (`roles.manage`) |
| `POST` | `/api/v1/roles/assign` | Assigns a role to a user | Yes (`roles.manage`) |
| `DELETE` | `/api/v1/roles/users/{id}/roles/{role}` | Removes a role from a user | Yes (`roles.manage`) |
| `GET` | `/api/v1/roles/permissions` | Lists all system permissions | Yes (`roles.manage`) |
| `PUT` | `/api/v1/roles/{role}/permissions` | Updates permissions for a role | Yes (`roles.manage`) |
| `GET` | `/api/v1/users/me` | Retrieves profile of currently logged-in user | Yes |
| `PUT` | `/api/v1/users/me` | Updates current user's profile | Yes |
| `GET` | `/api/v1/users/{userId}` | Admin retrieves user by ID | Yes (`users.manage`) |
| `GET` | `/api/v1/users` | Paginated user search with role/school filters | Yes (`users.manage`) |
| `PUT` | `/api/v1/users/{userId}/status` | Activates or deactivates user account | Yes (`users.manage`) |

---

## 🛠 Running the Project

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build Solution
```bash
dotnet build EduGuard.slnx
```

### Run Unit Tests
```bash
dotnet test EduGuard.slnx
```

### Run Web API Server
```bash
dotnet run --project src/EduGuard.WebApi/EduGuard.WebApi.csproj
```
