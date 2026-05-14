# 🦅 GS Interactive Analyzer: The Engine Room (Backend)

![C#](https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)

![ASP.NET Core](https://img.shields.io/badge/Framework-ASP.NET_Core_10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

![SignalR](https://img.shields.io/badge/WebSockets-SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)

The Engine Room is the high-performance, multithreaded backend for the GS Interactive Device Analyzer. Built on **ASP.NET Core 10.0**, this C# API acts as a bridge between the frontend Command Center and the Windows Operating System. 

## 🔄 The Running Process (Execution Lifecycle)
The backend operates as a continuous, stateful application. Understanding the runtime process is critical for further development:

### 1. Bootstrap & Memory Load (Startup)
When `dotnet run` is executed, the `Program.cs` Dependency Injection (DI) container initializes the `DiskScannerEngine` as a **Singleton**. The engine immediately searches the local disk for `scanner_memory.json`. If memory exists from a previous session, it loads the data into a `ConcurrentDictionary`, allowing the API to skip millions of I/O disk reads on the next scan.

### 2. The Radar Deployment (Active State)
Once the REST API and SignalR Hubs are open, the server waits for a target sector from the frontend. Upon receiving a path:
* The `FileSystemWatcher` (_The Radar_) is deployed to the specific sector. 
* It operates independently of the API endpoints, actively listening for OS-level `Changed`, `Created`, or `Deleted` events.
* A `SemaphoreSlim` lock guarantees that multiple frontend requests do not overlap and corrupt the scanning threads.

### 3. The Execution Loop (Multithreading)
When a deep scan is required, the engine utilizes `Parallel.ForEachAsync`. This allows the C# backend to attack the hard drive from multiple angles at once, processing thousands of files per second and calculating total byte sizes without blocking the main HTTP thread. Progress is streamed back to the frontend dynamically via the `/storageHub` SignalR connection.

### 4. Cache Invalidation & The Nuke Protocol
When a destructive command (`ObliterateNodes`) is received via the HTTP Body payload, the backend physically permanently deletes the data. Crucially, the process then executes `InvalidateCache()`, traversing the `ConcurrentDictionary` memory tree and safely removing the destroyed nodes so the server's internal memory perfectly mirrors the physical hard drive.

## 📂 Project Structure
```text
backend/
├── Controllers/        # REST Endpoints (StorageController)
├── Hubs/               # SignalR WebSockets (StorageHub)
├── Interfaces/         # Service Contracts (IDiskOperationService)
├── Models/             # DTOs and Data Structures
├── Services/           # Core OS-level logic execution
├── DiskScannerEngine.cs# Multithreaded core memory & radar system
└── Program.cs          # DI Container & Bootstrap
