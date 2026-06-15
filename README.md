# 🦅 GS System Analyzer

![Flutter](https://img.shields.io/badge/Frontend-Flutter-02569B?style=for-the-badge&logo=flutter&logoColor=white)

![Riverpod](https://img.shields.io/badge/State-Riverpod-000000?style=for-the-badge&logo=dart&logoColor=white)

![ASP.NET Core](https://img.shields.io/badge/Backend-ASP.NET_Core_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

![SignalR](https://img.shields.io/badge/WebSockets-SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)

![Status](https://img.shields.io/badge/Status-Pre--Beta_(v2.0)-FF8C00?style=for-the-badge)

[![Discord](https://img.shields.io/badge/Community-Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/FA8WsVXMx)

A high-performance, cross-platform system telemetry and disk management engine. Built with a reactive Flutter UI and powered by a multithreaded C# backend, GS System Analyzer provides real-time OS-level insights and execution protocols wrapped in a custom **"Cyber-HUD"** aesthetic.

> Think **Task Manager + TreeSize + HWiNFO**, fused into one keyboard-fast desktop cockpit — but open source, scriptable, and built around a strict frontend/backend boundary.

---

## 📍 Project Status — Where We Are Right Now

**Current stage: Pre-Beta, finalizing the v2.0 feature set (late May 2026).**

The core engine is stable and the primary telemetry panels are live. We are now hardening the remaining v2.0 features ahead of the public beta.

| Milestone | Target | State |
| --- | --- | --- |
| **Public Beta** | Aug 2026 (summer) | 🟡 In progress — feature freeze approaching |
| **v2.0 Official Release** | Sept 2026 | ⏳ Planned |
| **v2.1** | Oct – Dec 2026 | 🗒️ Backlog |
| **v3.0** | Q1 2027 | 🔭 Future |

What's working today vs. what's still being built is tracked in the **Feature Status** section below.

---

## 🧭 Overview

GS System Analyzer is split into two cleanly decoupled halves that talk over REST + SignalR:

- **The Command Center (Frontend)** — a Flutter application using `Riverpod` for state management. It renders the Telemetry HUD, reactive directory trees, and all system-operation UX. Flutter **never** touches OS APIs directly.
- **The Engine Room (Backend)** — an ASP.NET Core 10 (C#) backend that handles all heavy OS-level I/O, memory caching, multithreaded directory walking, and hardware sensor reads, then streams results to the UI.

This boundary is the single most important architectural rule in the project: **all OS-level work happens in C#, and data reaches Flutter only via SignalR streams or REST.**

```
┌───────────┬──────────────────────────────────────────────┐
│  SIDEBAR  │  [CPU_LOAD]   [MEM_ALLOCATION]   [NET_IO]   │
│           ├──────────────────────────────────────────────┤
│  DASHBRD  │  [ACTIVE_PROCESS_TREE  (wide)]  [THERMAL]   │
│  CPU MTR  │                                              │
│  MEMORY   │                                              │
│  STORAGE  │                                              │
│  NETWORK  │                                              │
│  THERMAL  │  ← dedicated full-screen thermal module      │
└───────────┴──────────────────────────────────────────────┘
```

---

## 🔥 Key Features

### 1. The Nuke Protocol (Bulk Obliteration) — ✅ Shipped
A weapons-grade deletion system. Instead of N+1 API calls, the frontend bundles targeted nodes into a single JSON payload. The multithreaded C# backend bypasses the OS recycle bin, obliterating massive directory structures and clearing memory caches simultaneously. Every nuke is gated behind a mandatory **Dry Run** preview + non-dismissible confirmation.

### 2. Live Radar (Reactive File System Monitoring) — ✅ Shipped
`FileSystemWatcher` wired directly into a **SignalR WebSocket Hub**. Any external create/modify/delete on the target drive is instantly pushed to the Flutter UI, triggering a targeted Riverpod invalidation — zero manual refresh.

### 3. Parallel Disk Scanning Engine — ✅ Shipped
`Parallel.ForEachAsync` + `ConcurrentDictionary` aggressively map storage, calculating deep directory sizes across thousands of subfolders concurrently and caching results to cut CPU load on repeat reads. Live progress streams to the UI; scans are cancellable mid-flight with no dangling threads.

### 4. `CPU_LOAD` Panel — ✅ Shipped
Real-time average CPU % with per-tick delta, live frequency, process/thread/handle counts, L1–L3 cache info, and grouped per-core bar charts (CORE 0–3, 4–7, 8–15…).

### 5. `MEM_ALLOCATION` Panel (RAM Scanner) — ✅ Shipped
Live total/used/cached/swap RAM plus a full per-process breakdown (the system's Task-Manager-Memory equivalent), with a kill-process action streamed back over SignalR.

### 6. Disk Intelligence Suite — ✅ Shipped
**Duplicate File Detector** (SHA-256 content hashing), **Large File Hunter** (top-N space hogs), and a **Temp Folder Cleaner** — all feeding directly into the Nuke Protocol with Dry Run safety.

### 7. `THERMAL_SENSORS` Panel — 🛠️ In Progress (v2.0)
Real-time CPU package/per-core temps, motherboard/chipset/NVMe temps, fan RPM, power draw, and throttle detection via **LibreHardwareMonitor** on Windows and `sysfs` on Linux. Graceful `N/A` fallback when sensors or elevation are unavailable. *(GPU/extended sensors deferred to v2.1.)*

> ⚠️ **Admin note:** Full thermal/fan/power data on Windows requires running the backend **as Administrator** (LHM uses EC/MSR/RAPL access). Without elevation, CPU temps may still appear via CPUID, but fan RPM, board sensors, and power can read `N/A`.

---
## 📂 Repo Layout

```
/lib            → Flutter frontend (Dart, Riverpod)
/backend        → ASP.NET Core 10 backend (C#, SignalR)
/test           → Flutter widget + unit tests
```

## 📊 Feature Status

| Feature | Target | Status |
| --- | --- | --- |
| Nuke Protocol + Dry Run | v2.0 | ✅ Shipped |
| Live Radar (FileSystemWatcher + SignalR) | v2.0 | ✅ Shipped |
| Parallel Disk Scanning + progress stream | v2.0 | ✅ Shipped |
| Scan cancellation | v2.0 | ✅ Shipped |
| `CPU_LOAD` panel | v2.0 | ✅ Shipped |
| `MEM_ALLOCATION` panel | v2.0 | ✅ Shipped |
| Duplicate File Detector | v2.0 | ✅ Shipped |
| Large File Hunter | v2.0 | ✅ Shipped |
| Temp Folder Cleaner | v2.0 | 🛠️ In progress |
| `THERMAL_SENSORS` panel (Standard tier) | v2.0 | 🛠️ In progress |
| Settings / Config panel | v2.0 | ⏳ Planned |
| CPU history line-chart view | v2.0 | ⏳ Planned |
| `NET_IO` panel | v2.0 | ⏳ Planned |
| `ACTIVE_PROCESS_TREE` panel | v2.0 | ⏳ Planned |
| Advanced thermals (GPU), Multi-Drive, baselines | v2.1 | 🗒️ Backlog |
| Historical / predictive / cross-platform | v3.0 | 🔭 Future |

---

## 🛠️ Installation & Setup

### Prerequisites
- [Flutter SDK](https://flutter.dev/docs/get-started/install) (stable channel)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Git

### Running the Backend (C# Engine)
1. Navigate to the `/backend` directory.
2. Restore NuGet packages: `dotnet restore`
3. Launch the API + SignalR Hub: `dotnet run`

> 💡 On Windows, run your terminal **as Administrator** if you want full thermal/fan/power readings.

The server initializes the Disk Scanner Engine and awaits WebSocket connections (default base URL `http://localhost:5200`).

### Running the Frontend (Flutter UI)
1. Navigate to the `/lib` directory.
2. Fetch dependencies: `flutter pub get`
3. Ensure `ApiService` points to your local ASP.NET port.
4. Launch: `flutter run`

---

## 🗺️ Roadmap

### 🔜 Beta — Aug 2026
Feature-complete v2.0 candidate. Public beta ships **before summer**. Focus: finishing the Thermal panel, Settings persistence, and the Temp Cleaner; full platform-matrix validation (Windows 10/11 + Ubuntu 22.04).

### 🎯 v2.0 — Sept 2026 (Official Release)
Stable cross-platform release of the full telemetry + disk-intelligence suite described above.

### 🟣 v2.1 — Oct – Dec 2026
- Advanced thermals (GPU core/hotspot/VRAM, GPU fan)
- Multi-Drive support (per-drive scan, monitor, and manage)
- Behavioural baselines & anomaly detection

### 🔭 v3.0 — Q1 2027
- Historical telemetry charts & predictive analytics
- Cross-platform expansion (macOS native layer)

---

## 💬 Community

Join the **GS System Analyzer** Discord — the hub for contributors and developers (issue triage, design discussion, build help, and release pings):

👉 **[discord.gg](https://discord.gg/FA8WsVXMx)**

A dedicated user-support space will open alongside the public beta.

---

## 🤝 Contributing

Contributions are welcome from developers with Flutter/Dart, C#/ASP.NET Core, or C++ experience. Please read **[CONTRIBUTING.md](CONTRIBUTING.md)** in full before opening a pull request — it covers the architecture rule, the HudTheme UI contract, and the testing gates every PR is checked against.

---

*Contributions welcome.*
