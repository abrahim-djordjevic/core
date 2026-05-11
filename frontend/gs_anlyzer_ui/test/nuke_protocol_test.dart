import 'dart:math';

import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'mock_factory.dart';

void main() {
  group('Nuke Operation Safety Test', () {
    test('Nuke Targeter should correctly identify all items for destruction', () {
     final notifier = DirectoryNotifier();

     notifier.state = notifier.state.copyWith(currentPath: 'C:/Downloads');


     final nukeList = MockFactory.generateNukeTarget(10, 'C:/Downloads');

     notifier.receiveStreamChunk('c:/Downloads', nukeList);

     final targetCount = notifier.state.displayNodes.where((node) => node.name.contains('target_item')).length;

     expect(targetCount, 10, reason: 'The Nuke Targeter missed some files!');
    });
  });

  group('Nuclear Clearance (Recursive Delete', () {
    test('Nuke Targeter should identify a full directory for total removal', () {
      final notifier = DirectoryNotifier();

      notifier.state = notifier.state.copyWith(currentPath: 'C:/Downloads');

      final folderPath = 'C:/Downloads/OldProject';
      final rootFolder = MockFactory.createFakeNodeMap(name: 'OldProject', path: folderPath, isDirectory: true);

      final folderContents = [
        MockFactory.createFakeNodeMap(name: 'file1.doc', path: '$folderPath/file1.doc'),
      MockFactory.createFakeNodeMap(name: 'subFolder', path: '$folderPath/subFolder', isDirectory: true),
      ];

      notifier.receiveStreamChunk('c:/Downloads', [rootFolder]);

      final target = notifier.state.displayNodes.firstWhere((node) => node.name == 'OldProject');

      expect(target.type, 'Directory');
      expect(target.path, folderPath);
    });
  });

  group('Multi-Select Nuke Protocol Tests', () {
    test('Nuke Targeter isolates multiple selected items while keeping unselected items safe', () {
      final notifier = DirectoryNotifier();
      notifier.state = notifier.state.copyWith(currentPath: 'C:/Downloads');

      final mixedChunk = [
      MockFactory.createFakeNodeMap(name: 'keep_me_safe.pdf', path: 'C:/Downloads/keep_me_safe.pdf', size: 5000),
      MockFactory.createFakeNodeMap(name: 'do_not_delete.png', path: 'C:/Downloads/do_not_delete.png', size: 12000),

      MockFactory.createFakeNodeMap(name: 'trash_1.tmp', path: 'C:/Downloads/trash_1.tmp', size: 1024),
      MockFactory.createFakeNodeMap(name: 'trash_2.tmp', path: 'C:/Downloads/trash_2.tmp', size: 1024),
      MockFactory.createFakeNodeMap(name: 'Old_Cache_Folder', path: 'C:/Downloads/Old_Cache_Folder', isDirectory: true)
      ];

      notifier.receiveStreamChunk('c:/downloads', mixedChunk);

      notifier.toggleSelectionMode();

      notifier.toggleSelection('C:/Downloads/trash_1.tmp');
      notifier.toggleSelection('C:/Downloads/trash_2.tmp');
      notifier.toggleSelection('C:/Downloads/Old_Cache_Folder');

      final armedBlastZone = notifier.state.selectedPath;

      expect(
        armedBlastZone.contains('C:/Downloads/keep_me_safe.pdf'), false, reason: 'CRITICAL: The Nuke list grabbed the wrong number of items!'
      );

      expect(armedBlastZone.contains('C:/Downloads/do_not_delete.png'), false, reason: 'CRITICAL FAILURE: do_not_delete.png was targeted for deletion!');

      expect(armedBlastZone.contains('C:/Downloads/trash_1.tmp'), true);
      expect(armedBlastZone.contains('C:/Downloads/trash_2.tmp'), true);
      expect(armedBlastZone.contains('C:/Downloads/Old_Cache_Folder'), true);

    });
  });
}