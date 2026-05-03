import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_label.dart';
import 'dart:math';

import 'package:gs_analyzer_ui/utils/hud_theme.dart';


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
            color: HudTheme.bgBase,
            border: Border(top: BorderSide(color: Colors.white10, width: 1)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  HudLabel('CAPACITY (C:)'),
                  Text('${stats.percentageFree}% FREE', style: HudTheme.statGreen,
                  ),
                ],
              ),
              const SizedBox(height: 8),
              LinearProgressIndicator(
                value: usageFraction,
                backgroundColor: Colors.white10,
                color: HudTheme.accentGreen,
                minHeight: 6,
              ),
              const SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  HudLabel('${formatBytes(stats.usedBytes)} USED'),
                  HudLabel('${formatBytes(stats.totalBytes)} TOTAL'),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}