import 'package:gs_analyzer_ui/utils/logger.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class TelemetryService {
  late HubConnection _hubConnection;

  Function(String)? onSectorChanged;
  final Function(String? scanId, String? status, int? completed, int? total, double? percentComplete, String? target) onProgressUpdate;
  Function(double percentage, String target, int completed)? onNukeProgress;
  Function()? onNukeAborted;
  Function(Map<String, dynamic>)? onRamUpdate;
  Function(String path, List<dynamic> chunk)? onDirectoryChunk;
  Function(String path)? onDirectoryStreamComplete;
  Function(Map<String, dynamic>)? onCpuUpdate;
  Function(List<dynamic>)? onDriveUpdate;
  Function(Map<String, dynamic>)? onAuditProgress;

  TelemetryService({
    required this.onProgressUpdate,
    int backendPort            = 5200,
    int reconnectDelayMs       = 3000,
    int maxRetries             = 10,
  }) {
    _initRadio(backendPort, reconnectDelayMs, maxRetries);
  }

  void _initRadio(int port, int reconnectDelayMs, int maxRetries) {
    final url = "http://localhost:$port/systemHub";

    final retryDelays = List.generate(
      maxRetries,
      (i) => reconnectDelayMs * (i + 1),
    );

    _hubConnection = HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect(retryDelays: retryDelays)
        .build();

    _hubConnection.on('ScanProgress',            _handleIncomingTelemetry);
    _hubConnection.on('SectorChanged',           _handleSectorChanged);
    _hubConnection.on('NukeProgress',            _handleNukeProgress);
    _hubConnection.on('NukeAborted',             _handleNukeAborted);
    _hubConnection.on('RamUpdate',               _handleRamUpdate);
    _hubConnection.on('DirectoryChunk',          _handleDirectoryChunk);
    _hubConnection.on('DirectoryStreamComplete', _handleDirectoryStreamComplete);
    _hubConnection.on('ReceiveCpuTelemetry',     _handleCpuUpdate);
    _hubConnection.on('DriveListUpdate',         _handleDriveUpdate);
    _hubConnection.on('AuditProgress',           _handleAuditProgress);
  }

    Future<void> startListening() async {
      if (_hubConnection.state == HubConnectionState.Disconnected) {
        try {
          await _hubConnection.start();
          appLogger.i('TELEMETRY RADIO: CONNECTED TO BASE STATION!');
          
          final api = ApiService();
          api.startRamRadar();
          api.startCpuRadar();
        } catch (e) {
          appLogger.i(
              'TELEMETRY RADIO ERROR: FAILED TO CONNECT TO BASE STATION! - $e');
        }
      }
    }

    Future<void> stopListening() async {
      if (_hubConnection.state == HubConnectionState.Connected) {
        await _hubConnection.stop();
        appLogger.i('TELEMETRY RADIO: DISCONNECTED FROM BASE STATION!');
      }
    }

    void _handleIncomingTelemetry(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;

        final scanId = data['scanId'] as String?;
        final status = data['status'] as String?;
        final completed = (data['completed'] as num?)?.toInt();
        final total = (data['total'] as num?)?.toInt();
        final percentageComplete = (data['percentComplete'] as num?)
            ?.toDouble();
        final target = data['currentTarget'] as String?;

        onProgressUpdate(scanId, status, completed, total, percentageComplete, target);
      }
    }

    void _handleSectorChanged(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        String changedFolder = arguments[0].toString();
        appLogger.i('RADAR ALERT RECEIVED: Changes in $changedFolder');

        if (onSectorChanged != null) {
          onSectorChanged!(changedFolder);
        }
      }
    }

    void _handleNukeProgress(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;

        final percentage = (data['percentage'] as num?)?.toDouble() ?? 0.0;
        final target = data['target'] as String? ?? '';
        final completed = (data['completed'] as num?)?.toInt() ?? 0;
        if (onNukeProgress != null) {
          onNukeProgress!(percentage, target, completed);
        }
      }
    }

    void _handleNukeAborted(List<Object?>? arguments) {
      appLogger.i('RADIO ALERT: NUKE ABORT SIGNAL RECEIVED FROM BACKEND');
      if (onNukeAborted != null) {
        onNukeAborted!();
      }
    }

    void _handleRamUpdate(List<Object?>? arguments) {
      if (arguments == null || arguments.isEmpty) return;
      try {
        final rawData = arguments[0];

        if (rawData is Map) {
          final data = Map<String, dynamic>.from(rawData);
          if (onRamUpdate != null) {
            onRamUpdate!(data);
          }
        }

        else if (rawData is List) {
          appLogger.i(
              'ARCHITECT ALERT: The backend is still sending the old list! The C# engine needs to be rebuilt');
        }

        else {
          appLogger.i('UNKNOWN PAYLOAD TYPE: ${rawData.runtimeType}');
        }
      } catch (e) {
        appLogger.i('RAM PAYLOAD CRASH: $e');
      }
    }

    void _handleDirectoryChunk(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        if (onDirectoryChunk != null) {
          onDirectoryChunk!(data['path'], data['chunk']);
        }
      }
    }

    void _handleDirectoryStreamComplete(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        if (onDirectoryStreamComplete != null) {
          onDirectoryStreamComplete!(arguments[0].toString());
        }
      }
    }

    void _handleCpuUpdate(List<Object?>? arguments) {
      if (arguments == null || arguments.isEmpty) return;

      try {
        final rawData = arguments[0];

        if (rawData is Map) {
          final data = Map<String, dynamic>.from(rawData);
          if (onCpuUpdate != null) {
            onCpuUpdate!(data);
          }
        }
      } catch (e) {
        appLogger.i('CPU TELEMETRY CRASH: $e');
      }
    }

    void _handleDriveUpdate(List<Object?>? arguments) {
        if (arguments != null && arguments.isNotEmpty) return;
        try {
          final rawData = arguments?[0];

          if (rawData is List) {
            if (onDriveUpdate != null) {
              onDriveUpdate!(rawData);
            }
          } else {
            appLogger.i('UNKNOWN DRIVE PAYLOAD TYPE: ${rawData.runtimeType}');
          }
        } catch (e) {
          appLogger.i('DRIVE TELEMETRY CRASH: $e');
        }
    }

    void _handleAuditProgress(List<Object?>? arguments) {
      if (onAuditProgress != null && arguments != null && arguments.isNotEmpty) {
        onAuditProgress!(arguments[0] as Map<String, dynamic>);
      }
    }
  }