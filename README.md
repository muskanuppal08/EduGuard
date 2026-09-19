# EduGuard - Student Dropout Analysis & Retention Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/Tests-22%20Passed-success.svg)]()

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
│   └── EduGuard.WebApi/            # REST Controllers, Middleware, Web UI Dashboard (wwwroot)
├── tests/
│   └── EduGuard.UnitTests/         # Automated Test Suite (22 Unit Tests Passed)
```

---

## 🚀 Module 1: Authentication & User Management

### Features
- **Authentication**: Secure credential verification with PBKDF2 (SHA-256, 100,000 iterations) + random 128-bit salt, account lockout after 5 consecutive failed attempts, HMAC-SHA256 JWT access token generation, and rotatable 64-byte Refresh Tokens.
- **Roles & Permissions**: Multi-tier hierarchy (`SuperAdmin`, `DistrictAdmin`, `SchoolPrincipal`, `Teacher`, `Counselor`, `StudentParent`) with claim-based authorization enforcement via custom `[RequirePermission(...)]` filter.
- **Profiles**: Profile management, staff onboarding, school assignments, and account activation/deactivation.

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

### Features
- **School Management**: Institutional profiling with UDISE code, District, Block/Zone, Rural/Urban area classification (`AreaType.Rural`), and marginalized area marker (`IsMarginalizedArea`).
- **Classes & Sections**: Multi-year academic calendar management (`AcademicYear`), grade levels (Grade 1 through 12), and sections (e.g. `9-A`) linked to class teachers.
- **Student Demographic & Socioeconomic Vulnerability Profiling**:
  - **Below Poverty Line (BPL)** flag (`IsBPL`).
  - **Commute Distance Hardship**: Exact distance in km (`DistanceToSchoolKm`), flagging students traveling $>5\text{ km}$ on foot.
  - **First-Generation Learner** marker (`IsFirstGenerationLearner`).
  - **Single Parent / Orphan** status indicator (`IsSingleParentOrOrphan`).
  - **Transport Mode**: Walking, Bicycle, Public Bus, School Bus.
  - **Guardian Details**: Name, relationship, emergency phone, and occupation (e.g. seasonal daily wage labor).
- **Enrollment & Lifecycle Transitions**:
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
| `POST` | `/api/v1/enrollments/enroll` | Enrolls a student into a section for an academic year | Yes (`students.write`) |
| `POST` | `/api/v1/enrollments/promote` | Promotes student to the next academic grade | Yes (`students.write`) |
| `POST` | `/api/v1/enrollments/dropout` | Records student dropout with root cause diagnosis | Yes (`students.write`) |
| `GET` | `/api/v1/students/{id}/history` | Retrieves full chronological timeline of student transitions | Yes (`students.history.read`) |

---

## 📅 Module 3: Attendance Management (Critical Dropout Indicator #1)

Educational research establishes chronic absenteeism as the single strongest early predictor of school dropouts (often appearing 1 to 2 years before actual withdrawal).

### Features
- **Daily Attendance Entry**:
  - Bulk section attendance marking (`Present`, `Absent`, `Late`, `HalfDay`, `Excused`).
  - Contextual absence reason tracking (`Illness`, `FamilyDomesticChore`, `AgriculturalOrSeasonalLabor`, `ExtremeWeatherOrTransportFailure`, `Unexcused`).
- **Attendance Percentage Engine**:
  - Real-time mathematical calculation:
    $$\text{Attendance Rate} = \frac{\text{Present Days} + 0.5 \times \text{HalfDays}}{\text{Total Working Days}} \times 100$$
  - Tracks cumulative rate, rolling 30-day window rate, and current consecutive absent streaks.
- **Absence Pattern Recognition & Alerts**:
  - **Chronic Absenteeism Flag**: Automatically flags any student whose cumulative attendance falls below **85%**.
  - **Consecutive Absence Streak Alerter**: Triggers high-priority counselor alerts when a student misses **3 or more consecutive unexcused school days**.
- **Attendance Calendar & Trends**:
  - Day-by-day monthly calendar view (`GET /api/v1/attendance/student/{id}/calendar`).
  - Trajectory tracking across months (`Improving`, `Stable`, `Declining`, `CriticalDrop`).
- **Intervention & Resolution Workflow**:
  - Counselors record intervention notes (home visits, parent meetings) directly into the student's longitudinal timeline.

### Key Endpoints (Module 3)
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `GET` | `/api/v1/attendance/roster` | Retrieves daily attendance roster for section marking | Yes (`attendance.read`) |
| `POST` | `/api/v1/attendance/batch` | Bulk records daily attendance for an entire section | Yes (`attendance.record`) |
| `PUT` | `/api/v1/attendance/{recordId}` | Updates individual student attendance record with reason | Yes (`attendance.record`) |
| `GET` | `/api/v1/attendance/student/{studentId}/summary` | Cumulative attendance percentage & stats | Yes (`attendance.read`) |
| `GET` | `/api/v1/attendance/student/{studentId}/calendar` | Monthly day-by-day attendance history grid | Yes (`attendance.read`) |
| `GET` | `/api/v1/attendance/alerts/chronic-absentees` | Lists all students with attendance &lt; 85% | Yes (`attendance.read`) |
| `POST` | `/api/v1/attendance/alerts/{alertId}/resolve` | Resolves absence alert with logged counselor notes | Yes (`attendance.record`) |

---

## 🎓 Module 4: Academic Performance & Learning Loss Monitoring (Critical Dropout Indicator #2)

Failing foundational core courses (Mathematics, Science, Language) and sudden collapses in test scores directly precipitate student disengagement and permanent withdrawal.

### Features
- **Academic Subjects & Assessments**:
  - Subject configuration with critical `IsCoreSubject` flag (Mathematics, Science, Language).
  - Multi-tiered examination scheduling: Unit Tests, Midterm Exams, Quarterly Assessments, Half-Yearly, and Final Examinations.
- **Grading & Evaluation Engine**:
  - Letter grading and GPA scale:
    - 90%–100%: **A+** (4.0 GPA)
    - 80%–89%: **A** (3.7 GPA)
    - 70%–79%: **B+** (3.3 GPA)
    - 60%–69%: **B** (3.0 GPA)
    - 50%–59%: **C** (2.0 GPA)
    - 35%–49%: **D** (1.0 GPA)
    - &lt;35% or Absent: **F** (0.0 GPA, `IsPass = false`)
  - Handles exam absences gracefully (`IsAbsent = true`, marks = 0, automatic failure).
- **Academic Shock Detection**:
  - Identifies acute learning loss: any student experiencing a score drop of **$\ge 15\%$** between consecutive evaluation periods (either within the same subject or term-over-term) is automatically flagged with `Trajectory = "AcademicShock"`.
- **Core Subject Failure & Academic Risk Tiering**:
  - $\ge 2$ Core Subjects Failed: **Critical** risk
  - $1$ Core Subject Failed: **High** risk
  - Any non-core failed: **Moderate** risk
  - 0 subjects failed: **Low** risk
- **Comprehensive Student Report Cards**:
  - Evaluates cumulative percentage, overall GPA, and individual subject summaries.

### Key Endpoints (Module 4)
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/academics/subjects` | Configures a new academic subject | Yes (`academics.record`) |
| `GET` | `/api/academics/subjects/school/{schoolId}` | Lists all subjects configured for a school | Yes (`academics.read`) |
| `POST` | `/api/academics/assessments` | Creates and schedules an assessment/examination | Yes (`academics.record`) |
| `GET` | `/api/academics/assessments` | Queries scheduled assessments by school/section/category | Yes (`academics.read`) |
| `GET` | `/api/academics/assessments/{id}/roster` | Retrieves grading roster with enrolled students & marks | Yes (`academics.read`) |
| `POST` | `/api/academics/marks/batch` | Bulk records marks for an assessment roster | Yes (`academics.record`) |
| `PUT` | `/api/academics/marks/{markId}` | Updates an individual student's mark | Yes (`academics.record`) |
| `GET` | `/api/academics/analytics/report-card/{studentId}` | Generates full student report card with GPA & core failures | Yes (`academics.reports`) |
| `GET` | `/api/academics/analytics/trajectory/{studentId}` | Analyzes academic trajectory and detects **Academic Shock** | Yes (`academics.reports`) |
| `GET` | `/api/academics/analytics/failing/{assessmentId}` | Lists failing students for a specific assessment | Yes (`academics.reports`) |
| `GET` | `/api/academics/analytics/at-risk/school/{schoolId}` | Identifies all students at academic risk in the school | Yes (`academics.reports`) |

