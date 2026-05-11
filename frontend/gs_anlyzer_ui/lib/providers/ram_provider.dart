import 'dart:io';

import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';

import '../services/api_service.dart';

class RamState {
  final List<ProcessGroup> groupedProcesses;
  final bool isLoading;
  final double activeGb;
  final double cacheGb;
  final double swapGb;
  final double totalGb;
  final List<double> usageHistory;

  const RamState({
    this.groupedProcesses = const [],
    this.isLoading = true,
    this.activeGb = 0.0,
    this.cacheGb = 0.0,
    this.swapGb = 0.0,
    this.totalGb = 16.0, // fix: hard coded total ram size
    this.usageHistory = const [],
  });

  double get usedPercentage => totalGb > 0 ? activeGb / totalGb : 0.0;
  String get displayedString => '${activeGb.toStringAsFixed(2)} GB / ${totalGb.toStringAsFixed(2)} GB';
  bool get isCritical => totalGb > 0 && (activeGb / totalGb) >= 0.90;

  RamState copyWith({List<ProcessGroup>? groupedProcesses, bool? isLoading, double? activeGb, double? cacheGb, double? swapGb, double? totalGb, List<double>? usageHistory,}) {
    return RamState(
      groupedProcesses: groupedProcesses ?? this.groupedProcesses,
      isLoading: isLoading ?? this.isLoading,
      activeGb: activeGb ?? this.activeGb,
      cacheGb: cacheGb ?? this.cacheGb,
      swapGb: swapGb ?? this.swapGb,
      totalGb: totalGb ?? this.totalGb,
      usageHistory: usageHistory ?? this.usageHistory,
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

    // print('🦅 MATRIX RADAR - Global Memory Payload: $global');

    double parseGb(String lowerKey, String upperKey, double fallback) {
      final val = global[lowerKey] ?? global[upperKey] ?? fallback;
      return (val as num).toDouble();
    }
    final totalGb = parseGb('totalGb', 'TotalGb', 16.0);
    final totalMb = totalGb * 1024.0;

    final rawProcesses = payload['processes'] as List<dynamic>? ?? [];
    final parsed = rawProcesses.map((p) => ProcessTelemetry.fromJson(p, totalMb)).toList();

    final Map<String, List<ProcessTelemetry>> groupsMap = {};
    for (var p in parsed) {
      if (!groupsMap.containsKey(p.name)) {
        groupsMap[p.name] = [];
      }
      groupsMap[p.name]!.add(p);
    }

    final groupedList = groupsMap.entries
        .map((e) => ProcessGroup(name: e.key, processes: e.value)).toList();

    groupedList.sort((a, b) => b.totalRamMb.compareTo(a.totalRamMb));

    //
    // state = state.copyWith(groupedProcesses: groupedList, isLoading: false, activeGb: (global['activeGb'] ?? 0.0).toDouble(), cacheGb: (global['cacheGb'] ?? 0.0).toDouble(), swapGb: (global['swapGb'] ?? 0.0).toDouble(), totalGb: totalGb);

    final parsedActiveGb = (global['activeGb'] ?? 0.0).toDouble();

    final currentPercentage = totalGb > 0 ? parsedActiveGb / totalGb : 0.0;
    final updatedHistory = List<double>.from(state.usageHistory)..add(currentPercentage);

    if (updatedHistory.length > 20) {
      updatedHistory.removeAt(0);
    }

    state = state.copyWith(
      groupedProcesses: groupedList,
      isLoading: false,
      activeGb: parsedActiveGb,
      cacheGb: (global['cacheGb'] ?? 0.0).toDouble(),
      swapGb: (global['swapGb'] ?? 0.0).toDouble(),
      totalGb: totalGb,
      usageHistory: updatedHistory,
    );
  }

  Future<void> killProcess(int pid) async {
    try {
      state = state.copyWith(groupedProcesses: state.groupedProcesses.map((group) {
        return ProcessGroup(
          name: group.name,
          processes: group.processes.where((process) => process.pid != pid).toList(),
        );
      }).where((group) => group.processes.isNotEmpty).toList(),
      );

      await _apiService.killRamProcesses([pid]);

      print('EXECUTE ORDER 66 ON PID: $pid - SUCCESS');
    } catch (e) {
      print('Failed to kill process: $e');
    }
  }

  Future<void> killProcessGroup(String groupName) async {
    try {
      final targetGroup = state.groupedProcesses.firstWhere((g) => g.name == groupName);
      final pidsToKill = targetGroup.processes.map((p) => p.pid).toList();

      state = state.copyWith(
        groupedProcesses: state.groupedProcesses.where((g) => g.name != groupName).toList()
      );

      await _apiService.killRamProcesses(pidsToKill);
    } catch (e) {
      print('Failed to kill process group: $e');
    }
  }
}

final ramProvider = StateNotifierProvider<RamNotifier, RamState>((ref) {
  return RamNotifier();
});