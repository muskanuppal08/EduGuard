# EduGuard - Student Dropout Analysis & Retention Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/Tests-22%20Passed-success.svg)]()
[![UI Dashboard](https://img.shields.io/badge/Web%20Dashboard-Integrated%20SPA-orange.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)]()

> **Mission**: School dropouts in marginalized and rural communities rarely happen overnight—they are preceded by warning signals across attendance, academic performance, and socioeconomic hardship. **EduGuard** is an Early Warning & Retention System (EWIS) built with **C# (.NET 10)** following **Clean Architecture** principles to proactively detect at-risk students, diagnose root causes, and orchestrate timely interventions.

---

## 📑 Table of Contents
1. [Clean Architecture Overview](#-clean-architecture-overview)
2. [Platform Capabilities (Modules 1 - 4)](#-platform-capabilities)
   - [Module 1: Authentication & RBAC](#-module-1-authentication--user-management)
   - [Module 2: School & Student Management](#-module-2-school--student-management)
   - [Module 3: Attendance Management (Dropout Predictor #1)](#-module-3-attendance-management-critical-dropout-indicator-1)
   - [Module 4: Academic Performance & Learning Loss (Dropout Predictor #2)](#-module-4-academic-performance--learning-loss-monitoring-critical-dropout-indicator-2)
3. [Interactive Web UI Dashboard](#-interactive-web-ui-dashboard)
4. [Developer Experience & VS Code Integration](#-developer-experience--vs-code-integration)
5. [End-to-End API Quickstart (cURL Examples)](#-end-to-end-api-quickstart-curl-examples)
6. [Automated Test Suite (22 Tests Passed)](#-automated-test-suite-22-tests-passed)
7. [Running the Solution](#-running-the-solution)

---

## 🏛 Clean Architecture Overview

EduGuard enforces strict separation of concerns using Clean Architecture and Domain-Driven Design (DDD):

```
EduGuard/
├── EduGuard.slnx                               # Solution file (.NET 10 format)
├── .vscode/
│   ├── launch.json                             # F5 debug configuration for Web API
│   └── tasks.json                              # Build and test background tasks
├── src/
│   ├── EduGuard.Domain/                        # Domain Layer: Pure C# without external dependencies
│   │   ├── Entities/                           # User, Role, School, Student, AttendanceRecord, Assessment, StudentExamMark...
│   │   ├── Enums/                              # AttendanceStatus, AbsencePatternType, AcademicTrajectory, AssessmentCategory...
│   │   └── ValueObjects/                       # Domain primitives and invariants
│   ├── EduGuard.Application/                   # Application Layer: Business logic orchestrations
│   │   ├── Interfaces/                         # IAuthService, IStudentService, IAttendanceService, IAcademicService...
│   │   └── DTOs/                               # Request/Response contracts for all modules
│   ├── EduGuard.Infrastructure/                # Infrastructure Layer: Persistence, Security & Implementation
│   │   ├── Persistence/                        # InMemoryEduGuardDataStore & DataSeeder
│   │   ├── Security/                           # PasswordHasher (PBKDF2 SHA-512) & JwtTokenService (HMAC-SHA256)
│   │   └── Services/                           # Concrete implementations of Application interfaces
│   └── EduGuard.WebApi/                        # Presentation Layer: ASP.NET Core Web API & SPA
│       ├── Controllers/                        # Auth, Roles, Users, Schools, Students, Attendance, Academics
│       ├── Middleware/                         # Global Exception Handler, Logging & Auth filters
│       ├── wwwroot/                            # Single Page Interactive Visual Dashboard (HTML5/CSS3/Vanilla JS)
│       └── EduGuard.WebApi.http                # Interactive REST Client file for VS Code
└── tests/
    └── EduGuard.UnitTests/                     # Unit Test Project (xUnit + FluentAssertions + Moq)
        ├── AuthTests.cs                        # 7 Tests: Hashing, JWT, Lockout, RBAC, Profiles
        ├── SchoolAndStudentTests.cs            # 5 Tests: Schools, Vulnerability, Enrollments, Dropouts
        ├── AttendanceTests.cs                  # 5 Tests: Rosters, %, Chronic Absenteeism, 3-Day Streaks
        └── AcademicTests.cs                    # 5 Tests: Subjects, GPA, Core Failures, Academic Shock
```

---

## 🚀 Platform Capabilities

### 🔐 Module 1: Authentication & User Management

- **Military-Grade Security**:
  - Passwords hashed using **PBKDF2** with SHA-512, 100,000 iterations, and unique 128-bit cryptographically secure salt.
  - Cryptographic **HMAC-SHA256 JWT** access tokens with short lifetimes.
  - Rotatable 64-byte **Refresh Tokens** with client IP/User-Agent tracking and revocation capabilities (`/revoke-token`, `/revoke-all`).
  - Account lockout protection after 5 consecutive failed attempts (15-minute cooldown).
- **Role-Based Access Control (RBAC)**:
  - Default Roles: `SuperAdmin`, `DistrictAdmin`, `SchoolPrincipal`, `Teacher`, `Counselor`, `StudentParent`.
  - Claim-level granular permissions (`schools.read`, `schools.write`, `students.read`, `students.write`, `attendance.record`, `academics.reports`, etc.).
- **User Profiles & Staff Onboarding**:
  - Staff registration, school association, and profile updates.

#### Default Seed Credentials
| Username | Email | Password | Role | Permissions |
|---|---|---|---|---|
| `admin` | `admin@eduguard.org` | `AdminPassword123!` | `SuperAdmin` | Full System Access (All claims) |

#### Module 1 Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Authenticate user & issue JWT + Refresh Token | No |
| `POST` | `/api/v1/auth/refresh-token` | Rotate refresh token & issue new JWT | No |
| `POST` | `/api/v1/auth/revoke-token` | Invalidate a specific refresh token | Yes |
| `POST` | `/api/v1/auth/revoke-all` | Revoke all active sessions for current user | Yes |
| `POST` | `/api/v1/auth/register-staff` | Register staff (Principal, Teacher, Counselor) | Yes (`users.manage`) |
| `GET` | `/api/v1/roles` | List all system roles and mapped permissions | Yes (`roles.manage`) |
| `POST` | `/api/v1/roles/assign` | Assign role to a user | Yes (`roles.manage`) |
| `GET` | `/api/v1/users/profile` | Retrieve authenticated user profile | Yes |
| `PUT` | `/api/v1/users/profile` | Update user profile details | Yes |

---

### 🏫 Module 2: School & Student Management

- **School & Section Hierarchy**:
  - Institutional profiles with UDISE code, District, Zone, Area Type (`Rural`/`Urban`), and Marginalized Area marker (`IsMarginalizedArea`).
  - Class grades (Grade 1–12) and academic year sections (e.g. `10-A`) linked to designated class teachers.
- **Socioeconomic Vulnerability Profiling**:
  - **Below Poverty Line (BPL)** flag (`IsBPL`).
  - **Commute Distance Barrier**: Tracking exact distance (`DistanceToSchoolKm`), flagging students traveling $>5\text{ km}$ on foot.
  - **First-Generation Learner** marker (`IsFirstGenerationLearner`).
  - **Single Parent / Orphan** status indicator (`IsSingleParentOrOrphan`).
  - **Transport Mode**: `Walking`, `Bicycle`, `PublicBus`, `SchoolBus`.
- **Enrollment & Longitudinal Timeline**:
  - Student promotions, section transfers, and re-enrollment transitions.
  - Structured dropout recording with root cause diagnosis (`FinancialHardship`, `ChildLabor`, `EarlyMarriage`, `FamilyMigration`, etc.).
  - Chronological timeline capturing academic, behavioral, and counselor intervention events.

#### Module 2 Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/v1/schools` | Create a new school profile | Yes (`schools.write`) |
| `GET` | `/api/v1/schools` | List schools with area filters | Yes (`schools.read`) |
| `GET` | `/api/v1/schools/{id}` | Retrieve comprehensive school profile | Yes (`schools.read`) |
| `POST` | `/api/v1/classes/grades` | Configure a grade level in a school | Yes (`schools.write`) |
| `POST` | `/api/v1/classes/sections` | Create an active section for an academic year | Yes (`schools.write`) |
| `POST` | `/api/v1/students` | Register student with socioeconomic vulnerability attributes | Yes (`students.write`) |
| `GET` | `/api/v1/students` | Search and filter students by school, section, and BPL | Yes (`students.read`) |
| `GET` | `/api/v1/students/{id}` | Full student demographic & vulnerability profile | Yes (`students.read`) |
| `POST` | `/api/v1/enrollments/enroll` | Enroll a student into a section | Yes (`students.write`) |
| `POST` | `/api/v1/enrollments/promote` | Promote student to next academic grade | Yes (`students.write`) |
| `POST` | `/api/v1/enrollments/dropout` | Record dropout event with diagnosed root cause | Yes (`students.write`) |
| `GET` | `/api/v1/students/{id}/history` | Retrieve full chronological student timeline | Yes (`students.history.read`) |

---

### 📅 Module 3: Attendance Management (Critical Dropout Indicator #1)

Educational research indicates that chronic absenteeism is the earliest and most accurate predictor of eventual school dropout, detectable 1–2 years prior to withdrawal.

- **Daily Bulk Marking**:
  - Statuses: `Present`, `Absent`, `Late`, `HalfDay`, `Excused`.
  - Contextual absence reason coding: `Illness`, `FamilyDomesticChore`, `AgriculturalOrSeasonalLabor`, `ExtremeWeatherOrTransportFailure`, `Unexcused`.
- **Attendance Percentage Engine**:
  $$\text{Attendance Rate} = \frac{\text{Present Days} + 0.5 \times \text{HalfDays}}{\text{Total Working Days}} \times 100$$
  - Tracks cumulative rate, rolling 30-day window rate, and current consecutive absent streaks.
- **Automated Absence Pattern Recognition**:
  - **Chronic Absenteeism Flag**: Automatically flags any student whose cumulative attendance falls below **85%**.
  - **Consecutive Streak Alerter**: Fires actionable counselor alerts when a student misses **$\ge 3$ consecutive unexcused school days**.
- **Counselor Resolution Workflow**:
  - Direct alert resolution logging home visit notes and guardian interventions to the student timeline.

#### Module 3 Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/v1/attendance/roster` | Retrieve daily attendance roster for section marking | Yes (`attendance.read`) |
| `POST` | `/api/v1/attendance/batch` | Bulk record attendance for an entire class section | Yes (`attendance.record`) |
| `PUT` | `/api/v1/attendance/{recordId}` | Update an individual attendance record with absence reasons | Yes (`attendance.record`) |
| `GET` | `/api/v1/attendance/student/{studentId}/summary` | Cumulative attendance statistics and rate % | Yes (`attendance.read`) |
| `GET` | `/api/v1/attendance/student/{studentId}/calendar` | Day-by-day monthly attendance grid | Yes (`attendance.read`) |
| `GET` | `/api/v1/attendance/alerts/chronic-absentees` | List all students with attendance &lt; 85% | Yes (`attendance.read`) |
| `POST` | `/api/v1/attendance/alerts/{alertId}/resolve` | Resolve an absence alert with intervention notes | Yes (`attendance.record`) |

---

### 🎓 Module 4: Academic Performance & Learning Loss Monitoring (Critical Dropout Indicator #2)

Struggling with core subjects and abrupt score drops create frustration and disengagement, directly driving students out of school.

- **Subjects & Assessments Configuration**:
  - Subjects with `IsCoreSubject` distinction (Mathematics, Science, Language).
  - Assessment categories: `UnitTest`, `Midterm`, `Quarterly`, `HalfYearly`, `Annual`, `Assignment`, `Project`.
- **Grading & GPA Engine**:
  - 90%–100%: **A+** (4.0 GPA)
  - 80%–89%: **A** (3.7 GPA)
  - 70%–79%: **B+** (3.3 GPA)
  - 60%–69%: **B** (3.0 GPA)
  - 50%–59%: **C** (2.0 GPA)
  - 35%–49%: **D** (1.0 GPA)
  - &lt;35% or Absent: **F** (0.0 GPA, `IsPass = false`)
  - Exam absence handling (`IsAbsent = true`, marks = 0, automatic failure).
- **Academic Shock Detection**:
  - Automatically flags acute learning collapse: any student experiencing a score drop of **$\ge 15\%$** between consecutive evaluation periods is categorized with `Trajectory = "AcademicShock"`.
- **Core Subject Failure & Academic Risk Tiering**:
  - $\ge 2$ Core Subjects Failed: **Critical** risk
  - $1$ Core Subject Failed: **High** risk
  - Any non-core failed: **Moderate** risk
  - 0 subjects failed: **Low** risk

#### Module 4 Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/academics/subjects` | Register a new academic subject | Yes (`academics.record`) |
| `GET` | `/api/academics/subjects/school/{schoolId}` | List all subjects configured for a school | Yes (`academics.read`) |
| `POST` | `/api/academics/assessments` | Schedule an assessment/examination | Yes (`academics.record`) |
| `GET` | `/api/academics/assessments` | Query scheduled assessments | Yes (`academics.read`) |
| `GET` | `/api/academics/assessments/{id}/roster` | Retrieve grading roster with enrolled students | Yes (`academics.read`) |
| `POST` | `/api/academics/marks/batch` | Bulk record exam marks for a roster | Yes (`academics.record`) |
| `PUT` | `/api/academics/marks/{markId}` | Update an individual mark record | Yes (`academics.record`) |
| `GET` | `/api/academics/analytics/report-card/{studentId}` | Generate full report card with GPA & core failures | Yes (`academics.reports`) |
| `GET` | `/api/academics/analytics/trajectory/{studentId}` | Trajectory analysis and **Academic Shock** detection | Yes (`academics.reports`) |
| `GET` | `/api/academics/analytics/failing/{assessmentId}` | List failing students for an assessment | Yes (`academics.reports`) |
| `GET` | `/api/academics/analytics/at-risk/school/{schoolId}` | Identify all academically vulnerable students in a school | Yes (`academics.reports`) |

---

## 💻 Interactive Web UI Dashboard

EduGuard provides a zero-dependency, responsive Single Page Application directly embedded in the API server (`src/EduGuard.WebApi/wwwroot/index.html`):

1. **Executive Dashboard**: Real-time KPI summary showing total enrollments, chronic absentee counts, students suffering academic shock, and core subject failures.
2. **Student Profiling**: Visual directory highlighting socioeconomic vulnerability flags:
   - 🔴 **BPL** (Below Poverty Line)
   - 🚶 **>5km Commute** (Long-distance walker)
   - 🎓 **First-Gen** (First in family to attend school)
3. **Attendance Register**: Daily roster grid with instant percentage calculation and active streak alert badges.
4. **Academic Analytics & Report Cards**: Visual grade reports showing letter grade, GPA, core subject failure warnings, and learning loss trajectory.

To access: Simply run the application and open **`http://localhost:5065`** in your browser.

---

## 🛠 Developer Experience & VS Code Integration

### 1. F5 Debugging in Visual Studio Code
The repository includes `.vscode/launch.json` and `.vscode/tasks.json`:
- Press **F5** in VS Code to immediately build, launch, and attach the debugger to `EduGuard.WebApi`.
- Automatically opens `http://localhost:5065` in your default browser.

### 2. Interactive REST Client (`EduGuard.WebApi.http`)
Located at `src/EduGuard.WebApi/EduGuard.WebApi.http`, this file enables 1-click execution of requests across all 4 modules directly inside VS Code (using the REST Client extension) or JetBrains Rider.

---

## 📡 End-to-End API Quickstart (cURL Examples)

### 1. Authenticate & Obtain JWT
```bash
curl -s -X POST http://localhost:5065/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"usernameOrEmail": "admin", "password": "AdminPassword123!"}'
```

### 2. Register Student with Socioeconomic Vulnerability Factors
```bash
curl -s -X POST http://localhost:5065/api/v1/students \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Priya",
    "lastName": "Kumari",
    "dateOfBirth": "2010-04-15T00:00:00Z",
    "gender": "Female",
    "schoolId": "<SCHOOL_ID>",
    "isBPL": true,
    "distanceToSchoolKm": 6.5,
    "isFirstGenerationLearner": true,
    "transportMode": "Walking",
    "guardianName": "Ramesh Kumari",
    "guardianRelationship": "Father",
    "guardianOccupation": "Seasonal Agricultural Laborer"
  }'
```

### 3. Record Daily Attendance for a Section
```bash
curl -s -X POST http://localhost:5065/api/v1/attendance/batch \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "sectionId": "<SECTION_ID>",
    "date": "2026-09-20T00:00:00Z",
    "records": [
      {
        "studentId": "<STUDENT_ID>",
        "status": "Absent",
        "absenceReason": "AgriculturalOrSeasonalLabor",
        "remarks": "Helping family during harvest season"
      }
    ]
  }'
```

### 4. Fetch Student Report Card & Academic Shock Analysis
```bash
curl -s -X GET http://localhost:5065/api/academics/analytics/report-card/<STUDENT_ID> \
  -H "Authorization: Bearer <TOKEN>"
```

---

## 🧪 Automated Test Suite (22 Tests Passed)

EduGuard features 100% automated test coverage across all critical business rules in Modules 1–4:

```text
Test run for EduGuard.UnitTests.dll (.NETCoreApp,Version=v10.0)
VSTest version 18.0.2 (arm64)

Passed!  - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 393 ms - EduGuard.UnitTests.dll (net10.0)
```

| Test Class | Tested Functionality | Status |
|---|---|---|
| `AuthTests` | PBKDF2 hashing, JWT issue, lockout after 5 failures, role assignment, user profile update | ✅ 7 Passed |
| `SchoolAndStudentTests` | School creation, student socioeconomic profiling (BPL, >5km, First-Gen), enrollment, promotions, dropout recording, student timeline history | ✅ 5 Passed |
| `AttendanceTests` | Daily rosters, attendance rate % calculation, chronic absenteeism (<85%) detection, 3+ day consecutive absence streak alerts, alert resolution | ✅ 5 Passed |
| `AcademicTests` | Core subject configuration, batch exam mark grading (A+ to F, GPA 0-4.0), core subject failure risk tiering, Academic Shock ($\ge 15\%$ drop) detection | ✅ 5 Passed |

---

## 🏃 Running the Solution

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Clone the Repository
```bash
git clone https://github.com/muskanuppal08/EduGuard.git
cd EduGuard
```

### 2. Build the Solution
```bash
dotnet build EduGuard.slnx
```

### 3. Run the Automated Tests
```bash
dotnet test EduGuard.slnx
```

### 4. Start the Application & Open Dashboard
```bash
dotnet run --project src/EduGuard.WebApi/EduGuard.WebApi.csproj
```
Navigate to **`http://localhost:5065`** in your browser.
