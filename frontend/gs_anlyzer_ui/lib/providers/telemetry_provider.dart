import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/services/telemetry_service.dart';

class TelemetryState {
  final String status;
  final int count;
  final String target;

  const TelemetryState({
    this.status = 'IDLE',
    this.count = 0,
    this.target = '',
  });

  TelemetryState copyWith({String? status, int? count, String? target}) {
    return TelemetryState(
      status: status ?? this.status,
      count: count ?? this.count,
      target: target ?? this.target,
    );
  }
}

class TelemetryNotifier extends StateNotifier<TelemetryState> {
  TelemetryService? _telemetryService;

  TelemetryNotifier() : super(const TelemetryState()) {
    _initRadio();
  }

  void _initRadio() {
    _telemetryService = TelemetryService(onProgressUpdate: (status, count, target) {
      state = state.copyWith(status: status, count: count, target: target);
      },
    );
    _telemetryService?.startListening();
  }

  @override
  void dispose() {
    _telemetryService?.stopListening();
    super.dispose();
  }
}

final telemetryProvider = StateNotifierProvider<TelemetryNotifier, TelemetryState>((ref) {
  return TelemetryNotifier();
});
