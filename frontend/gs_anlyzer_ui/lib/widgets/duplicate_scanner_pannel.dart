import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/duplicate_provider.dart';
import 'package:gs_analyzer_ui/providers/storage_mode_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';

import '../utils/nuke_protocol.dart';

class DuplicateScannerPanel extends ConsumerWidget {
  const DuplicateScannerPanel({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dupState = ref.watch(duplicateProvider);
    final dupNotifier = ref.read(duplicateProvider.notifier);

    return Container(
      color: HudTheme.bgBase,
      child: Column(
        children: [
          // Action Screen Header
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            decoration: const BoxDecoration(
              color: HudTheme.bgPanel,
              border: Border(bottom: BorderSide(color: Colors.white10)),
            ),
            child: Row(
              children: [
                const Icon(Icons.difference_outlined, color: HudTheme.accentAmber,),
                const SizedBox(width: 12),
                const Expanded(
                  child: Text('DUPLICATE HUNTER PROTOCOL', style: TextStyle(color: HudTheme.accentAmber, fontFamily: HudTheme.fontCore, fontSize: 16, fontWeight: FontWeight.bold),),
                ),
                // The back button to close the action layer
                TextButton.icon(
                    onPressed: () {
                      ref.read(storageModeProvider.notifier).state = StorageMode.diskAnalyzer;
                    },
                    icon: const Icon(Icons.close, color: HudTheme.textDim),
                    label: Text('CLOSE TOOL', style: HudTheme.bodyText,)
                )
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.all(16),
            decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: Colors.white10))),
            child: Row(
              children: [
                ElevatedButton.icon(
                  style: ElevatedButton.styleFrom(backgroundColor: HudTheme.accentAmber, foregroundColor: Colors.black),
                  icon: const Icon(Icons.radar_outlined),
                  label: const Text('SCAN C:/ FOR DUPLICATES', style: TextStyle(fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore)),
                  onPressed: () => dupNotifier.startScan('C:/'),
                ),
                const Spacer(),

                if (dupState.duplicateGroups.isNotEmpty) ...[
                  Text('WASTED SPACE: ${dupState.totalWastedSpaceFormatted}', style: HudTheme.actionRed,),
                  const SizedBox(width: 24),
                  OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(foregroundColor: HudTheme.accentCyan, side: const BorderSide(color: HudTheme.accentCyan)),
                    icon: const Icon(Icons.auto_fix_high),
                    label: const Text('SMART SELECT (KEEP OLDEST)', style: TextStyle(fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore)),
                    onPressed: () => dupNotifier.smartSelectAll(),
                  ),
                  const SizedBox(width: 12),
                  ElevatedButton.icon(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: HudTheme.accentRed.withValues(alpha: 0.2),
                      foregroundColor: HudTheme.accentRed,
                      side: const BorderSide(color: HudTheme.accentRed)
                    ),
                    icon: const Icon(Icons.delete_forever),
                    label: Text('NUKE SELECTED (${dupState.pathsToNuke.length})', style: const TextStyle(fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore),
                  ),
                    onPressed: () => print('Target to nuke: ${dupState.pathsToNuke}')
                  ),

                ]
              ],
            ),
          ),
          Expanded(
            child: dupState.isLoading
              ? const Center(child: CircularProgressIndicator(color: HudTheme.accentAmber))
              : dupState.duplicateGroups.isEmpty
                ? Center(child: Text('AWAITING BACKEND SCAN COMMAND...', style: HudTheme.bodyText.copyWith(color: HudTheme.textDim)))
                : ListView.builder(
                    itemCount: dupState.duplicateGroups.length,
                    itemBuilder: (context, index) {
                      final group = dupState.duplicateGroups[index];
                      return ExpansionTile(
                        collapsedIconColor: HudTheme.accentAmber,
                        iconColor: HudTheme.accentAmber,
                        title: Text('GROUP HASH: ${group.fileHash.substring(0, 12)}...', style: const TextStyle(color: HudTheme.accentAmber, fontFamily: HudTheme.fontCore, fontWeight: FontWeight.bold),
                        ),
                        subtitle: Text('Wasted Space: ${(group.wastedSizeBytes / (1024 * 1024)).toStringAsFixed(2)} MB', style: const TextStyle(color: HudTheme.accentRed, fontFamily: HudTheme.fontCore)),
                        children: group.files.map((file) {
                            return CheckboxListTile(
                              activeColor: HudTheme.accentRed,
                                checkColor: Colors.black,
                              title: Text(file.path, style: const TextStyle(color: Colors.white70, fontSize: 12)),
                              subtitle: Text('Modified: ${file.lastModified.toString().split('.')[0]}', style: const TextStyle(color: Colors.white38, fontSize: 11)),
                              value: file.isSelected,
                              onChanged: (bool? value) {
                                dupNotifier.toggleFileSelection(group.fileHash, file.path);
                              },
                            );
                        }).toList(),
                      );
                    }
            )

          )
        ],
      ),
    );
  }
}