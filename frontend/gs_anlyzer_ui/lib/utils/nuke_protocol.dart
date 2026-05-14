import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/providers/root_tree_provider.dart';
import 'package:gs_analyzer_ui/main.dart';
import 'package:gs_analyzer_ui/utils/globals.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';

import '../providers/nuke_provider.dart';
import '../widgets/nuke_progress_dialog.dart';

void executeNukeProtocol(BuildContext context, WidgetRef ref, {String? fileName, String? filePath, List<String>? customPath, VoidCallback? onComplete}) {
  final dirState = ref.read(directoryProvider);
  final dirNotifier = ref.read(directoryProvider.notifier);
  final List<String> targetsToNuke = customPath ?? (dirState.isSelectionMode && dirState.selectedPath.isNotEmpty ? dirState.selectedPath.toList() : (fileName != null ? [filePath!] : []));

  if (targetsToNuke.isEmpty) return;

  final isBulk = targetsToNuke.length > 1;

  final warningText = isBulk
      ? 'WARNING: You are about to permanently delete ${targetsToNuke.length} items. This cannot be undone.'
      : 'Warning: You are about to permanently delete "${fileName ?? 'this target'}". This cannot be undone.';

  showDialog(
    context: context,
    builder: (dialogContext) {
      return AlertDialog(
        backgroundColor: HudTheme.bgPanel,
        shape: RoundedRectangleBorder(
          side: const BorderSide(color: HudTheme.accentRed, width: 2),
          borderRadius: BorderRadius.circular(8),
        ),
        title: const Text('CONFIRM NUKE', style: HudTheme.actionRed),
        content: Text(warningText, style: HudTheme.bodyText),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: Text('ABORT', style: HudTheme.bodyText.copyWith(color: HudTheme.textDim)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: HudTheme.accentRed.withValues(alpha: 0.2),
              foregroundColor: HudTheme.accentRed,
              side: const BorderSide(color: HudTheme.accentRed),
            ),
            onPressed: () async {
              Navigator.pop(dialogContext);
              ref.read(nukeProgressProvider.notifier).state = 0.0;
              ref.read(nukeCompletedProvider.notifier).state = 0;
            ref.read(nukeTargetProvider.notifier).state = 'INITIALIZING OBLITERATION....';


              showDialog(
                context: context,
                barrierDismissible: false,
                builder: (context) => const NukeProgressDialog(),
              );

              final masterNavigator = Navigator.of(context, rootNavigator: true);

              try {
                final api = ApiService();
                bool allSuccess = await api.nukeNode(targetsToNuke);
                masterNavigator.pop();
                
                if (onComplete != null) {
                  onComplete();
                } else {
                  if (dirState.isSelectionMode) {
                    dirNotifier
                      .toggleSelectionMode();
                  }
                  final currentPath = ref
                      .read(directoryProvider)
                      .currentPath;
                  await ref.read(directoryProvider.notifier).scanDirectory(
                      currentPath);
                }

                ref.invalidate(rootTreeProvider);
                ref.invalidate(driveStatsProvider);

                snackbarKey.currentState?.showSnackBar(SnackBar(
                  behavior: SnackBarBehavior.floating,
                  content: Text(
                    allSuccess ? (isBulk ? 'ALL TARGET NUKED SUCCESSFULLY' : 'TARGET NUKED SUCCESSFULLY') : 'PARTIAL NUKE: Some Files were Locked',
                    style: TextStyle(fontFamily: HudTheme.fontCore, fontWeight: FontWeight.bold,),
                  ),
                  backgroundColor: allSuccess ? HudTheme.accentGreen : HudTheme.accentAmber,
                ),
                );
              } catch (e) {
                masterNavigator.pop();
                snackbarKey.currentState?.showSnackBar(SnackBar(content: Text('ERROR: $e', style: TextStyle(fontFamily: HudTheme.fontCore),), backgroundColor: HudTheme.accentRed));
              }
            },
            child: const Text('NUKE TARGET', style: TextStyle(fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore)),
          )
        ],
      );
    },
  );
}