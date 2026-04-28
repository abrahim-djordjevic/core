# 🦅 GS Interactive Device Analyzer

![Flutter](https://img.shields.io/badge/Frontend-Flutter-02569B?style=for-the-badge&logo=flutter&logoColor=white)
![Riverpod](https://img.shields.io/badge/State-Riverpod-000000?style=for-the-badge&logo=dart&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/Backend-ASP.NET_Core_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SignalR](https://img.shields.io/badge/WebSockets-SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)

A high-performance, cross-platform system telemetry and disk management engine. Built with a reactive Flutter UI and powered by a multithreaded C# backend, the GS Analyzer provides real-time OS-level insights and execution protocols wrapped in a custom "Cyber-HUD" aesthetic.

## 🚀 Core Architecture

This project is built on a strict decoupling of concerns, utilizing an API-driven micro-architecture:

* **The Command Center (Frontend):** A Flutter application utilizing `Riverpod` for state management. It features a custom-built Telemetry HUD, reactive Directory Tree structures, and a streamlined UX for system operations.
* **The Engine Room (Backend):** An ASP.NET Core C# backend designed to handle heavy OS-level I/O operations, memory caching, and multithreaded directory walking without bottlenecking the main thread.

## 🔥 Key Features

### 1. The Nuke Protocol (Bulk Obliteration)
A highly optimized, weapons-grade deletion system. Instead of making standard N+1 API calls, the frontend bundles targeted nodes into a unified JSON payload. The multithreaded C# backend bypasses the OS recycling bin, obliterating massive directory structures and clearing memory caches simultaneously.

### 2. Live Radar (Reactive File System Monitoring)
Integrated `FileSystemWatcher` tied directly to a **SignalR WebSocket Hub**. If a user or external application modifies, deletes, or creates a file on the target drive, the backend instantly pushes the update to the Flutter UI, triggering a targeted Riverpod invalidation to redraw the UI with zero manual refreshing.

### 3. Parallel Disk Scanning Engine
Utilizes `Parallel.ForEachAsync` and `ConcurrentDictionary` to aggressively map local storage. It calculates deep directory sizes across thousands of subfolders concurrently, caching the results to heavily reduce CPU load on subsequent reads.

### 4. Hardware Telemetry HUD
Real-time monitoring of drive space, allocation percentages, and system limits dynamically displayed via custom-built Flutter widgets (`TelemetryHudWidget`).

## 🛠️ Installation & Setup

### Prerequisites
* [Flutter SDK](https://flutter.dev/docs/get-started/install)
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

### Running the Backend (C# Engine)
1. Navigate to the `/backend` directory.
2. Restore NuGet packages: `dotnet restore`
3. Launch the API and SignalR Hub: `dotnet run`
*The server will initialize the Disk Scanner Engine and await WebSocket connections.*

### Running the Frontend (Flutter UI)
1. Navigate to the `/lib` directory.
2. Fetch dependencies: `flutter pub get`
3. Ensure the `ApiService` is pointing to your local ASP.NET localhost port.
4. Launch the application: `flutter run`

## 🗺️ Future Roadmap
* **Volatile Memory (RAM) Scanner:** Implementing a dedicated OS Task Manager subset to monitor, chart, and free up system RAM via `System.Diagnostics.Process` APIs.
* **Deep File Type Analytics:** Visualizing disk usage via dynamically generated storage pie charts mapping out media vs. executable bloat.

---
*Engineered by [G00dS0ul]*
