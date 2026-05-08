import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/utils/hud_label.dart';

class RamScannerScreen extends ConsumerWidget {
  const RamScannerScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ramState = ref.watch(ramProvider);

    return Column(
      children: [
        // Top Dashboard Cards
        Container(
          padding: const EdgeInsets.all(24),
          decoration: const BoxDecoration(
          border: Border(bottom: BorderSide(color: Colors.white10)),
          ),
          child: Row(
            children: [
              Expanded(child: _buildAllocationCard('ACTIVE MEMORY', '${ramState.activeGb.toStringAsFixed(1)} GB', HudTheme.accentCyan, ramState.activeGb / ramState.totalGb)),
              Expanded(child: _buildAllocationCard('CACHE (STANDBY)', '${ramState.cacheGb.toStringAsFixed(1)} GB', HudTheme.accentGreen, ramState.cacheGb / ramState.totalGb)),
              Expanded(child: _buildAllocationCard('SWAP / PAGEFILE', '${ramState.swapGb.toStringAsFixed(1)} GB', HudTheme.accentAmber, ramState.swapGb / (ramState.totalGb * 2))),
            ],
          ),
        ),

        // Live Data Table
        Expanded(
          child: ramState.isLoading && ramState.groupedProcesses.isEmpty ? const Center(child: CircularProgressIndicator(color: HudTheme.primaryBorder,)) : ListView.builder(
            itemCount: ramState.groupedProcesses.length + 1,
            itemBuilder: (context, index) {
              if (index == 0) return _buildTableHeader();

              final group = ramState.groupedProcesses[index - 1];
              // Highlight rows > 10% memory usage
              final isCritical = group.totalPercentMem > 10.0;
              final textColor = isCritical ? HudTheme.accentAmber : HudTheme.textMain;

              final displayName = group.count > 1 ? '${group.name} (x${group.count})' : group.name;

              return Container(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              decoration: BoxDecoration(
                color: isCritical ? HudTheme.accentAmber.withValues(alpha: 0.05) : Colors.transparent,
                border: const Border(bottom: BorderSide(color: Colors.white10)),
                ),
                child: Row(
                  children: [
                    Expanded(flex: 2, child: Text(group.count > 1 ? 'GROUPED' : group.primaryPid.toString(), style: HudTheme.bodyText.copyWith(color: HudTheme.textDim))),
                    Expanded(flex: 4, child: Text(displayName, style: HudTheme.bodyText.copyWith(color: textColor, fontWeight: FontWeight.bold))),
                    Expanded(child: Text('SYS_ADMIN', style: HudTheme.bodyText.copyWith(color: HudTheme.textDim))),
                    Expanded(flex: 2, child: Text('${group.totalPercentMem.toStringAsFixed(1)}%', style: HudTheme.statGreen.copyWith(color: textColor), textAlign: TextAlign.right)),
                    Expanded(flex: 2, child: Text('${group.totalRamMb.toStringAsFixed(1)} MB', style: HudTheme.statGreen.copyWith(color: textColor), textAlign: TextAlign.right)),
                    Expanded(
                      flex: 1,
                      child: IconButton(
                        icon: const Icon(Icons.cancel_outlined, color: HudTheme.accentRed, size: 20,),
                        tooltip: 'Kill Process',
                        onPressed: () {
                          if (group.count > 1) {
                            ref.read(ramProvider.notifier).killProcess(
                                group.primaryPid);
                          } else {
                            ref.read(ramProvider.notifier).killProcessGroup(
                                group.name);
                          }
                        }
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _buildTableHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
      decoration: BoxDecoration(color: HudTheme.bgPanel, border: const Border(bottom: BorderSide(color: Colors.white10))),
      child: const Row(
        children: [
          Expanded(flex: 2, child: HudLabel('PID')),
          Expanded(flex: 4, child: HudLabel('COMMAND')),
          Expanded(flex: 3, child: HudLabel('USER')),
          Expanded(flex: 2, child: HudLabel('%MEM', textAlign: TextAlign.right)),
          Expanded(flex: 2, child: HudLabel('MB', textAlign: TextAlign.right)),
          Expanded(flex: 1, child: HudLabel('ACTION', textAlign: TextAlign.right)),
        ],
      ),
    );
  }

  Widget _buildAllocationCard(String title, String subtitle, Color accentColor, double percentage) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: HudTheme.hudPanelDecoration,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          HudLabel(title),
          const SizedBox(height: 8),
          Text(subtitle, style: TextStyle(color: accentColor, fontSize: 16, fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore)),
          const SizedBox(height: 12),
          LinearProgressIndicator(
            value: percentage,
            color: accentColor,
            backgroundColor: Colors.white10,
            minHeight: 4,
          ),
        ],
      ),
    );
  }
}