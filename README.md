# EduGuard - Student Dropout Analysis & Retention Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/Tests-12%20Passed-success.svg)]()

> **Problem Statement**: High dropout rates in schools hinder educational progress, especially in marginalized and rural communities. **EduGuard** is an Early Warning & Retention System (EWIS) engineered in C# (.NET 10) to proactively identify at-risk students, diagnose root causes (attendance patterns, academic struggles, socioeconomic hardship), and orchestrate targeted interventions before students disengage permanently.

---

## 🏛 Clean Architecture Overview

The solution is structured into four decoupled layers following Clean Architecture and Domain-Driven Design (DDD) principles:

```
EduGuard/
├── EduGuard.slnx
├── src/
│   ├── EduGuard.Domain/            # Entities, Value Objects, Enums, Core Domain Rules
│   ├── EduGuard.Application/       # DTOs, Service Interfaces, CQRS, Risk Engine Contracts
│   ├── EduGuard.Infrastructure/    # Persistence, Security, Password Hashing, Token Services
│   └── EduGuard.WebApi/            # Controllers, Middleware, Auth Policies, App Entrypoint
├── tests/
│   └── EduGuard.UnitTests/         # Automated Test Suite (12 Tests Passed)
```

---

## 🚀 Module 1: Authentication & User Management

### Core Capabilities
- **Authentication**: Secure credential verification with PBKDF2 (SHA-256, 100,000 iterations) + random 128-bit salt, account lockout after 5 consecutive failed attempts, JWT access token generation, and Refresh Token rotation.
- **Role Hierarchy**: Multi-tier hierarchy (`SuperAdmin`, `DistrictAdmin`, `SchoolPrincipal`, `Teacher`, `Counselor`, `StudentParent`).
- **Permissions**: Claim-based permission enforcement using custom `[RequirePermission(...)]` filter attribute.
- **Profiles**: Profile management, user activation/deactivation, staff onboarding.

### Default Seed Account
- **Username**: `admin`
- **Email**: `admin@eduguard.org`
- **Password**: `AdminPassword123!`
- **Role**: `SuperAdmin` (Full system permissions)

### Key Endpoints (Module 1)
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Authenticates user & issues JWT + Refresh token | No |
| `POST` | `/api/v1/auth/refresh-token` | Rotates refresh token & issues new access token | No |
| `POST` | `/api/v1/auth/revoke-token` | Revokes specific refresh token | Yes |
| `POST` | `/api/v1/auth/revoke-all` | Revokes all active sessions for current user | Yes |
| `POST` | `/api/v1/auth/register-staff` | Registers staff member (Teacher/Counselor/Principal) | Yes (`users.manage`) |
| `GET` | `/api/v1/roles` | Retrieves all roles and mapped permissions | Yes (`roles.manage`) |
| `POST` | `/api/v1/roles/assign` | Assigns a role to a user | Yes (`roles.manage`) |
| `GET` | `/api/v1/users/profile` | Retrieves profile of currently logged-in user | Yes |
| `PUT` | `/api/v1/users/profile` | Updates current user's profile | Yes |

---

## 🏫 Module 2: School & Student Management

### Core Capabilities
- **School Management**:
  - School profiling with UDISE/School Code, District, Block/Zone.
  - Urban/Rural area classification (`AreaType.Rural`, `AreaType.Urban`, `AreaType.SemiUrban`).
  - Marginalized area marker (`IsMarginalizedArea`) to focus retention resources on tribal and disadvantaged areas.
- **Class & Section Organization**:
  - Multi-year academic calendar management (`AcademicYear`).
  - Grade levels (Grade 1 through 12) and sections (e.g. `9-A`) linked with class teachers.
