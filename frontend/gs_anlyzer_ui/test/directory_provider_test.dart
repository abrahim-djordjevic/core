import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';

void main() {
  group('DirectoryNotifier Sorting Engine Tests', () {
    test ('Sorting by Size should put largest file at the top (Ascending = true)', () {
      final notifier = DirectoryNotifier();

      final fakeChunk = [
        {
          'name': 'small_file.txt', 'path': 'C:/small_file.txt', 'type': 'File', 'sizeBytes': 100, 'lastModified': DateTime.now().toIso8601String()
        },
        {
          'name': 'big_movie.mp4', 'path': 'C:/big_movie.mp4', 'type': 'File', 'sizeBytes': 5000000, 'lastModified': DateTime.now().toIso8601String()
        },
        {
          'name': 'medium_image.png', 'path': 'C:/medium_image.png', 'type': 'File', 'sizeBytes': 250000, 'lastModified': DateTime.now().toIso8601String()
        }
      ];

      notifier.receiveStreamChunk('C:/', fakeChunk);

      notifier.finalizeStream('C:/');

      notifier.setSortMethod(SortMethod.size);

      final resultList = notifier.state.displayNodes;
      expect(resultList.isNotEmpty, true, reason: 'The display list should not be empty!');

      expect(resultList[0].name, 'big_movie.mp4');
      expect(resultList[1].name, 'medium_image.png');
      expect(resultList[2].name, 'small_file.txt');

    });
  });

  group('Enterprise First-Load Experience Tests', () {

    test('Test 1: No Forever Loading on First Load (isLoading must be false)', () async {
      final notifier = DirectoryNotifier();

      await notifier.scanDirectory('C:/TestSector');

      expect(
          notifier.state.isLoading,
          false,
          reason: 'CRITICAL FAILURE: isLoading remained true. The HUD will spin forever!'
      );
    });

    test('Test 2: Correct Info on First Load (Files show actual size, not 0 bytes)', () {
      final notifier = DirectoryNotifier();

      notifier.state = notifier.state.copyWith(currentPath: 'C:/Downloads');

      final firstLoadChunk = [
        {
          'name': 'Siren-S1E1-1080P.mp4',
          'path': 'C:/Downloads/Siren-S1E1-1080P.mp4',
          'type': 'File',
          'sizeBytes': 250000000, // 250 MB
          'lastModified': DateTime.now().toIso8601String()
        },
      ];

      notifier.receiveStreamChunk('c:/downloads', firstLoadChunk);
      notifier.finalizeStream('c:/downloads');

      final loadedFile = notifier.state.displayNodes.first;

      expect(loadedFile.type, 'File');
      expect(
          loadedFile.sizeBytes,
          250000000,
          reason: 'CRITICAL FAILURE: The file size parsed as 0 bytes on first load!'
      );
    });

    test('Test 3: No Blank Screen on First Load (Data paints without needing a refresh)', () {
      final notifier = DirectoryNotifier();

      notifier.state = notifier.state.copyWith(currentPath: 'C:/Documents');

      final firstLoadChunk = [
        {
          'name': 'PCD_Report.pdf',
          'path': 'C:/Documents/PCD_Report.pdf',
          'type': 'File',
          'sizeBytes': 15000,
          'lastModified': '2026-05-04T22:06:54'
        },
        {
          'name': 'small_file.txt', 'path': 'C:/small_file.txt', 'type': 'File', 'sizeBytes': 100, 'lastModified': DateTime.now().toIso8601String()
        }
      ];

      notifier.receiveStreamChunk('c:/documents', firstLoadChunk);
      notifier.finalizeStream('c:/documents');

      expect(
          notifier.state.displayNodes.isNotEmpty,
          true,
          reason: 'CRITICAL FAILURE: displayNodes is empty! The screen is blank until the user hits refresh!'
      );

      expect(notifier.state.displayNodes.first.name, 'PCD_Report.pdf');
    });

  });
}