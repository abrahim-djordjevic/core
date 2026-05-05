import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';
import 'package:http/http.dart' as http;

import '../services/api_service.dart';

class RamState {
  final List<ProcessTelemetry> processes;
  final bool isLoading;
  final double activeGb;
  final double cacheGb;
  final double swapGb;
  final double totalGb;

  const RamState({
    this.processes = const [],
    this.isLoading = true,
    this.activeGb = 0.0,
    this.cacheGb = 0.0,
    this.swapGb = 0.0,
    this.totalGb = 16.0, // fix: hard coded total ram size
  });

  RamState copyWith({List<ProcessTelemetry>? processes, bool? isLoading, double? activeGb, double? cacheGb, double? swapGb, double? totalGb}) {
    return RamState(
      processes: processes ?? this.processes,
      isLoading: isLoading ?? this.isLoading,
      activeGb: activeGb ?? this.activeGb,
      cacheGb: cacheGb ?? this.cacheGb,
      swapGb: swapGb ?? this.swapGb,
      totalGb: totalGb ?? this.totalGb,
    );
  }
}

class RamNotifier extends StateNotifier<RamState> {
  final ApiService _apiService = ApiService();

  RamNotifier() : super(const RamState())
  {
    _apiService.startRamRadar();
  }

  void updateProcesses(Map<String, dynamic> payload) {
    final global = payload['global'] ?? payload['Global'] ?? {};

    print('🦅 MATRIX RADAR - Global Memory Payload: $global');

    double parseGb(String lowerKey, String upperKey, double fallback) {
      final val = global[lowerKey] ?? global[upperKey] ?? fallback;
      return (val as num).toDouble();
    }
    final totalGb = parseGb('totalGb', 'TotalGb', 16.0);
    final totalMb = totalGb * 1024.0;

    final rawProcesses = payload['processes'] as List<dynamic>? ?? [];
    final parsed = rawProcesses.map((p) => ProcessTelemetry.fromJson(p, totalMb)).toList();

    parsed.sort((a, b) => b.ramMb.compareTo(a.ramMb));

    state = state.copyWith(processes: parsed, isLoading: false, activeGb: (global['activeGb'] ?? 0.0).toDouble(), cacheGb: (global['cacheGb'] ?? 0.0).toDouble(), swapGb: (global['swapGb'] ?? 0.0).toDouble(), totalGb: totalGb);
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