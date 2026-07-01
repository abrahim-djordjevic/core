import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/widgets/telemetry_history_chart.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';

class TelemetryHistoryScreen extends StatelessWidget {
  const TelemetryHistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: HudTheme.bgBase,
      body: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'TELEMETRY HISTORY',
              style: TextStyle(
                fontFamily: HudTheme.fontCore,
                color: HudTheme.primaryBorder,
                fontSize: 24,
                fontWeight: FontWeight.bold,
                letterSpacing: 3,
              ),
            ),
            const SizedBox(height: 8),
            const Text(
              'SYSTEM-WIDE METRIC TRENDS',
              style: HudTheme.labelMuted,
            ),
            const SizedBox(height: 24),
            Expanded(
              child: ListView(
                children: const [
                  SizedBox(
                    height: 400,
                    child: TelemetryHistoryChart(metricKey: 'cpu'),
                  ),
                  SizedBox(height: 24),
                  SizedBox(
                    height: 400,
                    child: TelemetryHistoryChart(metricKey: 'ram'),
                  ),
                  SizedBox(height: 24),
                  SizedBox(
                    height: 400,
                    child: TelemetryHistoryChart(metricKey: 'thermal_cpu_package'),
                  ),
                  SizedBox(height: 48), // Padding at bottom
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
