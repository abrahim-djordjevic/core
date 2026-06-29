import 'package:flutter_riverpod/legacy.dart';

enum StorageMode {
  diskAnalyzer,
  duplicateScanner,
  largeFileScanner,
  tempFileCleaner
}

final storageModeProvider = StateProvider<StorageMode>((ref) => StorageMode.diskAnalyzer);