import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/telemetry_provider.dart';
import 'package:gs_analyzer_ui/utils/globals.dart';
import '../providers/directory_provider.dart';
import '../services/api_service.dart';

class TelemetryHudWidget extends ConsumerWidget {
  const TelemetryHudWidget({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final telemetry = ref.watch(telemetryProvider);
    final double calculateProgress = telemetry.total > 0 ? telemetry.completed / telemetry.total : 0.0;
    final double displayPercentage = calculateProgress * 100;

    return Center(
      child: Container(
        width: 500, // Fixed width
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: const Color(0XFF1E1E1E),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.cyan.withValues(alpha: 0.3)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2, color: Colors.cyan),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Text(
                    '${telemetry.status}...',
                    style: const TextStyle(
                      color: Colors.cyan,
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 2,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),

            if (telemetry.total > 0) ...[
              LinearProgressIndicator(
                value: calculateProgress,
                color: Colors.greenAccent,
                backgroundColor: Colors.white10,
                minHeight: 6,
              ),
              const SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'SECTORS SCANNED: ${telemetry.completed} / ${telemetry.total}',
                    style: const TextStyle(
                      color: Colors.greenAccent,
                      fontSize: 14,
                      fontFamily: 'Courier',
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    '${displayPercentage.toStringAsFixed(1)}%',
                    style: const TextStyle(
                      color: Colors.greenAccent,
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                      fontFamily: 'Courier',
                    ),
                  ),
                ],
              ),
            ] else ...[
              const Text(
                'CALCULATING SECTORS...',
                style: TextStyle(
                  color: Colors.greenAccent,
                  fontSize: 18,
                  fontFamily: 'Courier',
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],

            const SizedBox(height: 16),

            Text(
              'TARGET: ${telemetry.target}',
              style: TextStyle(
                color: Colors.grey[400],
                fontSize: 12,
                fontFamily: 'Courier',
              ),
              overflow: TextOverflow.ellipsis,
              maxLines: 1,
            ),

            const SizedBox(height: 16),

            Align(
              alignment: Alignment.bottomRight,
              child: TextButton(
                onPressed: () async {
                  await ApiService().abortScan();
                  ref.read(directoryProvider.notifier).state = ref.read(directoryProvider).copyWith(isLoading: false);
                  snackbarKey.currentState?.showSnackBar(
                      const SnackBar(
                        content: Text('Scan Aborted', style: TextStyle(fontFamily: 'Courier', fontWeight: FontWeight.bold)),
                        backgroundColor: Colors.orange,
                      )
                  );
                },
                child: const Text(
                  'ABORT SCAN',
                  style: TextStyle(
                    color: Colors.redAccent,
                    fontFamily: 'Courier',
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.5,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
