import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/temp_cleaner_model.dart';

void main() {
  group('TempLocationPreview.fromJson', () {
    test('parses all fields from camelCase and PascalCase keys', () {
      final loc1 = TempLocationPreview.fromJson({
        'path': 'C:\\temp',
        'sizeBytes': 1024,
        'sizeFormatted': '1 KB',
        'fileCount': 5,
      });

      expect(loc1.path, 'C:\\temp');
      expect(loc1.sizeBytes, 1024);
      expect(loc1.sizeFormatted, '1 KB');
      expect(loc1.fileCount, 5);

      final loc2 = TempLocationPreview.fromJson({
        'Path': 'C:\\cache',
        'SizeBytes': 2048,
        'SizeFormatted': '2 KB',
        'FileCount': 10,
      });

      expect(loc2.path, 'C:\\cache');
      expect(loc2.sizeBytes, 2048);
      expect(loc2.sizeFormatted, '2 KB');
      expect(loc2.fileCount, 10);
    });

    test('applies safe defaults', () {
      final loc = TempLocationPreview.fromJson({});
      
      expect(loc.path, '');
      expect(loc.sizeBytes, 0);
      expect(loc.sizeFormatted, '');
      expect(loc.fileCount, 0);
    });
  });

  group('TempPreviewResponse.fromJson', () {
    test('parses full response', () {
      final response = TempPreviewResponse.fromJson({
        'totalBytes': 3072,
        'totalFormatted': '3 KB',
        'locations': [
          {
            'path': 'C:\\temp',
            'sizeBytes': 1024,
            'sizeFormatted': '1 KB',
            'fileCount': 5,
          },
          {
            'path': 'C:\\cache',
            'sizeBytes': 2048,
            'sizeFormatted': '2 KB',
            'fileCount': 10,
          }
        ]
      });

      expect(response.totalBytes, 3072);
      expect(response.totalFormatted, '3 KB');
      expect(response.locations.length, 2);
      expect(response.locations[0].path, 'C:\\temp');
      expect(response.locations[1].path, 'C:\\cache');
    });

    test('handles empty locations', () {
      final response = TempPreviewResponse.fromJson({
        'totalBytes': 0,
        'totalFormatted': '0 B',
        'locations': []
      });

      expect(response.totalBytes, 0);
      expect(response.totalFormatted, '0 B');
      expect(response.locations, isEmpty);
    });
  });

  group('TempCleanResult.fromJson', () {
    test('parses all fields', () {
      final result = TempCleanResult.fromJson({
        'deletedFiles': 15,
        'freedBytes': 3072,
        'freedFormatted': '3 KB',
        'skippedFiles': 2,
      });

      expect(result.deletedFiles, 15);
      expect(result.freedBytes, 3072);
      expect(result.freedFormatted, '3 KB');
      expect(result.skippedFiles, 2);
    });

    test('applies safe defaults', () {
      final result = TempCleanResult.fromJson({});

      expect(result.deletedFiles, 0);
      expect(result.freedBytes, 0);
      expect(result.freedFormatted, '');
      expect(result.skippedFiles, 0);
    });
  });
}
