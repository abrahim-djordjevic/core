import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/providers/root_tree_provider.dart';

void executeNukeProtocol(BuildContext context, WidgetRef ref, {String? fileName, String? filePath}) {
  final dirState = ref.read(directoryProvider);
  final dirNotifier = ref.read(directoryProvider.notifier);

  final isBulk = dirState.isSelectionMode && dirState.selectedPath.isNotEmpty;

  if (!isBulk && filePath == null) return;

  final warningText = isBulk
      ? 'WARNING: You are about to permanently delete ${dirState.selectedPath.length} items. This cannot be undone.'
      : 'Warning: You are about to permanently delete "$fileName". This cannot be undone.';

  showDialog(
    context: context,
    builder: (dialogContext) {
      return AlertDialog(
        backgroundColor: const Color(0xFF1A1A1A),
        shape: RoundedRectangleBorder(
          side: const BorderSide(color: Colors.redAccent, width: 2),
          borderRadius: BorderRadius.circular(8),
        ),
        title: const Text('CONFIRM NUKE', style: TextStyle(color: Colors.redAccent, fontFamily: 'Courier', fontWeight: FontWeight.bold)),
        content: Text(warningText, style: const TextStyle(color: Colors.white70)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('ABORT', style: TextStyle(color: Colors.white54, fontFamily: 'Courier')),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.redAccent.withValues(alpha: 0.2),
              foregroundColor: Colors.redAccent,
              side: const BorderSide(color: Colors.redAccent),
            ),
            onPressed: () async {
              final messenger = ScaffoldMessenger.of(context);
              Navigator.pop(dialogContext);

              try {
                bool allSuccess = true;
                if(isBulk) {
                  for (final targetPath in dirState.selectedPath) {
                    bool success = await ApiService().nukeNode(targetPath);
                    if (!success) {
                      allSuccess = false;
                    }
                  }
                  dirNotifier.toggleSelectionMode();
                } else {
                  bool success = await ApiService().nukeNode(filePath!);
                  allSuccess = success;
                }

                if (context.mounted) {
                  await Future.delayed(const Duration(milliseconds: 500));
                  final currentPath = ref.read(directoryProvider).currentPath;
                  await ref.read(directoryProvider.notifier).scanDirectory(currentPath);

                  ref.invalidate(rootTreeProvider);

                  messenger.showSnackBar(
                    SnackBar(
                      content: Text(
                        allSuccess
                            ? (isBulk ? 'ALL TARGETS NUKE SUCCESSFUL' : 'TARGET NUKE SUCCESSFUL')
                            : 'PARTIAL NUKE: Some files were locked by the OS',
                        style: const TextStyle(fontWeight: FontWeight.bold),
                      ),
                      backgroundColor: allSuccess ? Colors.redAccent : Colors.orange,
                    ),
                  );
                }
              } catch (e) {
                if (context.mounted) {
                  messenger.showSnackBar(SnackBar(
                    content: Text('ERROR: $e'),
                    backgroundColor: Colors.orange,
                  ));
                }
              }
            },
            child: const Text('NUKE TARGET', style: TextStyle(fontWeight: FontWeight.bold, fontFamily: 'Courier')),
          )
        ],
      );
    },
  );
}