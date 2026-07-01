import 'dart:io';

void main() {
  final dir = Directory('lib');
  final files = dir.listSync(recursive: true).whereType<File>().where((f) => f.path.endsWith('.dart') && !f.path.contains('logger.dart'));

  for (final file in files) {
    var content = file.readAsStringSync();
    
    if (content.contains('print(')) {
      content = content.replaceAll('print(', 'appLogger.i(');
      
      // Add import if not present
      if (!content.contains("import 'package:gs_analyzer_ui/utils/logger.dart';")) {
        content = "import 'package:gs_analyzer_ui/utils/logger.dart';\n" + content;
      }
      
      file.writeAsStringSync(content);
      print('Updated \${file.path}');
    }
  }
}
