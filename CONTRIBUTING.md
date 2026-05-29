# Contributing to GS System Analyzer

Thanks for your interest in contributing. This is an active open source project and contributions are welcome from developers with experience in Flutter/Dart, C#/[ASP.NET](http://ASP.NET) Core, or C++.

Read this document fully before opening a pull request.

---

## 📍 Where We Are Right Now

<aside>
🚧

**Stage: Pre-Beta (v2.0 finalizing).** Core engine and most v2.0 panels are shipped. We're finishing the remaining panels and hardening before the public beta.

**Beta — June 2026** (before summer)  ·  **v2.0 official release — July 2026.**

Active focus right now: `THERMAL_SENSORS`, Settings/Config, Temp Cleaner, `NET_IO`, and `ACTIVE_PROCESS_TREE`.

</aside>

**Roadmap at a glance:**

- **v2.0 (now → July 2026)** — Finish thermal, settings, temp cleaner, network I/O, process tree.
- **v2.1 (Aug–Oct 2026)** — Advanced GPU thermals, multi-drive support, behavioural baselines.
- **v3.0 (Q1 2027)** — Historical telemetry, predictive alerts, cross-platform (macOS).

---

## What We're Building

GS System Analyzer is a cross-platform desktop application for real-time system telemetry, disk intelligence, and high-performance file management. It combines CPU/RAM/thermal monitoring, visual disk analysis, and a bulk file operations engine — all in a single Cyber-HUD interface built with Flutter and a C# backend.

The repo is structured as:

```
/lib       → Flutter frontend (Dart, Riverpod)
/backend   → ASP.NET Core 10 backend (C#, SignalR)
/test      → Flutter widget + unit tests
```

Backend unit tests live in `/backend/GSInteractiveDeviceAnalyzer.Tests`.

---

## Before You Start

1. **Check open issues first.** If you want to work on something, comment on the issue to claim it before writing code. This avoids duplicate work.
2. **No unsolicited rewrites.** Do not refactor code outside the scope of your assigned issue. If you see something worth improving, open a separate issue for it.
3. **Read the architecture rule.** Flutter never calls OS APIs directly. All OS-level work goes through the C# backend and arrives at the Flutter client via SignalR or REST. Do not break this boundary.

---

## Setting Up

**Prerequisites**

- Flutter SDK (stable channel)
- .NET 10.0 SDK
- Git

**Running locally**

```bash
# Backend
cd backend
dotnet restore
dotnet run

# Frontend (in a separate terminal)
cd lib
flutter pub get
flutter run
```

The backend serves on `http://localhost:5200` by default — make sure `ApiService` in the Flutter project points to it before running. Thermal/sensor reads require running the backend with Administrator privileges.

---

## Engineering Rules

These are not suggestions. Every pull request is checked against them.

### 1. Architecture

- Flutter → SignalR/REST → C# backend → OS APIs. This is the only permitted data flow.
- Flutter widgets receive data via Riverpod providers connected to SignalR streams.
- No direct OS calls from Dart code.

### 2. UI — HudTheme

All colors, typography, and decorations must reference `HudTheme` constants from `hud_theme.dart`.

```dart
// ✅ Correct
Text('58.6 GB', style: HudTheme.valueStyle)

// ❌ Wrong — never hardcode colors
Text('58.6 GB', style: TextStyle(color: Colors.white))
```

- All panel headers use **ALL_CAPS with underscores** via the `HudLabel` widget.
- Icon colors: folders → `accentAmber`, files → `accentGreen`, delete → `accentRed`, navigation → `accentCyan`.

### 3. Testing

- Every new backend service must include at least **one xUnit test** covering the happy path and **one edge case**.
- File deletion tests must use a **temp directory only** — never real paths.
- Every new Flutter widget must include at least **one widget test** covering the empty/loading state.
- Tests live in `/backend/GSInteractiveDeviceAnalyzer.Tests` (C#) and `/test/widget/` (Flutter).

```csharp
// ✅ Always use temp directories for file tests
var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
Directory.CreateDirectory(tempRoot);
```

---

## Pull Request Process

1. Fork the repo and create a branch from `main`.
2. Branch naming: `feature/short-description` or `fix/short-description`.
3. Write or update tests for any code you add.
4. Run existing tests before submitting — PRs that break passing tests will not be reviewed.
5. Keep PRs focused. One feature or fix per PR. Large PRs will be asked to be split.
6. Write a clear PR description: what you changed, why, and how to test it.
7. Not all contributions will be accepted. Decisions are based on product direction and engineering priorities.

**Merge gate — every PR to `main` must satisfy:**

1. New backend service → at least 1 xUnit test (happy path + 1 edge case).
2. File deletion code → tests use temp directories only, never real paths.
3. New Flutter widget → at least 1 widget test covering empty/loading state.
4. Cross-platform feature → tested on at least Windows before merge; Linux validation tracked in the issue.
5. No hardcoded colors → all colors reference `HudTheme` constants.

---

## Compensation & IP

This project is **open source** and currently **bootstrapped with no revenue**.

- Contributing is **voluntary**.
- There is **no payment** at this stage.
- When the product generates revenue, compensation for contributors will be considered based on level of impact, duration of involvement, and technical complexity handled. This is not a guarantee — it is a stated intention.
- All contributions are licensed under the same open source license as the rest of the project. You retain your rights as a contributor under that license.
- You retain the right to showcase your contributions in your portfolio and describe your work in professional profiles, provided no confidential internal information (outside the public repo) is disclosed.

---

## Code of Conduct

- Be direct and professional in reviews and discussions.
- Criticism of code is not criticism of the person.
- No dismissiveness toward contributors at any experience level.
- The project lead has final say on technical direction.

---

## Questions & Community

- **GitHub Discussions** — open a discussion or comment on the relevant issue. Do not DM for questions that belong in public; public discussions help everyone.
- **Discord** — join the contributor community at https://discord.gg/YOUR_INVITE_CODE for onboarding, dev chat, and release pings.

---

*Engineered by G00dS0ul — contributions welcome.*
