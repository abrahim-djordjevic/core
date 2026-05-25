import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:gs_analyzer_ui/models/thermal_telemetry.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class ThermalNotifier extends StateNotifier<ThermalTelemetry?> {
  HubConnection? _hubConnection;
  final ApiService _apiService = ApiService();

  ThermalNotifier(): super(null) {
    _fetchInitialSnapshot();
    _initSignalR();
}

  Future<void> _fetchInitialSnapshot() async {
    try {
      final snapshot = await _apiService.getCurrentThermals();
      if (snapshot != null) {
        // Instantly populate the UI while SignalR is still waking up
        state = ThermalTelemetry.fromJson(snapshot);
        print("🦅🔥 THERMAL RADAR: Instant Snapshot Loaded!");
      }
    } catch (e) {
      print("Failed to load initial thermal snapshot: $e");
    }
  }

Future<void> _initSignalR() async {
    final serverUrl = 'http://localhost:5200/systemHub';

    _hubConnection = HubConnectionBuilder().withUrl(serverUrl).withAutomaticReconnect().build();

    _hubConnection?.on("ReceiveThermalTelemetry", _handleThermalUpdate);

    try {
      await _hubConnection?.start();
      print('THERMAL RADAR CONNECTED TO SYSTEM HUB!');
    } catch (e) {
      print('TELEMETRY ERROR: Failed to connect thermal Radar: $e');
    }
  }

  void _handleThermalUpdate(List<dynamic>? arguments) {
    if (arguments != null && arguments.isNotEmpty) {
      try {
        final data = arguments.first as Map<String, dynamic>;
        state = ThermalTelemetry.fromJson(data);
      } catch (e) {
        print('THERMAL PAYLOAD CRASH: $e');
      }
    }
  }

  @override
  void dispose() {
    _hubConnection?.stop();
    super.dispose();
  }
}

final thermalProvider = StateNotifierProvider<ThermalNotifier, ThermalTelemetry?>((ref) {
  return ThermalNotifier();
});