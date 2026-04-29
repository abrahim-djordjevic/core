import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/services/telemetry_service.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';

import 'nuke_provider.dart';

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
  final Ref ref;

  TelemetryNotifier(this.ref) : super(const TelemetryState()) {
    _initRadio();
  }

  void _initRadio() {
    _telemetryService = TelemetryService(onProgressUpdate: (status, count, target) {
      state = state.copyWith(status: status, count: count, target: target);
      },
    );

    _telemetryService?.onNukeProgress = (percentage, target, completed) {
      ref.read(nukeProgressProvider.notifier).state = percentage;
      ref.read(nukeTargetProvider.notifier).state = target;
      ref.read(nukeCompletedProvider.notifier).state = completed;
      };

      _telemetryService?.onNukeAborted = () {
        ref.read(nukeProgressProvider.notifier).state = 0.0;
        ref.read(nukeTargetProvider.notifier).state = 'ABORTED';
      };

    _telemetryService?.onSectorChanged = (changedFolder) {
      final currentProgress = ref.read(nukeProgressProvider);
      if (currentProgress > 0.0 && currentProgress < 100.0) {
        return;
      }
      final currentPath = ref.read(directoryProvider).currentPath;

      final normalizedCurrent = currentPath.replaceAll('\\', '/').toLowerCase();
      final normalizedChanged = changedFolder.replaceAll('\\', '/').toLowerCase();

      if(normalizedCurrent == normalizedChanged) {
        print('LIVE UPDATE: REFRESHING UI FOR $currentPath');
        ref.read(directoryProvider.notifier).scanDirectory(currentPath);
      }
      ref.invalidate(driveStatsProvider);
    };
    _telemetryService?.startListening();
  }

  @override
  void dispose() {
    _telemetryService?.stopListening();
    super.dispose();
  }
}

final telemetryProvider = StateNotifierProvider<TelemetryNotifier, TelemetryState>((ref) {
  return TelemetryNotifier(ref);
});
