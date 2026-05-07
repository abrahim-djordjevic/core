import 'package:flutter_riverpod/legacy.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

enum SortMethod {
  name,
  size,
  date,
}

class DirectoryState {
  final String currentPath;
  final List<StorageNode> allNodes;
  final Set<String> selectedPath;
  final String searchQuery;
  final SortMethod sortMethod;
  final bool isAscending;
  final bool isLoading;
  final bool isSelectionMode;
  final String? errorMessage;

  const DirectoryState({
    this.currentPath = 'C:/',
    this.allNodes = const [],
    this.selectedPath = const {},
    this.searchQuery = '',
    this.sortMethod = SortMethod.name,
    this.isAscending = true,
    this.isLoading = true,
    this.isSelectionMode = false,
    this.errorMessage,
});

  DirectoryState copyWith({
  String? currentPath,
  List<StorageNode>? allNodes,
  Set<String>? selectedPath,
  String? searchQuery,
  SortMethod? sortMethod,
  bool? isAscending,
  bool? isLoading,
  bool? isSelectionMode,
  String? errorMessage,
}) {
    return DirectoryState(
      currentPath: currentPath ?? this.currentPath,
      allNodes: allNodes ?? this.allNodes,
      selectedPath: selectedPath ?? this.selectedPath,
      searchQuery: searchQuery ?? this.searchQuery,
      sortMethod: sortMethod ?? this.sortMethod,
      isAscending: isAscending ?? this.isAscending,
      isLoading: isLoading ?? this.isLoading,
      isSelectionMode: isSelectionMode ?? this.isSelectionMode,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }

  List<StorageNode> get displayNodes {
    List<StorageNode> nodes = searchQuery.isEmpty ? List.from(allNodes) : allNodes.where((node) => node.name.toLowerCase().contains(searchQuery.toLowerCase())).toList();

    nodes.sort((a, b) {
      int comparison;

      switch(sortMethod) {
        case SortMethod.name:
          comparison = a.name.toLowerCase().compareTo(b.name.toLowerCase());
          break;
        case SortMethod.size:
          comparison = b.sizeBytes.compareTo(a.sizeBytes);
          break;
        case SortMethod.date:
          comparison = b.lastModified.compareTo(a.lastModified);
          break;
      }
      return isAscending ? comparison : -comparison;
    });

    return nodes;
  }

}

class DirectoryNotifier extends StateNotifier<DirectoryState> {
  final ApiService _apiService = ApiService();

  DirectoryNotifier() : super(const DirectoryState()) {
    scanDirectory('C:/');
  }

  Future<void> scanDirectory(String targetPath) async {
    state = state.copyWith(
      isLoading: true,
      errorMessage: null,
    );
    String safePath = targetPath.replaceAll('\\', '/');
    state = state.copyWith(currentPath: safePath, isLoading: true, searchQuery: '', errorMessage: null, allNodes: []);

    try {
      final nodes = await _apiService.scanDirectory(safePath);

      state = state.copyWith(allNodes: nodes, currentPath: targetPath, isLoading: false);
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

  // Filter and Sort
  void setSortMethod(SortMethod method) {
    state = state.copyWith(sortMethod: method);
  }

  //Search Query
  void updateSearchQuery(String query) {
    state = state.copyWith(searchQuery: query);
  }

  // Direction of sorting
  void setAscending(bool ascending) {
    state = state.copyWith(isAscending: ascending);
  }

  // Select Multiple
  void toggleSelection(String path) {
    final currentSelection = Set<String>.from(state.selectedPath);

    if(currentSelection.contains(path)) {
      currentSelection.remove(path);
    } else {
      currentSelection.add(path);
    }

    state = state.copyWith(selectedPath: currentSelection);
  }

  // Toggle Selection Mode
  void toggleSelectionMode() {
    if (state.isSelectionMode) {
      state = state.copyWith(isSelectionMode: false, selectedPath: {});
    } else {
      state = state.copyWith(isSelectionMode: true);
    }
  }


}

final directoryProvider = StateNotifierProvider<DirectoryNotifier, DirectoryState>((ref) {
  return DirectoryNotifier();
});

final treeExpandedProvider = StateProvider<bool>((ref) => true);