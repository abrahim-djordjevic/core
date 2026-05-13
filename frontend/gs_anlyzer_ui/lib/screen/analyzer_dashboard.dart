import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/providers/telemetry_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/_directory_search_widget.dart';
import 'package:gs_analyzer_ui/widgets/directory_node_widget.dart';
import 'package:gs_analyzer_ui/widgets/drive_telemetry_widget.dart';
import 'package:gs_analyzer_ui/widgets/go_up_row_widget.dart';
import 'package:gs_analyzer_ui/widgets/telemetry_hud_widget.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import '../utils/nuke_protocol.dart';
import '../widgets/directory_table_header.dart';
import '../widgets/side_bar_widget.dart';
import 'package:gs_analyzer_ui/providers/storage_mode_provider.dart';
import 'package:gs_analyzer_ui/widgets/duplicate_scanner_pannel.dart';

class AnalyzerDashboard extends ConsumerStatefulWidget {
  const AnalyzerDashboard({super.key});

  @override
  ConsumerState<AnalyzerDashboard> createState() => _AnalyzerDashboardState();
}

class _AnalyzerDashboardState extends ConsumerState<AnalyzerDashboard> {

  @override
  Widget build(BuildContext context) {
    final dirState = ref.watch(directoryProvider);
    final dirNotifier = ref.read(directoryProvider.notifier);
    final currentMode = ref.watch(storageModeProvider);
    ref.watch(telemetryProvider);

    return Scaffold(
        backgroundColor: HudTheme.bgBase,
        appBar: AppBar(
            title: Row(
              children: [
                IconButton(
                  icon: Icon(
                    ref.watch(treeExpandedProvider) ? Icons.menu_open_outlined : Icons.menu_outlined,
                    color: HudTheme.accentCyan,
                  ),
                  tooltip: 'Toggle Data Tree',
                  onPressed: () {
                    final notifier = ref.read(treeExpandedProvider.notifier);
                    notifier.state = !notifier.state;
                  },
                ),
                const SizedBox(width: 8),
                Expanded(child: Text(dirState.currentPath,
                  style: HudTheme.bodyText.copyWith(color: HudTheme.textMain, fontWeight: FontWeight.bold),
                  overflow: TextOverflow.ellipsis,
                )
                )
              ],
            ),
            backgroundColor: HudTheme.bgPanel,
            elevation: 0,
            actions: [
              PopupMenuButton<StorageMode>(
                icon: const Icon(Icons.build_circle_outlined, color: HudTheme.accentAmber,),
                tooltip: 'Storage Tools',
                color: HudTheme.bgPanel,
                offset: const Offset(0, 50),
                onSelected: (mode) {
                  ref.read(storageModeProvider.notifier).state = mode;
                },
                itemBuilder: (context) => [
                  PopupMenuItem(
                    value: StorageMode.duplicateScanner,
                    child: Text('DUPLICATE HUNTER', style: HudTheme.bodyText.copyWith(color: HudTheme.accentAmber, fontWeight: FontWeight.bold)),
                  ),
                  PopupMenuItem(
                    value: StorageMode.largeFileScanner,
                    child: Text('LARGE FILE SCANNER', style: HudTheme.bodyText.copyWith(color: HudTheme.accentAmber, fontWeight: FontWeight.bold)),
                  ),
                  PopupMenuItem(
                    value: StorageMode.tempFileCleaner,
                    child: Text('TEMP FILE CLEANER', style: HudTheme.bodyText.copyWith(color: HudTheme.accentAmber, fontWeight: FontWeight.bold)),
                  ),
                ],
              ),
              if (currentMode == StorageMode.diskAnalyzer) ...[
                PopupMenuButton<dynamic>(
                  icon: const Icon(Icons.sort_outlined),
                  tooltip: 'Sort Option',
                  color: HudTheme.bgPanel,
                  onSelected: (value) {
                    if (value is SortMethod) {
                      ref.read(directoryProvider.notifier).setSortMethod(value);
                    } else if (value is bool) {
                      ref.read(directoryProvider.notifier).setAscending(value);
                    }
                  },
                  itemBuilder: (context) =>
                  [
                    CheckedPopupMenuItem(
                      value: SortMethod.name,
                      checked: dirState.sortMethod == SortMethod.name,
                      child: Text('Name', style: HudTheme.bodyText,),
                    ),
                    CheckedPopupMenuItem(
                      value: SortMethod.size,
                      checked: dirState.sortMethod == SortMethod.size,
                      child: Text('Total Size', style: HudTheme.bodyText,),
                    ),
                    CheckedPopupMenuItem(
                      value: SortMethod.date,
                      checked: dirState.sortMethod == SortMethod.date,
                      child: Text('DateModified', style: HudTheme.bodyText,),
                    ),
                    const PopupMenuDivider(),
                    CheckedPopupMenuItem(
                      value: true,
                      checked: dirState.isAscending == true,
                      child: Text('Ascending', style: HudTheme.bodyText,),
                    ),
                    CheckedPopupMenuItem(
                      value: false,
                      checked: dirState.isAscending == false,
                      child: Text('Descending', style: HudTheme.bodyText,),
                    ),
                  ],
                ),
                TextButton(
                  onPressed: () {
                    ref.read(directoryProvider.notifier).toggleSelectionMode();
                  },
                  child: Text(
                    dirState.isSelectionMode ? 'CANCEL SELECTION' : 'SELECT MULTIPLE',
                    style: HudTheme.bodyText.copyWith(color: HudTheme.accentCyan, fontWeight: FontWeight.bold)
                  ),
                ),
                if (dirState.isSelectionMode && dirState.selectedPath.isNotEmpty)
                  IconButton(
                    icon: const Icon(Icons.delete_forever_outlined, color: HudTheme.accentRed),
                    tooltip:'Nuke Selected (${dirState.selectedPath.length})',
                    onPressed: () => executeNukeProtocol(context, ref)
                  ),
                IconButton(
                  icon: const Icon(Icons.refresh_outlined, color: HudTheme.accentCyan),
                  tooltip: 'Refresh',
                  onPressed: () => dirNotifier.scanDirectory(dirState.currentPath)
                ),
              ],
            ],
            bottom: currentMode == StorageMode.diskAnalyzer ? const PreferredSize(
              preferredSize: const Size.fromHeight(60),
              child: Padding(
                padding: const EdgeInsets.all(8.0),
                child: const DirectorySearchWidget(),
              )
            ) : null,
        ),
        body: Row(
          children: [
            // LEFT PANEL: Persistent Tree
            if (currentMode == StorageMode.diskAnalyzer) SideBarTreeWidget(
              onNuke: (name, path) => executeNukeProtocol(context, ref, fileName: name, filePath: path),
            ),
            // RIGHT PANEL: Directory Table
            Expanded(
              child: Column(
                children: [
                  Expanded(
                    child: currentMode == StorageMode.duplicateScanner ? const DuplicateScannerPanel() : currentMode == StorageMode.largeFileScanner ? const Center(child: Text('LARGE FILE SCANNER OFFLINE', style: HudTheme.actionRed)) : currentMode == StorageMode.tempFileCleaner ? const Center(child: Text('TEMP FILE CLEANER OFFLINE', style: HudTheme.actionRed,)) : dirState.isLoading
                        ? const TelemetryHudWidget()
                        : dirState.errorMessage != null
                        ? Center(child: Text('BRIDGE FAILURE: ${dirState.errorMessage}', style: HudTheme.actionRed))
                        : Column(
                      children: [
                        DirectoryTableHeader(),
                        if (dirState.currentPath != 'C:/' && dirState.searchQuery.isEmpty)
                          GoUpRowWidget(),
                        Expanded(
                          child: dirState.displayNodes.isEmpty && dirState.searchQuery.isNotEmpty
                              ? const Center(
                            child: Text('NO DATA FOUND IN SECTOR', style: HudTheme.labelMuted),
                          )
                              : ListView.builder(
                            itemCount: dirState.displayNodes.length,
                            itemBuilder: (context, index) {
                              return DirectoryNodeWidget(
                                node: dirState.displayNodes[index],
                                apiService: ApiService(),
                                onNuke: (name, path) => executeNukeProtocol(context, ref, fileName: name, filePath: path),
                                onNavigate: dirNotifier.scanDirectory,
                                depth: 0,
                              );
                            },
                          ),
                        ),
                      ],
                    ),
                  ),
                  const DriveTelemetryWidget(),
                ],
              ),
            ),
          ],
        )
    );
  }
}
