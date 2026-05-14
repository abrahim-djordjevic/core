import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/duplicate_model.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class DuplicateState {
  final bool isLoading;
  final List<DuplicateGroup> duplicateGroups;

  DuplicateState({this.isLoading = false, this.duplicateGroups = const[]});

  String get totalWastedSpaceFormatted {
    int totalBytes = duplicateGroups.fold(0, (sum, group) => sum + group.wastedSizeBytes);
    if (totalBytes >= 1024 * 1024 * 1024) {
      return '${(totalBytes / (1024 * 1024 * 1024)).toStringAsFixed(2)} GB';
    }
    return '${(totalBytes / (1024 * 1024)).toStringAsFixed(2)} MB';
  }

  List<String> get pathsToNuke {
    return duplicateGroups
        .expand((group) => group.files)
        .where((file) => file.isSelected)
        .map((file) => file.path)
        .toList();
  }
}

class DuplicateNotifier extends StateNotifier<DuplicateState> {
  DuplicateNotifier() : super(DuplicateState());

  Future<void> startScan(String rootPath) async {
    state = DuplicateState(isLoading: true, duplicateGroups: []);

    try {
      final apiService = ApiService();
      final rawData = await apiService.scanForDuplicates(rootPath);

      loadFromBackend(rawData);
    } catch (e) {
      print('DUPLICATE SCAN CRASHED: $e');
      state = DuplicateState(isLoading: false, duplicateGroups: []);
    }
  }

  void loadFromBackend(List<dynamic> jsonList) {
    List<DuplicateGroup> parsedGroups = [];

    for (var item in jsonList) {
      final hash = item['FileHash'] ?? item['fileHash'];
      final List<dynamic> rawPaths = item['FilePaths'] ?? item['filePaths'];
      final List<dynamic> paths = rawPaths;

      final items = paths.map((p) => DuplicateItem.fromPath(p.toString())).toList();

      parsedGroups.add(DuplicateGroup(fileHash: hash, files: items));
    }

    state = DuplicateState(isLoading: false, duplicateGroups: parsedGroups);
  }

  void smartSelectAll() {
    final updateGroups = state.duplicateGroups.map((group) {
      group.files.sort((a, b) => a.lastModified.compareTo(b.lastModified));

      for (int i = 0; i < group.files.length; i++) {
        group.files[i].isSelected = (i != 0);
      }
      return group;
    }).toList();

    state = DuplicateState(isLoading: state.isLoading, duplicateGroups: updateGroups);
  }

  void clearNukedFiles() {
    final updatedGroups = state.duplicateGroups.map((group) {
      group.files.removeWhere((file) => file.isSelected);
      return group;
    }).where((group) => group.files.length > 1).toList();

    state = DuplicateState(isLoading: false, duplicateGroups: updatedGroups);
  }

  void toggleFileSelection(String hash, String path) {
    final updatedGroups = state.duplicateGroups.map((group) {
      if (group.fileHash == hash) {
        for (var file in group.files) {
          if (file.path == path) {
            file.isSelected = !file.isSelected;
          }
        }
      }
      return group;
    }).toList();

    state = DuplicateState(isLoading: state.isLoading, duplicateGroups: updatedGroups);
  }
}

final duplicateProvider = StateNotifierProvider<DuplicateNotifier, DuplicateState>((ref) => DuplicateNotifier());