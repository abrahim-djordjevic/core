import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

final rootTreeProvider = FutureProvider<List<StorageNode>>((ref) async {
  return ApiService().scanDirectory('C:/');
});