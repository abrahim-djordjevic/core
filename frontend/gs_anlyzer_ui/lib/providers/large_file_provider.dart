import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/large_file_model.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class LargeFileState {
  final bool isLoading;
  final List<LargeFileModel> largeFiles;
  final String? errorMessage;

  LargeFileState({
    this.isLoading = false,
    this.largeFiles = const [],
    this.errorMessage,
});
}

class LargeFileNotifier extends StateNotifier<LargeFileState> {
  LargeFileNotifier() : super(LargeFileState());

  Future<void> startScan(String rootPath, int topN) async {
    state = LargeFileState(isLoading: true, largeFiles: []);

    try {
      final apiService = ApiService();
      final rawData = await apiService.scanForLargeFiles(rootPath, topN);

      final parsedFiles = rawData.map((json) => LargeFileModel.fromJson(json)).toList();

      state = LargeFileState(isLoading: false, largeFiles: parsedFiles);
    } catch (e) {
      print('LARGE FILE SCAN CRACHED: $e');
      state = LargeFileState(isLoading: false, largeFiles: [], errorMessage: e.toString());
    }
  }

  void removeNukeFiles(List<String> nukePaths) {
    final updatedList = state.largeFiles
        .where((file) => !nukePaths.contains(file.path))
        .toList();
    state = LargeFileState(isLoading: false, largeFiles: updatedList);
  }
}

final largeFileProvider = StateNotifierProvider<LargeFileNotifier, LargeFileState>((ref) => LargeFileNotifier());