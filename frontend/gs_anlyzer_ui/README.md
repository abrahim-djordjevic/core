# 🦅 GS Interactive Analyzer: Command Center (Frontend)

![Flutter](https://img.shields.io/badge/Framework-Flutter-02569B?style=for-the-badge&logo=flutter&logoColor=white)

![Dart](https://img.shields.io/badge/Language-Dart-0175C2?style=for-the-badge&logo=dart&logoColor=white)

![Riverpod](https://img.shields.io/badge/State_Management-Riverpod-000000?style=for-the-badge&logo=dart&logoColor=white)

The Command Center is the reactive, cross-platform graphical user interface (GUI) for the GS Interactive Device Analyzer. Built with **Flutter** and powered by **Riverpod** for robust state management, this frontend provides a high-performance "Cyber-HUD" aesthetic to visualize OS-level telemetry, manage massive directory trees, and execute destructive operations.

## 🏗️ Architecture & State Management

This application strictly follows a decoupled, reactive architecture. UI components never manage their own complex state or make direct API calls. 

We utilize **Riverpod** to create a highly predictable, immutable state flow:
* `DirectoryProvider`: Manages the current path state, bulk-selection mode, and directory traversal logic.
* `TelemetryProvider` & `DriveStatsProvider`: Ingests and formats raw hardware metrics (Total Bytes, Free Bytes) from the backend into human-readable UI states.
* `RootTreeProvider`: Caches and invalidates the visual directory structure, enabling instant UI redraws when the backend's "Live Radar" detects a file system change.

## 🚀 Core UX Features

### 1. The Telemetry HUD
A custom-built, dark-themed dashboard (`TelemetryHudWidget` & `DriveTelemetryWidget`) that continuously visualizes disk usage and system health. It translates raw byte data into dynamic visual gauges and typography-heavy readouts.

### 2. Deep Tree Navigation
The `DirectoryNodeWidget` combined with the `GoUpRowWidget` allows users to seamlessly traverse massive storage structures. It features a reactive search filter (`DirectorySearchWidget`) to instantly query loaded nodes without triggering unnecessary network requests.

### 3. The Nuke Protocol (UX & Execution)
A specialized operations layer (`nuke_protocol.dart`) that handles dangerous destructive commands safely.
* **Smart UI Context:** Dynamically shifts warning dialogs based on single-target vs. bulk-selection (`isBulk`) contexts.
* **Global Alerting:** Utilizes a global `ScaffoldMessengerKey` to trigger success/failure SnackBars directly from background execution files, bypassing localized UI context limitations.

## 📂 Project Structure

```text
lib/
├── models/             # Immutable data structures (StorageNode, DriveStats)
├── providers/          # Riverpod State Notifiers and Providers
├── screen/             # Main layout canvases (AnalyzerDashboard)
├── services/           # External communication (ApiService, TelemetryService)
├── utils/              # Global keys and operational logic (NukeProtocol)
├── widgets/            # Modular, reusable UI components (Nodes, HUDs, Headers)
└── main.dart           # App entry point and ProviderScope wrapper
