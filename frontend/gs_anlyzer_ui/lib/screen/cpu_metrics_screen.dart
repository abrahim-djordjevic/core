import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:gs_analyzer_ui/providers/cpu_provider.dart';
import 'package:gs_analyzer_ui/models/cpu_snapshot.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';

class CpuMetricsScreen extends ConsumerWidget {
  const CpuMetricsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cpuState = ref.watch(cpuProvider);

    return Padding(
      padding: const EdgeInsets.all(24.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('CPU TELEMETRY MODULE', style: HudTheme.headerCyan),
          const SizedBox(height: 24),
          if(cpuState == null)
            const Expanded(
              child: Center(
                child: CircularProgressIndicator(color: HudTheme.primaryBorder),
              ),
            )
          else
            Expanded(
              child: _buildDashBoard(context, cpuState),
            )
        ],
      )
    );
  }

  Widget _buildDashBoard(BuildContext context, CpuSnapshot snapshot) {
    final isStable = snapshot.delta.abs() <= 5.0;
    final deltaColor = isStable ? HudTheme.accentGreen : HudTheme.accentAmber;
    final detalPrefix = snapshot.delta > 0 ? '+' : '';

    return Container(
      padding: const EdgeInsets.all(24),
      decoration: HudTheme.hudPanelDecoration,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: const [
              Text('CPU UTILIZATION [AVG]', style: HudTheme.labelMuted,),
              Icon(Icons.memory_outlined, color: HudTheme.accentCyan, size: 24,),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            crossAxisAlignment: CrossAxisAlignment.baseline,
            textBaseline: TextBaseline.alphabetic,
            children: [
              Text('${snapshot.averageLoad.toStringAsFixed(1)}%', style: const TextStyle(color: HudTheme.accentCyan, fontSize: 48, fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore)),
            ],
          ),
          const SizedBox(height: 16),
          FittedBox(
            fit: BoxFit.scaleDown,
            child: Text(
              'FREQ → ${snapshot.currentFrequencyGhz} Ghz | PROC → ${snapshot.totalProcesses} | THREADS → ${snapshot.totalThreads} | HANDLES → ${snapshot.totalHandles}',
              style: HudTheme.bodyText,
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              _buildCacheChip('L1', snapshot.l1Cache),
              const SizedBox(width: 8),
              _buildCacheChip('L2', snapshot.l2Cache),
              const SizedBox(width: 8),
              _buildCacheChip('L3', snapshot.l3Cache),
            ],
          ),
          const SizedBox(height: 32),
          Expanded(child: _buildCoreCharts(snapshot.coreGroups)),
        ],
      ),
    );
  }

  Widget _buildCacheChip(String label, String value) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: HudTheme.accentCyan.withValues(alpha: 0.1),
        border: Border.all(color: HudTheme.accentCyan.withValues(alpha: 0.3)),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        '$label: $value',
        style: HudTheme.statGreen.copyWith(color: HudTheme.accentCyan, fontSize: 12),
      ),
    );
  }

  Widget _buildCoreCharts(Map<String, List<double>> coreGroups) {
    List<BarChartGroupData> barGroups = [];
    List<String> groupLabels = [];
    int xIndex = 0;

    for (var entry in coreGroups.entries) {
      groupLabels.add(entry.key);
      List<BarChartRodData> rods = [];
      for (var coreLoad in entry.value) {
        rods.add(
          BarChartRodData(
            toY: coreLoad,
            color: coreLoad > 80.0 ? HudTheme.accentAmber : HudTheme.accentCyan,
            width: 12,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(2)),
          ),
        );
      }
      barGroups.add(BarChartGroupData(x: xIndex, barRods: rods, barsSpace: 6));
      xIndex++;
    }

    return BarChart(
      BarChartData(
        maxY: 100,
        minY: 0,
        barGroups: barGroups,
        gridData: FlGridData(
          show: true,
          drawVerticalLine: false,
          getDrawingHorizontalLine: (value) {
            return FlLine(
              color: Colors.white12,
              strokeWidth: 1,
              dashArray: [4, 4]
            );
          },
        ),
        titlesData: FlTitlesData(
          show: true,
          topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
          leftTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
          rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              getTitlesWidget: (double value, TitleMeta meta) {
                if (value.toInt() >= 0 && value.toInt() < groupLabels.length) {
                  return Padding(padding: const EdgeInsets.only(top: 8), child: Text(groupLabels[value.toInt()], style: HudTheme.labelMuted));
                }
                return const SizedBox.shrink();
              },
            ),
          )
        ),
        borderData: FlBorderData(show: false),
      ),
      duration: Duration.zero,
    );
  }
}