---

## 💻 Interactive Web UI Dashboard

EduGuard includes an integrated, zero-dependency visual Single Page Dashboard served directly at `http://localhost:5065`:
- **Executive Dashboard**: Real-time KPI counters (Enrolled, Chronic Absentees, Academic Shock, Core Failures).
- **Module 2 Directory**: Student demographic profiles with socioeconomic hardship badges (BPL, Commute Distance $>5\text{ km}$, First-Gen Learner).
- **Module 3 Register**: Daily attendance roster marking and active chronic absenteeism alerts.
- **Module 4 Analytics**: Report cards with GPA, core failure tracking, and Academic Shock alerts.

---

## 🧪 Automated Test Suite (22 Tests Passed)

```text
Test run for EduGuard.UnitTests.dll (.NETCoreApp,Version=v10.0)
VSTest version 18.0.2 (arm64)

Passed!  - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 393 ms - EduGuard.UnitTests.dll (net10.0)
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
- **Module 3 (Attendance Management)**:
  13. `AttendanceService_GetRosterAndRecordBatch_ShouldUpdateCounts`
  14. `AttendanceAnalytics_Summary_ShouldCalculatePercentagesAccurately`
  15. `AbsencePatternDetector_DetectChronicAbsentees_ShouldIdentifyMarginalizedAtRiskStudent`
  16. `AbsencePatternDetector_RunAnalysis_ShouldTriggerConsecutiveStreakAlert`
  17. `AbsencePatternDetector_ResolveAlert_ShouldUpdateStatusAndLogTimeline`
- **Module 4 (Academic Performance & Analytics)**:
  18. `AcademicService_CreateSubjectAndAssessment_ShouldSucceed`
  19. `MarksService_RecordMarksBatchAndGetRoster_ShouldCalculateGradesCorrectly`
  20. `PerformanceAnalytics_GenerateReportCard_ShouldIdentifyCoreSubjectFailures`
  21. `PerformanceAnalytics_GetAcademicTrajectory_ShouldDetectAcademicShock`
  22. `PerformanceAnalytics_GetAtRiskAcademicStudentsBySchool_ShouldFlagHighRiskStudents`

---

## 🛠 Running the Project

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build Solution
```bash
dotnet build EduGuard.slnx
```

### Run All Tests
```bash
dotnet test EduGuard.slnx
```

### Run Web API & Interactive Dashboard
```bash
dotnet run --project src/EduGuard.WebApi/EduGuard.WebApi.csproj
```
Open **`http://localhost:5065`** in your browser to view the interactive dashboard.
