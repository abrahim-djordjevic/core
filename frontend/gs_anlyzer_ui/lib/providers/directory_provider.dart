import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

class DirectoryState {
  final String currentPath;
  final List<StorageNode> allNodes;
  final String searchQuery;
  final bool isLoading;
  final String? errorMessage;

  const DirectoryState({
    this.currentPath = 'C:/',
    this.allNodes = const [],
    this.searchQuery = '',
    this.isLoading = true,
    this.errorMessage,
});

  DirectoryState copyWith({
  String? currentPath,
  List<StorageNode>? allNodes,
  String? searchQuery,
  bool? isLoading,
  String? errorMessage,
}) {
    return DirectoryState(
      currentPath: currentPath ?? this.currentPath,
      allNodes: allNodes ?? this.allNodes,
      searchQuery: searchQuery ?? this.searchQuery,
      isLoading: isLoading ?? this.isLoading,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }

  List<StorageNode> get displayNodes {
    if(searchQuery.isEmpty) return allNodes;
    return allNodes.where((node) => node.name.toLowerCase().contains(searchQuery.toLowerCase())).toList();
  }
}

class DirectoryNotifier extends StateNotifier<DirectoryState> {
  final ApiService _apiService = ApiService();

  DirectoryNotifier() : super(const DirectoryState()) {
    scanDirectory('C:/');
  }

  Future<void> scanDirectory(String targetPath) async {
    String safePath = targetPath.replaceAll('\\', '/');
    state = state.copyWith(currentPath: safePath, isLoading: true, searchQuery: '', errorMessage: null, allNodes: []);

    try {
      final nodes = await _apiService.scanDirectory(safePath);

      state = state.copyWith(allNodes: nodes, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false, errorMessage: e.toString());
    }
  }
  // Navigate to previous Directory
  void navigateUp() {
    String normalized = state.currentPath.replaceAll('\\', '/');
    if(normalized == 'C:/' || normalized == 'C:' || normalized.isEmpty) return;

    List<String> parts = normalized.split('/');
    parts.removeWhere((part) => part.isEmpty);

    if (parts.length > 1) {
      parts.removeLast();
      String newPath = parts.join('/');
      if(newPath.endsWith(':')) newPath += '/';
      scanDirectory(newPath);
    } else {
      scanDirectory('C:/');
    }
  }

  // Search Filter
  void updateSearchQuery(String query) {
    state = state.copyWith(searchQuery: query);
  }
}

final directoryProvider = StateNotifierProvider<DirectoryNotifier, DirectoryState>((ref) {
  return DirectoryNotifier();
});