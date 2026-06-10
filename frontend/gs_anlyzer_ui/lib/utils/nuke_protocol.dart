
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/providers/drive_stats_provider.dart';
import 'package:gs_analyzer_ui/providers/nuke_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/providers/root_tree_provider.dart';
import 'package:gs_analyzer_ui/utils/globals.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import '../widgets/nuke_preview_dialog.dart';
import '../widgets/nuke_progress_dialog.dart';

Future<void> executeNukeProtocol(BuildContext context, WidgetRef ref, {String? fileName, String? filePath, List<String>? customPath, VoidCallback? onComplete}) async {
  final dirState = ref.read(directoryProvider);
  final dirNotifier = ref.read(directoryProvider.notifier);
  final List<String> targetsToNuke = customPath ?? (dirState.isSelectionMode && dirState.selectedPath.isNotEmpty ? dirState.selectedPath.toList() : (fileName != null ? [filePath!] : []));

  if (targetsToNuke.isEmpty) return;

  final isBulk = targetsToNuke.length > 1;

  ref.invalidate(nukeProgressProvider);
  ref.invalidate(nukeCompletedProvider);
  ref.invalidate(nukeTargetProvider);

  final confirmExecute = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (context) => NukePreviewDialog(targetPaths: targetsToNuke),
  );

  if (confirmExecute != true) {
    print('MATRIX: Nuke Sequence Aborted by System Administrator at Preview');
    return;
  }

  showDialog(
    context: context,
    barrierDismissible: false,
    builder: (context) => NukeProgressDialog(),
  );

  final masterNavigator = Navigator.of(context, rootNavigator: true);
  try {
    final api = ApiService();
    bool allSuccess = await api.executeNuke(targetsToNuke);

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
    ref.read(driveStatsProvider.notifier).refresh();

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
  } finally {
    ref.invalidate(nukeProgressProvider);
    ref.invalidate(nukeCompletedProvider);
    ref.invalidate(nukeTargetProvider);
  }
}