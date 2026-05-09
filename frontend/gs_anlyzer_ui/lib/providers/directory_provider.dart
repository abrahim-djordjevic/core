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
  final List<StorageNode> displayNodes;
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
    this.displayNodes = const [],
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
  List<StorageNode>? displayNodes,
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
      displayNodes: displayNodes ?? this.displayNodes,
      selectedPath: selectedPath ?? this.selectedPath,
      searchQuery: searchQuery ?? this.searchQuery,
      sortMethod: sortMethod ?? this.sortMethod,
      isAscending: isAscending ?? this.isAscending,
      isLoading: isLoading ?? this.isLoading,
      isSelectionMode: isSelectionMode ?? this.isSelectionMode,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }
}

class DirectoryNotifier extends StateNotifier<DirectoryState> {
  final ApiService _apiService = ApiService();
  final Map<String, List<StorageNode>> _sectorCache ={};

  DirectoryNotifier() : super(const DirectoryState()) {
    scanDirectory('C:/');
  }
  
  void _applyFiltersAndSort({
    List<StorageNode>? nodes,
    String? searchQuery,
    SortMethod? sortMethod,
    bool? isAscending,
}) {
    final activeNodes = nodes ?? state.allNodes;
    final activeSearchQuery = searchQuery ?? state.searchQuery;
    final activeSortMethod = sortMethod ?? state.sortMethod;
    final activeIsAscending = isAscending ?? state.isAscending;
      
    List<StorageNode> filteredNodes = activeSearchQuery.isEmpty ? List.from(activeNodes) : activeNodes.where((n) => n.name.toLowerCase().contains(activeSearchQuery.toLowerCase())).toList();

    filteredNodes.sort((a, b) {
      int comparison;
      switch(activeSortMethod) {
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
      return activeIsAscending ? comparison : -comparison;
    });

    state = state.copyWith(
      allNodes: activeNodes,
      displayNodes: filteredNodes,
      searchQuery: activeSearchQuery,
      sortMethod: activeSortMethod,
      isAscending: activeIsAscending,
    );
  }

  Future<void> scanDirectory(String targetPath, {bool forceRefresh = false}) async {
    String safePath = targetPath.replaceAll('\\', '/');

    if (!forceRefresh && _sectorCache.containsKey(safePath)) {
      _applyFiltersAndSort(nodes: _sectorCache[safePath]);
      state = state.copyWith(
        currentPath: safePath,
        isLoading: false,
        errorMessage: null,
      );

      _apiService.scanDirectory(safePath).then((freshNodes) {
        _sectorCache[safePath] = freshNodes;
        if (state.currentPath == safePath) _applyFiltersAndSort(nodes: freshNodes);
      }).catchError((_) {});
      return;
    }

    state = state.copyWith(currentPath: safePath, isLoading: true, searchQuery: '', errorMessage: null, allNodes: [], displayNodes: []);

    try {
      final nodes = await _apiService.scanDirectory(safePath);
      _sectorCache[safePath] = nodes;

      state = state.copyWith(isLoading: false);
      _applyFiltersAndSort(nodes: nodes);
    } catch (e) {
      state = state.copyWith(isLoading: false, errorMessage: e.toString());
    }
  }

  Future<List<StorageNode>> fetchChildrenForTree(String path) async {
    String safePath = path.replaceAll('\\', '/');
    if (_sectorCache.containsKey(safePath)) {
      return _sectorCache[safePath]!;
    }
    final nodes = await _apiService.scanDirectory(safePath);
    _sectorCache[safePath] = nodes;
    return nodes;
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
    _applyFiltersAndSort(sortMethod: method);
  }

  //Search Query
  void updateSearchQuery(String query) {
    _applyFiltersAndSort(searchQuery: query);
  }

  // Direction of sorting
  void setAscending(bool ascending) {
    _applyFiltersAndSort(isAscending: ascending);
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