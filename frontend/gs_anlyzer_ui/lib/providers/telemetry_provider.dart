import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';
import 'package:gs_analyzer_ui/services/telemetry_service.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';

import 'cpu_provider.dart';
import 'nuke_provider.dart';

class TelemetryState {
  final String status;
  final int completed;
  final int total;
  final double percentComplete;
  final String target;

  const TelemetryState({
    this.status = 'IDLE',
    this.completed = 0,
    this.total = 0,
    this.percentComplete = 0.0,
    this.target = '',
  });

  TelemetryState copyWith({String? status, int? completed, int? total, double? percentComplete, String? target}) {
    return TelemetryState(
      status: status ?? this.status,
      completed: completed ?? this.completed,
      total: total ?? this.total,
      percentComplete: percentComplete ?? this.percentComplete,
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
    _telemetryService = TelemetryService(onProgressUpdate: (status, completed, total, percentComplete, target) {
      state = state.copyWith(status: status, completed: completed, total: total, percentComplete: percentComplete, target: target);
      },
    );

    _telemetryService?.onRamUpdate = (data) {
      ref.read(ramProvider.notifier).updateProcesses(data);
    };

    _telemetryService?.onCpuUpdate = (data) {
      ref.read(cpuProvider.notifier).updateCpu(data);
    };
    _telemetryService?.onDirectoryChunk = (path, chunk) {
      ref.read(directoryProvider.notifier).receiveStreamChunk(path, chunk);
    };

    _telemetryService?.onDirectoryStreamComplete = (path) {
      ref.read(directoryProvider.notifier).finalizeStream(path);
    };

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
      ref.read(driveStatsProvider.notifier).refresh();
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
