import 'package:gs_analyzer_ui/models/storage_node.dart';

class MockFactory {
  static Map<String, dynamic> createFakeNodeMap({
    required String name,
    required String path,
    bool isDirectory = false,
    int size = 1024,
}) {
    return {
      'name': name,
      'path': path,
      'type': isDirectory ? 'Directory' : 'File',
      'sizeBytes': size,
      'lastModified': DateTime.now().toIso8601String(),
    };
    }

    static List<Map<String, dynamic>> generateFakeChunk(int count, String basePath) {
    return List.generate(count, (index) {
      return createFakeNodeMap(
      name: 'item_$index.bin',
      path: '$basePath/item_$index.bin',
      size: (index + 1) * 1000,
      );
      });
  }

  static List<Map<String, dynamic>> generateNukeTarget(int count, String targetPath) {
    return List.generate(count, (index) {
      return {
        'name': 'target_item_$index.tmp',
        'path': '$targetPath/target_item_$index.tmp',
        'type': index % 2 == 0 ? 'File' : 'Directory',
        'sizeBytes': 1024 * index,
        'lastModified': DateTime.now().toIso8601String(),
        'status': 'PendingDeletion'
      };
    });
  }

  static Map<String, dynamic> generateRamTelemetry({
    double usedPercentage = 0.5,
    double totalGb = 16.0,
}) {
    return {
      'usedBytes': (totalGb * 1024 * 1024 * 1024 * usedPercentage).toInt(),
      'totalBytes': (totalGb * 1024 * 1024 * 1024).toInt(),
      'usageHistory': List.generate(10, (i) => 20.0 + (i * 5)),
      'timestamp': DateTime.now().toIso8601String(),
    };
  }
}