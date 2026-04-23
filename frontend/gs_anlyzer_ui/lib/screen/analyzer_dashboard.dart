import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/providers/telemetry_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
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
    ref.watch(telemetryProvider);

    return Scaffold(
        backgroundColor: const Color(0xFF121212),
        appBar: AppBar(
            title: Text(dirState.currentPath, style: const TextStyle(color: Colors.white70, fontFamily: 'Courier', fontWeight: FontWeight.bold)),
            backgroundColor: const Color(0xFF1E1E1E),
            elevation: 0,
            bottom: PreferredSize(
                preferredSize: const Size.fromHeight(60),
                child: Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: const DirectorySearchWidget(),
                )
            )
        ),
        body: Row(
          children: [
            // LEFT PANEL: Persistent Tree
            SideBarTreeWidget(
              onNuke: (name, path) => executeNukeProtocol(context, ref, name, path),
            ),
            // RIGHT PANEL: Directory Table
            Expanded(
              child: Column(
                children: [
                  Expanded(
                    child: dirState.isLoading
                        ? const TelemetryHudWidget()
                        : dirState.errorMessage != null
                        ? Center(child: Text('BRIDGE FAILURE: ${dirState.errorMessage}', style: const TextStyle(color: Colors.red)))
                        : Column(
                      children: [
                        DirectoryTableHeader(),
                        if (dirState.currentPath != 'C:/' && dirState.searchQuery.isEmpty)
                          GoUpRowWidget(),
                        Expanded(
                          child: dirState.displayNodes.isEmpty && dirState.searchQuery.isNotEmpty
                              ? const Center(
                            child: Text('NO DATA FOUND IN SECTOR', style: TextStyle(color: Colors.white54)),
                          )
                              : ListView.builder(
                            itemCount: dirState.displayNodes.length,
                            itemBuilder: (context, index) {
                              return DirectoryNodeWidget(
                                node: dirState.displayNodes[index],
                                apiService: ApiService(),
                                onNuke: (name, path) => executeNukeProtocol(context, ref, name, path),
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
