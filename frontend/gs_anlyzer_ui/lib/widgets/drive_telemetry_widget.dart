import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'dart:math';


String formatBytes(int bytes) {
  if (bytes < 0) return "--";
  if (bytes == 0) return "0 B";
  const suffixes = ["B", "KB", "MB", "GB", "TB"];
  var i = (log(bytes) / log(1024)).floor();
  double val = bytes / pow(1024, i);
  return '${val < 10 && i > 0 ? val.toStringAsFixed(1) : val.toStringAsFixed(0)} ${suffixes[i]}';
}

class DriveTelemetryWidget extends ConsumerWidget {
  const DriveTelemetryWidget({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final driveStatsAsync = ref.watch(driveStatsProvider);

    return driveStatsAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (err, stack) => const SizedBox.shrink(),
      data: (stats) {
        final double usageFraction = stats.usedBytes / stats.totalBytes;

        return Container(
          padding: const EdgeInsets.all(20),
          decoration: const BoxDecoration(
            color: Color(0xFF121212),
            border: Border(top: BorderSide(color: Colors.white10, width: 1)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('CAPACITY (C:)', style: TextStyle(color: Colors.white54, fontSize: 12, fontWeight: FontWeight.bold)),
                  Text('${stats.percentageFree}% FREE', style: const TextStyle(color: Colors.greenAccent, fontSize: 12, fontWeight: FontWeight.bold)),
                ],
              ),
              const SizedBox(height: 8),
              LinearProgressIndicator(
                value: usageFraction,
                backgroundColor: Colors.white10,
                color: Colors.greenAccent,
                minHeight: 6,
              ),
              const SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('${formatBytes(stats.usedBytes)} USED', style: const TextStyle(color: Colors.white38, fontSize: 10)),
                  Text('${formatBytes(stats.totalBytes)} TOTAL', style: const TextStyle(color: Colors.white38, fontSize: 10)),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}