- **Student Demographic & Socioeconomic Vulnerability Profiling**:
  - **Below Poverty Line (BPL)** flag (`IsBPL`).
  - **Commute Distance Hardship**: Exact distance in kilometers (`DistanceToSchoolKm`), flagging students traveling $>5\text{ km}$ on foot.
  - **First-Generation Learner** marker (`IsFirstGenerationLearner`).
  - **Single Parent / Orphan** status indicator (`IsSingleParentOrOrphan`).
  - **Mode of Transport**: Walking, Bicycle, Public Bus, School Bus.
  - **Guardian Details**: Name, relationship, emergency phone, and occupation (e.g. seasonal daily wage labor).
- **Enrollment & Academic Journey**:
  - Year-over-year enrollment tracking (`Active`, `Promoted`, `Repeater`, `DroppedOut`, `Transferred`).
  - Student promotion workflows across academic years.
  - Dropout diagnosis recording exit dates and primary root causes (`FinancialHardship`, `ChildLabor`, `EarlyMarriage`, `FamilyMigration`, etc.).
- **Longitudinal History & Audit Trail**:
  - Chronological timeline recording promotions, section changes, leaves of absence, and counselor intervention notes.

### Key Endpoints (Module 2)
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/v1/schools` | Creates a new school profile | Yes (`schools.write`) |
| `GET` | `/api/v1/schools` | Lists schools with area and marginalized filters | Yes (`schools.read`) |
| `GET` | `/api/v1/schools/{id}` | Retrieves detailed school profile | Yes (`schools.read`) |
| `POST` | `/api/v1/classes/grades` | Configures a grade level within a school | Yes (`schools.write`) |
| `POST` | `/api/v1/classes/sections` | Creates a class section linked to an academic year | Yes (`schools.write`) |
| `POST` | `/api/v1/students` | Registers student with socioeconomic vulnerability factors | Yes (`students.write`) |
| `GET` | `/api/v1/students` | Search and filter students by school, section, and BPL status | Yes (`students.read`) |
| `GET` | `/api/v1/students/{id}` | Comprehensive student profile with vulnerability markers | Yes (`students.read`) |
| `GET` | `/api/v1/students/admission/{admNo}` | Retrieves student by unique admission number | Yes (`students.read`) |
| `POST` | `/api/v1/enrollments/enroll` | Enrolls a student into a section for an academic year | Yes (`students.write`) |
| `POST` | `/api/v1/enrollments/promote` | Promotes student to the next academic grade | Yes (`students.write`) |
| `POST` | `/api/v1/enrollments/dropout` | Records student dropout with root cause diagnosis | Yes (`students.write`) |
| `GET` | `/api/v1/students/{id}/history` | Retrieves full chronological timeline of student transitions | Yes (`students.history.read`) |

---

## 🧪 Automated Test Suite (12 Tests Passed)

```text
Test run for EduGuard.UnitTests.dll (.NETCoreApp,Version=v10.0)
VSTest version 18.0.2 (arm64)

Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12, Duration: 893 ms - EduGuard.UnitTests.dll (net10.0)
```

### Verified Test Cases
- **Module 1 (Auth & Users)**:
  1. `PasswordHasher_ShouldHashAndVerifySuccessfully`
  2. `AuthService_Login_WithValidCredentials_ShouldSucceed`
  3. `AuthService_Login_WithInvalidPassword_ShouldFail`
  4. `AuthService_RegisterStaff_ShouldCreateStaffWithRole`
  5. `RoleService_AssignAndRemoveRole_ShouldUpdatePermissions`
  6. `UserProfileService_UpdateProfile_ShouldUpdateFields`
  7. `AuthService_FailedAttempts_ShouldTriggerLockout`
- **Module 2 (School & Students)**:
  8. `SchoolService_CreateAndRetrieveSchool_ShouldSucceed`
  9. `StudentService_RegisterStudent_WithSocioeconomicVulnerability_ShouldSucceed`
  10. `EnrollmentService_EnrollAndPromoteStudent_ShouldUpdateClassAndHistory`
  11. `EnrollmentService_RecordDropout_And_ReEnroll_ShouldTransitionProperly`
  12. `StudentHistoryService_AddTimelineNote_ShouldAppendToHistory`

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
The server will start listening at: `http://localhost:5065`
