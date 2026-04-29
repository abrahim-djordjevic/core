import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/nuke_provider.dart';

import '../services/api_service.dart';

class NukeProgressDialog extends ConsumerWidget {
  const NukeProgressDialog({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final progress = ref.watch(nukeProgressProvider);
    final target = ref.watch(nukeTargetProvider);
    final completed = ref.watch(nukeCompletedProvider);

    return WillPopScope(
      onWillPop: () async => false,
      child: AlertDialog(
        backgroundColor: const Color(0xFF1A1A1A),
        shape: RoundedRectangleBorder(
          side: const BorderSide(color: Colors.redAccent, width: 2),
          borderRadius: BorderRadius.circular(8),),
        title: const Text('NUKE IN PROGRESS...', style: TextStyle(color: Colors.redAccent, fontFamily: 'Courier', fontWeight: FontWeight.bold)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Target: $target', style: const TextStyle(color: Colors.white70, fontSize: 12), maxLines: 1, overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 8),
            Text('Completed: $completed', style: const TextStyle(color: Colors.white70, fontSize: 12), maxLines: 1, overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 16),
            LinearProgressIndicator(
              value: progress / 100,
              color: Colors.redAccent,
              backgroundColor: Colors.white10,
              minHeight: 8,
            ),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerRight,
              child: Text(
                '${progress.toStringAsFixed(1)}%',
                style: const TextStyle(color: Colors.redAccent, fontFamily: 'Courier', fontWeight: FontWeight.bold),
              ),
            ),
          ],
        ),
        actions: [
          ElevatedButton.icon(
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.redAccent.withValues(alpha: 0.2),
              foregroundColor: Colors.redAccent,
              side: const BorderSide(color: Colors.redAccent),
            ),
            icon: const Icon(Icons.cancel_outlined, color: Colors.redAccent),
            label: const Text('ABORT', style: TextStyle(fontWeight: FontWeight.bold, fontFamily: 'Courier')),
            onPressed: () async {
              await ApiService().abortNuke();
            }
          ),
        ],
      ),
    );

  }
}