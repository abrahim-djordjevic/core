import 'package:signalr_netcore/signalr_client.dart';

  class TelemetryService {
    late HubConnection _hubConnection;
    Function(String)? onSectorChanged;
    final Function(String? status, int? completed, int? total, double? percentComplete, String? target) onProgressUpdate;
    Function(double percentage, String target, int completed)? onNukeProgress;
    Function()? onNukeAborted;
    Function(Map<String, dynamic>)? onRamUpdate;
    Function(String path, List<dynamic> chunk)? onDirectoryChunk;
    Function(String path)? onDirectoryStreamComplete;
    Function(Map<String, dynamic>)? onCpuUpdate;
    Function(List<dynamic>)? onDriveUpdate;

    TelemetryService({required this.onProgressUpdate}) {
      _initRadio();
    }

    void _initRadio() {
      final url = "http://localhost:5200/systemHub";

      _hubConnection = HubConnectionBuilder()
          .withUrl(url)
          .withAutomaticReconnect()
          .build();

      _hubConnection.on('ScanProgress', _handleIncomingTelemetry);
      _hubConnection.on('SectorChanged', _handleSectorChanged);
      _hubConnection.on('NukeProgress', _handleNukeProgress);
      _hubConnection.on('NukeAborted', _handleNukeAborted);
      _hubConnection.on('RamUpdate', _handleRamUpdate);
      _hubConnection.on('DirectoryChunk', _handleDirectoryChunk);
      _hubConnection.on('DirectoryStreamComplete', _handleDirectoryStreamComplete);
      _hubConnection.on('ReceiveCpuTelemetry', _handleCpuUpdate);
      _hubConnection.on('DriveListUpdate', _handleDriveUpdate);

      _hubConnection.start()?.catchError((err) {
        print('TELEMETRY RADIO ERROR: $err');
      });
    }

    Future<void> startListening() async {
      if (_hubConnection.state == HubConnectionState.Disconnected) {
        try {
          await _hubConnection.start();
          print('TELEMETRY RADIO: CONNECTED TO BASE STATION!');
        } catch (e) {
          print(
              'TELEMETRY RADIO ERROR: FAILED TO CONNECT TO BASE STATION! - $e');
        }
      }
    }

    Future<void> stopListening() async {
      if (_hubConnection.state == HubConnectionState.Connected) {
        await _hubConnection.stop();
        print('TELEMETRY RADIO: DISCONNECTED FROM BASE STATION!');
      }
    }

    void _handleIncomingTelemetry(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;

        final status = data['status'] as String?;
        final completed = (data['completed'] as num?)?.toInt();
        final total = (data['total'] as num?)?.toInt();
        final percentageComplete = (data['percentComplete'] as num?)
            ?.toDouble();
        final target = data['currentTarget'] as String?;

        onProgressUpdate(status, completed, total, percentageComplete, target);
      }
    }

    void _handleSectorChanged(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        String changedFolder = arguments[0].toString();
        print('RADAR ALERT RECEIVED: Changes in $changedFolder');

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
      print('RADIO ALERT: NUKE ABORT SIGNAL RECEIVED FROM BACKEND');
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
          print(
              'ARCHITECT ALERT: The backend is still sending the old list! The C# engine needs to be rebuilt');
        }

        else {
          print('UNKNOWN PAYLOAD TYPE: ${rawData.runtimeType}');
        }
      } catch (e) {
        print('RAM PAYLOAD CRASH: $e');
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
        print('CPU TELEMETRY CRASH: $e');
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
            print('UNKNOWN DRIVE PAYLOAD TYPE: ${rawData.runtimeType}');
          }
        } catch (e) {
          print('DRIVE TELEMETRY CRASH: $e');
        }
    }
  }