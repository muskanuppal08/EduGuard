# EduGuard - Student Dropout Analysis & Retention Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/Tests-22%20Passed-success.svg)]()
[![UI Dashboard](https://img.shields.io/badge/Web%20Dashboard-Integrated%20SPA-orange.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)]()

> **Mission**: School dropouts in marginalized and rural communities rarely happen overnight—they are preceded by warning signals across attendance, academic performance, commute barriers, and socioeconomic hardship. **EduGuard** is an Early Warning & Retention System (EWIS) built with **C# (.NET 10)** following **Clean Architecture** principles to proactively detect at-risk students, diagnose root causes, and orchestrate timely interventions before a student drops out.

---

## 📑 Table of Contents
1. [Clean Architecture Overview](#-clean-architecture-overview)
2. [Core Platform Capabilities](#-core-platform-capabilities)
   - [Authentication & Access Control (PBKDF2 & JWT)](#1-authentication--access-control)
   - [Student Socioeconomic Vulnerability Profiling](#2-student-socioeconomic-vulnerability-profiling)
   - [Attendance Tracking & Chronic Absence Engine](#3-attendance-tracking--chronic-absence-engine)
   - [Academic Performance & Learning Shock Detection](#4-academic-performance--learning-shock-detection)
   - [Student Welfare Helpdesk & Grievance Triage](#5-student-welfare-helpdesk--grievance-triage)
3. [Interactive Web UI & Navigation](#-interactive-web-ui--navigation)
4. [Role Personas & 1-Click Test Accounts](#-role-personas--1-click-test-accounts)
5. [Step-by-Step User Guides](#-step-by-step-user-guides)
   - [How a Student Logs a Grievance](#how-a-student-logs-a-grievance)
   - [How Super Admin / Counselor Logs on Behalf of Walk-In Students](#how-super-admin--counselor-logs-on-behalf-of-walk-in-students)
   - [How to Review, Action, and Resolve Grievances](#how-to-review-action-and-resolve-grievances)
   - [How New Users Self-Register](#how-new-users-self-register)
6. [Developer Experience & VS Code Integration](#-developer-experience--vs-code-integration)
7. [End-to-End API Quickstart (cURL Examples)](#-end-to-end-api-quickstart-curl-examples)
8. [Automated Test Suite (22 Tests Passed)](#-automated-test-suite-22-tests-passed)
9. [Running the Solution](#-running-the-solution)

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
│       ├── wwwroot/                            # Single Page Interactive Visual Dashboard (HTML5/Tailwind/JS)
│       └── EduGuard.WebApi.http                # Interactive REST Client file for VS Code
└── tests/
    └── EduGuard.UnitTests/                     # Unit Test Project (xUnit + FluentAssertions + Moq)
        ├── AuthTests.cs                        # 7 Tests: Hashing, JWT, Lockout, RBAC, Profiles
        ├── SchoolAndStudentTests.cs            # 5 Tests: Schools, Vulnerability, Enrollments, Dropouts
        ├── AttendanceTests.cs                  # 5 Tests: Rosters, %, Chronic Absenteeism, 3-Day Streaks
        └── AcademicTests.cs                    # 5 Tests: Subjects, GPA, Core Failures, Academic Shock
```

---

## 🚀 Core Platform Capabilities

### 1. Authentication & Access Control

- **Cryptographic Security**:
  - Passwords hashed using **PBKDF2** with SHA-512, 100,000 iterations, and unique 128-bit cryptographically secure salt.
  - Cryptographic **HMAC-SHA256 JWT** access tokens with custom claims.
  - Rotatable 64-byte **Refresh Tokens** with client IP tracking and revocation endpoints (`/revoke-token`, `/revoke-all`).
  - Account lockout protection after 5 consecutive failed attempts (15-minute cooldown).
- **Public Self-Registration**:
  - Open self-registration (`POST /api/v1/auth/register`) allowing any new student, parent, or educator to create an account and immediately receive access credentials.
- **Role-Based Access Control (RBAC)**:
  - Default Roles: `SuperAdmin`, `DistrictAdmin`, `SchoolPrincipal`, `Teacher`, `Counselor`, `StudentParent`.
  - Claim-level granular permissions (`schools.read`, `students.read`, `students.write`, `attendance.record`, `academics.reports`, etc.).

#### Auth Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Self-register a new account (Public) | No |
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

### 2. Student Socioeconomic Vulnerability Profiling

- **Demographic & Infrastructure Markers**:
  - School profile with UDISE code, District, Zone, Area Type (`Rural`/`Urban`), and Marginalized Area marker (`IsMarginalizedArea`).
  - Grade levels (Grade 1–12) and active academic sections (e.g. `9-A`).
- **Socioeconomic Risk Indicators**:
  - **Below Poverty Line (BPL)** flag (`IsBPL`).
  - **Commute Distance Barrier**: Capturing exact distance (`DistanceToSchoolKm`), highlighting students traveling $>5\text{ km}$ on foot through unpaved roads.
  - **First-Generation Learner** marker (`IsFirstGenerationLearner`).
  - **Single Parent / Orphan** status (`IsSingleParentOrOrphan`).
  - **Transport Mode**: `Walking`, `Bicycle`, `PublicBus`, `SchoolBus`.
- **Longitudinal Timeline**:
  - Chronological history tracking promotions, dropout alerts, counselor visits, and welfare disbursements.

#### Student Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/v1/schools` | Create a new school profile | Yes (`schools.write`) |
| `GET` | `/api/v1/schools` | List schools with area filters | Yes (`schools.read`) |
| `POST` | `/api/v1/students` | Register student with socioeconomic vulnerability factors | Yes (`students.write`) |
| `GET` | `/api/v1/students` | Search and filter students by school, section, and BPL | Yes (`students.read`) |
| `GET` | `/api/v1/students/{id}` | Full student demographic & vulnerability profile | Yes (`students.read`) |
| `GET` | `/api/v1/students/{id}/history` | Retrieve chronological student timeline | Yes (`students.history.read`) |

---

### 3. Attendance Tracking & Chronic Absence Engine

- **Daily Roll-Call Marking**:
  - Statuses: `Present`, `Absent`, `Late`, `HalfDay`, `Excused`.
  - Contextual absence reason coding: `Illness`, `FamilyDomesticChore`, `AgriculturalOrSeasonalLabor`, `ExtremeWeatherOrTransportFailure`, `Unexcused`.
- **Attendance Percentage Engine**:
  $$\text{Attendance Rate} = \frac{\text{Present Days} + 0.5 \times \text{HalfDays}}{\text{Total Working Days}} \times 100$$
- **Pattern Recognition**:
  - **Chronic Absenteeism Flag**: Automatically alerts when cumulative attendance drops below **85%**.
  - **Consecutive Streak Alerter**: Triggers high-urgency notifications when a student misses **$\ge 3$ consecutive unexcused days**.

#### Attendance Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/v1/attendance/roster` | Retrieve daily attendance roster for section marking | Yes (`attendance.read`) |
| `POST` | `/api/v1/attendance/batch` | Bulk record attendance for an entire class section | Yes (`attendance.record`) |
| `GET` | `/api/v1/attendance/student/{studentId}/summary` | Cumulative attendance statistics and rate % | Yes (`attendance.read`) |
| `GET` | `/api/v1/attendance/alerts/chronic-absentees` | List all students with attendance &lt; 85% | Yes (`attendance.read`) |
| `POST` | `/api/v1/attendance/alerts/{alertId}/resolve` | Resolve absence alert with intervention notes | Yes (`attendance.record`) |

---

### 4. Academic Performance & Learning Shock Detection

- **Subjects & Assessment Grading**:
  - Distinction for Core Subjects (Mathematics, Science, Language).
  - Assessment categories: `UnitTest`, `Midterm`, `Quarterly`, `HalfYearly`, `Annual`.
  - Grading & GPA Engine: A+ (4.0), A (3.7), B+ (3.3), B (3.0), C (2.0), D (1.0), F (0.0).
- **Academic Shock Alerter**:
  - Detects abrupt score collapse: flags any student experiencing a score drop of **$\ge 15\%$** between consecutive evaluations.
- **Core Subject Failure Risk**:
  - $\ge 2$ Core Subjects Failed: **Critical** risk
  - $1$ Core Subject Failed: **High** risk
  - Any non-core failed: **Moderate** risk

#### Academic Endpoints
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/academics/subjects` | Register a new academic subject | Yes (`academics.record`) |
| `POST` | `/api/academics/assessments` | Schedule an assessment/examination | Yes (`academics.record`) |
| `POST` | `/api/academics/marks/batch` | Bulk record exam marks for a roster | Yes (`academics.record`) |
| `GET` | `/api/academics/analytics/report-card/{studentId}` | Generate report card with GPA & core failures | Yes (`academics.reports`) |
| `GET` | `/api/academics/analytics/trajectory/{studentId}` | Trajectory analysis and **Academic Shock** detection | Yes (`academics.reports`) |

---

### 5. Student Welfare Helpdesk & Grievance Triage

- **Multi-Channel Intake**:
  - Self-service submission for enrolled students through their student portal.
  - Walk-in / unregistered student logging by Super Admins, Principals, Counselors, and Teachers.
- **Categorized Issue Types**:
  - 🚌 **Commute Hardship**: Impassable roads, lack of transport, distance $>5\text{ km}$.
  - 💰 **Financial Distress**: Inability to pay exam fees, lack of books, uniform, or BPL support.
  - 🏥 **Health & Medical**: Prolonged illness, domestic chore pressure, nutritional deficiency.
  - 📚 **Academic Remedial**: Requests for after-school tutoring in core subjects.
  - ⚠️ **Safety & Wellbeing**: Harassment, bullying, or unsafe transit routes.
- **Action & Resolution Workflow**:
  - Counselors record intervention notes (e.g. bicycle grant, fee waiver, remedial tutor assignment) and mark cases resolved.

---

## 💻 Interactive Web UI & Navigation

The platform features an integrated Single Page Application accessible directly at `http://localhost:5065`.

The navigation bar offers intuitive, role-aware tabs:

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│  EG EduGuard v1.0.0    Govt High School, Birmitrapur   [ 📝 Log Request ]  [ 👤 Switch Role ]    │
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│  [📊 Executive Overview] [👥 Students & Vulnerability] [📅 Attendance & Alerts]                 │
│  [🎓 Academics & Performance] [📢 Helpdesk & Requests]                                           │
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

1. **📊 Executive Overview**: High-level KPI summary of monitored students, chronic absenteeism rate, academic shock alerts, and platform pipeline health.
2. **👥 Students & Vulnerability**: Searchable directory displaying hardship markers (BPL status, commute distance, first-gen learner status) with 1-click modal case file summaries.
3. **📅 Attendance & Alerts**: Daily class rosters, real-time percentage calculator, and consecutive absence streak monitors.
4. **🎓 Academics & Performance**: Comprehensive report cards, core subject failure alerts, and academic trajectory trackers.
5. **📢 Helpdesk & Requests**: Dynamic welfare hub:
   - For **Students**: View attendance summary, academic report card, and personal grievance history.
   - For **Staff / Admins**: Administrative triage queue displaying all requests, urgency flags, status badges, and 1-click resolution actions.

---

## 👥 Role Personas & 1-Click Test Accounts

The login modal contains 4 pre-seeded personas for immediate 1-click testing:

| Role | Username | Password | Purpose & Capabilities |
|---|---|---|---|
| **SuperAdmin** | `admin` | `AdminPassword123!` | System-wide oversight, triage review, walk-in request logging, settings |
| **Teacher** | `teacher` | `TeacherPassword123!` | Daily section attendance roll-call, exam marks entry |
| **Counselor** | `counselor` | `CounselorPassword123!` | Student case files, chronic absence triage, grievance resolution |
| **StudentParent** | `student` | `StudentPassword123!` | Push grievances, view own attendance summary & report card |

---

## 📖 Step-by-Step User Guides

### How a Student Logs a Grievance
1. Log in as a student (or use the 1-click `student` persona).
2. Click the green **`[ 📝 Log Request / Grievance ]`** button in the header, or visit the **`📢 Helpdesk & Requests`** tab.
3. Select your issue category (e.g., *Commute Hardship* or *Financial Aid*).
4. Set urgency (*Standard* or *High*), describe your hardship, and click **`Push Complaint to Counselor`**.
5. The request is immediately logged to your student longitudinal timeline.

### How Super Admin / Counselor Logs on Behalf of Walk-In Students
1. Log in as `admin` or `counselor`.
2. Go to **`📢 Helpdesk & Requests`** and click **`[ ➕ Log Request for Walk-In Student ]`** (or click **`[ 📝 Log Request / Grievance ]`** in the top bar).
3. In the student selector dropdown:
   - Choose an enrolled student (*Priya Kumari*, *Sunita Kumari*), **OR**
   - Select **`+ Walk-In / Unregistered Student`** to type any walk-in student's full name and class.
4. Fill in the category, urgency, and details, then click **`Submit Request`**.

### How to Review, Action, and Resolve Grievances
1. While logged in as `admin` or `counselor`, open the **`📢 Helpdesk & Requests`** tab.
2. The **Administrative Grievance Review Queue** displays all open cases.
3. Filter by **All**, **Under Review**, **Urgent**, or **Resolved**.
4. Click **`[ 🛠️ Action / Resolve ]`** next to any request.
5. Enter resolution notes (e.g., *"Provided bicycle from welfare grant; attendance restored."*) and update status to **`Resolved`**.

### How New Users Self-Register
1. In the top-right header, click **`[ 👤 Switch Role / Login ]`**.
2. Click the **`✍️ Register New User`** tab.
3. Fill in **Full Name**, **Username**, **Email**, **Password** (min 8 chars, 1 uppercase, 1 digit), and choose your **Role** (`StudentParent`, `Teacher`, `Counselor`, `SuperAdmin`).
4. Click **`Create Account & Log In Automatically`**.
5. The backend validates and hashes your credentials, issues a JWT token, and logs you into your personalized dashboard.

---

## 🛠 Developer Experience & VS Code Integration

### 1. F5 Debugging in Visual Studio Code
The repository includes `.vscode/launch.json` and `.vscode/tasks.json`:
- Press **F5** in VS Code to build, run, and attach the debugger to `EduGuard.WebApi`.
- Automatically opens `http://localhost:5065` in your default browser.

### 2. Interactive REST Client (`EduGuard.WebApi.http`)
Located at `src/EduGuard.WebApi/EduGuard.WebApi.http`, this file enables 1-click execution of requests across all modules directly inside VS Code or JetBrains Rider.

---

## 📡 End-to-End API Quickstart (cURL Examples)

### 1. Self-Register a New User Account (Public)
```bash
curl -s -X POST http://localhost:5065/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Meera Sharma",
    "username": "meera",
    "email": "meera@example.com",
    "password": "Password123!",
    "role": "StudentParent"
  }'
```

### 2. Authenticate & Obtain JWT
```bash
curl -s -X POST http://localhost:5065/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"usernameOrEmail": "admin", "password": "AdminPassword123!"}'
```

### 3. Register Student with Socioeconomic Vulnerability Factors
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

### 4. Record Daily Attendance for a Section
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

### 5. Fetch Student Report Card & Academic Shock Analysis
```bash
curl -s -X GET http://localhost:5065/api/academics/analytics/report-card/<STUDENT_ID> \
  -H "Authorization: Bearer <TOKEN>"
```

---

## 🧪 Automated Test Suite (22 Tests Passed)

EduGuard features 100% automated test coverage across all business rules:

```text
Passed!  - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 1 s - EduGuard.UnitTests.dll (net10.0)
```

| Test Class | Tested Functionality | Status |
|---|---|---|
| `AuthTests` | PBKDF2 hashing, JWT issuance, lockout after 5 failures, role assignment, user profile update | ✅ 7 Passed |
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
dotnet run --project src/EduGuard.WebApi/EduGuard.WebApi.csproj --urls "http://localhost:5065"
```
Navigate to **`http://localhost:5065`** in your browser.
