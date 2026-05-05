import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';
import 'package:http/http.dart' as http;

import '../services/api_service.dart';

class RamState {
  final List<ProcessTelemetry> processes;
  final bool isLoading;
  final double totalSytemsRamMb = 16384.0;

  const RamState({
    this.processes = const [],
    this.isLoading = true,
  });

  RamState copyWith({List<ProcessTelemetry>? processes, bool? isLoading}) {
    return RamState(
      processes: processes ?? this.processes,
      isLoading: isLoading ?? this.isLoading,
    );
  }
}

class RamNotifier extends StateNotifier<RamState> {
  final ApiService _apiService = ApiService();

  RamNotifier() : super(const RamState());

  void updateProcesses(List<dynamic> rawList) {
    final parsed = rawList.map((json) => ProcessTelemetry.fromJson(json, state.totalSytemsRamMb)).toList();

    parsed.sort((a, b) => b.ramMb.compareTo(a.ramMb));

    state = state.copyWith(processes: parsed, isLoading: false);
  }

  Future<void> killProcess(int pid) async {
    try {
      state = state.copyWith(
        processes: state.processes.where((p) => p.pid != pid).toList()
      );

      await _apiService.killRamProcess(pid);

      print('EXECUTE ORDER 66 ON PID: $pid - SUCCESS');
    } catch (e) {
      print('Failed to kill process: $e');
    }
  }
}

final ramProvider = StateNotifierProvider<RamNotifier, RamState>((ref) {
  return RamNotifier();
});