# EduGuard - Student Dropout Analysis & Retention Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()
[![Tests](https://img.shields.io/badge/Tests-17%20Passed-success.svg)]()

> **Problem Statement**: High dropout rates in schools hinder educational progress, especially in marginalized and rural communities. **EduGuard** is an Early Warning & Retention System (EWIS) engineered in C# (.NET 10) to proactively identify at-risk students, diagnose root causes (attendance patterns, academic struggles, socioeconomic hardship), and orchestrate targeted interventions before students disengage permanently.

---

## 🏛 Clean Architecture Overview

The solution is structured into decoupled layers following Clean Architecture and Domain-Driven Design (DDD) principles:

```
EduGuard/
├── EduGuard.slnx
├── src/
│   ├── EduGuard.Domain/            # Entities, Value Objects, Enums, Core Domain Rules
│   ├── EduGuard.Application/       # DTOs, Service Interfaces, CQRS, Risk Engine
│   ├── EduGuard.Infrastructure/    # Persistence, Security, Token Services, Data Seeder
│   └── EduGuard.WebApi/            # Controllers, Middleware, Auth Policies, App Entrypoint
├── tests/
│   └── EduGuard.UnitTests/         # Automated unit and integration test suite
```

---

## 🚀 Module 1: Authentication & User Management (Completed)
- **Authentication**: Secure credential verification with PBKDF2 (SHA-256, 100,000 iterations) + random 128-bit salt, account lockout after 5 consecutive failed attempts, HMAC-SHA256 JWT access token generation, and rotatable 64-byte Refresh Tokens.
- **Roles & Permissions**: Multi-tier roles (`SuperAdmin`, `DistrictAdmin`, `SchoolPrincipal`, `Teacher`, `Counselor`, `StudentParent`) and claim-based authorization filter `[RequirePermission(...)]`.
- **Profiles**: Staff onboarding, user profile editing, school assignments, and account activation/deactivation.

---

## 🏫 Module 2: School & Student Management (Completed)
- **Schools**: School registration, district/block tracking, rural/urban classification, and `IsMarginalizedArea` markers.
- **Classes & Sections**: Academic year calendar management, grades/standards (e.g., Grade 9), sections (e.g., 9-A), and class teacher assignments.
- **Students & Socioeconomic Vulnerability Tracking**: Captures critical dropout risk indicators:
  - Below Poverty Line (`IsBPL`)
  - First-Generation Learner (`IsFirstGenerationLearner`)
  - Travel distance to school in km (`DistanceToSchoolKm`) & `TransportMode`
  - Single Parent / Orphan status (`IsSingleParentOrOrphan`)
  - Family income tier & guardian occupation
- **Enrollment & Lifecycle**: Class enrollments, batch student promotions across terms, and retention re-enrollment.
- **Dropout Diagnostic Recording**: Captures dropout events with date, primary root cause (`FinancialHardship`, `DistanceAndTransport`, `ChildLaborOrFamilyWork`, `EarlyMarriageOrFamilyIssues`, `AcademicDifficulty`, `HealthIllness`, `SeasonalMigration`), and audit remarks.
- **Longitudinal Student Timeline**: Chronological event history (Enrollment $\rightarrow$ Promotion $\rightarrow$ Counselor Notes $\rightarrow$ Dropout $\rightarrow$ Re-Enrollment).

---

## 📅 Module 3: Attendance Management (Completed)
- **Daily Attendance Capture**: Bulk section attendance marking (`Present`, `Absent`, `Late`, `HalfDay`, `Excused`) with diagnostic absence reason tracking (`Illness`, `DomesticChores`, `AgriculturalOrSeasonalLabor`, `TransportFailure`).
- **Attendance Percentage Engine**: Real-time aggregation:
  $$\text{Attendance Rate} = \frac{\text{Present Days} + 0.5 \times \text{HalfDays}}{\text{Total Working Days}} \times 100$$
  Tracks cumulative rate, rolling 30-day rate, and current consecutive absent streaks.
- **Chronic Absenteeism Detection**: Automatically flags students whose cumulative attendance drops below **85%** (a prime leading indicator of dropout risk).
- **Consecutive Absence Streak Alerter**: Triggers high-priority counselor alerts when a student misses **3 or more consecutive unexcused school days**.
- **Attendance Trends & Calendar**: Day-by-day student monthly calendar grids, section daily statistics, and long-term trajectory analysis (`Improving`, `Stable`, `Declining`, `CriticalDrop`).
- **Alert Resolution Workflow**: Counselors record intervention notes and mark alerts resolved directly into the student timeline.

---

## 🧪 Testing & Verification

All **17 automated unit tests** across Module 1, Module 2, and Module 3 run and pass in **1 second**:

```bash
dotnet test EduGuard.slnx
```
```text
Passed!  - Failed: 0, Passed: 17, Skipped: 0, Total: 17, Duration: 1 s - EduGuard.UnitTests.dll (net10.0)
```

---

## 🛠 Running the Application

```bash
# Build the solution
dotnet build EduGuard.slnx

# Run Web API server
dotnet run --project src/EduGuard.WebApi/EduGuard.WebApi.csproj
```
