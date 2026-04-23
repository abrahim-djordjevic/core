import 'package:http/http.dart' as ref;
import 'package:signalr_netcore/signalr_client.dart';
import '../providers/directory_provider.dart';

  class TelemetryService {
    late HubConnection _hubConnection;
    Function(String)? onSectorChanged;

    final Function(String status, int count, String target) onProgressUpdate;

    TelemetryService({required this.onProgressUpdate}) {
      _initRadio();
    }

    void _initRadio() {
      final url = "http://localhost:5200/storageHub";

      _hubConnection = HubConnectionBuilder()
          .withUrl(url)
          .withAutomaticReconnect()
          .build();

      _hubConnection.on('ScanProgress', _handleIncomingTelemetry);
      _hubConnection.on('SectorChanged', _handleSectorChanged);
    }

    Future<void> startListening() async {
      if (_hubConnection.state == HubConnectionState.Disconnected) {
        try {
          await _hubConnection.start();
          print('TELEMETRY RADIO: CONNECTED TO BASE STATION!');
        } catch(e) {
          print('TELEMETRY RADIO ERROR: FAILED TO CONNECT TO BASE STATION! - $e');
        }
      }
    }

    Future<void> stopListening() async {
      if(_hubConnection.state == HubConnectionState.Connected) {
        await _hubConnection.stop();
        print('TELEMETRY RADIO: DISCONNECTED FROM BASE STATION!');
      }
    }

    void _handleIncomingTelemetry(List<Object?>? arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;

        final status = data['status'] as String;
        final count = data['count'] as int;
        final target = data['currentTarget'] as String;

        onProgressUpdate(status, count, target);
      }
    }

    void _handleSectorChanged(List<Object?>? arguments) {
      if(arguments != null && arguments.isNotEmpty) {
        String changedFolder = arguments[0].toString();
        print('RADAR ALERT RECEIVED: Changes in $changedFolder');

        if(onSectorChanged != null) {
          onSectorChanged!(changedFolder);
        }
      }
    }

  }