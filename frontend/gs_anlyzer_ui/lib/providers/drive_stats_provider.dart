import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/drive_stats.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class DriveStatsState {
  final DriveStats? stats;
  final int diskThresholdPercent;

  DriveStatsState({this.stats, this.diskThresholdPercent = 90});

  bool get isCritical => stats != null && (stats!.usedBytes / stats!.totalBytes) >= (diskThresholdPercent / 100.0);
}

class DriveStatsNotifier extends StateNotifier<DriveStatsState> {
  final Ref ref;
  DriveStatsNotifier(this.ref) : super(DriveStatsState()) {
    _fetchStats();
    _listenToSettings();
  }

  void _listenToSettings() {
    ref.listen(settingsProvider, (previous, next) {
      final newThreshold = next.currentSettings?.alerts.diskThresholdPercent;
      if (newThreshold != null && newThreshold != state.diskThresholdPercent) {
        state = DriveStatsState(stats: state.stats, diskThresholdPercent: newThreshold);
      }
    }, fireImmediately: true);
  }

  Future<void> _fetchStats() async {
    try {
      final stats = await ApiService().getDriveTelemetry('C');
      state = DriveStatsState(stats: stats, diskThresholdPercent: state.diskThresholdPercent);
    } catch (e) {
      print('Failed to fetch drive stats: $e');
    }
  }

  void refresh() => _fetchStats();
}

final driveStatsProvider = StateNotifierProvider<DriveStatsNotifier, DriveStatsState>((ref) {
  return DriveStatsNotifier(ref);
});
