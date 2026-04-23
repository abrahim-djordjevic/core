import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

void executeNukeProtocol(BuildContext context, WidgetRef ref, String fileName, String filePath) {
  showDialog(
    context: context,
    builder: (BuildContext context) {
      return AlertDialog(
        backgroundColor: const Color(0xFF1A1A1A),
        shape: RoundedRectangleBorder(
          side: const BorderSide(
            color: Colors.redAccent,
            width: 2,
          ),
          borderRadius: BorderRadius.circular(8),
        ),
        title: const Text(
          'CONFIRM NUKE',
          style: TextStyle(color: Colors.redAccent, fontFamily: 'Courier', fontWeight: FontWeight.bold),
        ),
        content: Text(
          'You are about to permanently nuke this:\n\n$fileName\n\nThis bypass the RecycleBin. There is no go back.',
          style: const TextStyle(
            color: Colors.white70,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text(
              'ABORT',
              style: TextStyle(
                color: Colors.white54,
                fontFamily: 'Courier',
              ),
            ),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.redAccent.withValues(alpha: 0.2),
              foregroundColor: Colors.redAccent,
              side: const BorderSide(color: Colors.redAccent),
            ),
            onPressed: () async {
              Navigator.of(context).pop();

              try {
                bool success = await ApiService().nukeNode(filePath);
                if (success && context.mounted) {
                  final currentPath = ref.read(directoryProvider).currentPath;
                  ref.read(directoryProvider.notifier).scanDirectory(currentPath);

                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text(
                        'TARGET NUKE',
                      ),
                      backgroundColor: Colors.redAccent,
                    ),
                  );
                }
              } catch (e) {
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                    content: Text(
                      'ERROR: $e',
                    ),
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