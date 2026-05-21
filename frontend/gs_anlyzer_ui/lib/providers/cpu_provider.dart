import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/cpu_snapshot.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class CpuNotifier extends StateNotifier<CpuSnapshot?> {
  final ApiService _apiService = ApiService();
  CpuNotifier() : super(null) {
    _apiService.startCpuRadar();
  }

  void updateCpu(Map<String, dynamic> payload) {
    try {
      state = CpuSnapshot.fromJson(payload);
    } catch (e) {
      print('CPU PAYLOAD CRASH: $e');
    }
  }
}

final cpuProvider = StateNotifierProvider<CpuNotifier, CpuSnapshot?>((ref) {
  return CpuNotifier();
});