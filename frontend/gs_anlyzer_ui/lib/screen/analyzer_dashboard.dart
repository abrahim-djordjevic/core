import 'package:flutter/material.dart';
import 'dart:math';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/models/drive_stats.dart';
import 'package:gs_analyzer_ui/widgets/directory_node_widget.dart';
import 'package:gs_analyzer_ui/widgets/drive_telemetry_widget.dart';
import 'package:gs_analyzer_ui/widgets/telemetry_hud_widget.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';

import '../widgets/side_bar_widget.dart';

String formatBytes(int bytes) {
  if (bytes < 0) return "--";
  if (bytes == 0) return "0 B";
  const suffixes = ["B", "KB", "MB", "GB", "TB"];
  var i = (log(bytes) / log(1024)).floor();
  double val = bytes / pow(1024, i);
  return '${val < 10 && i > 0 ? val.toStringAsFixed(1) : val.toStringAsFixed(0)} ${suffixes[i]}';
}

class AnalyzerDashboard extends ConsumerStatefulWidget {
  const AnalyzerDashboard({super.key});

  @override
  ConsumerState<AnalyzerDashboard> createState() => _AnalyzerDashboardState();
}

class _AnalyzerDashboardState extends ConsumerState<AnalyzerDashboard> {
  final TextEditingController _searchController = TextEditingController();

  void _armNukePop(String fileName, String filePath) {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          backgroundColor: const Color(0xFF1A1A1A),
          shape: RoundedRectangleBorder(
            side: const BorderSide(
              color: Colors.redAccent,
              width: 2,
            ),
            borderRadius: BorderRadius.circular(8),
          ),
          title: const Text(
            'CONFIRM NUKE',
            style: TextStyle(color: Colors.redAccent, fontFamily: 'Courier', fontWeight: FontWeight.bold),
          ),
          content: Text(
            'You are about to permanently nuke this:\n\n$fileName\n\nThis bypass the RecycleBin. There is no go back.',
            style: const TextStyle(
              color: Colors.white70,
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text(
                'ABORT',
                style: TextStyle(
                  color: Colors.white54,
                  fontFamily: 'Courier',
                ),
              ),
            ),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.redAccent.withValues(alpha: 0.2),
                foregroundColor: Colors.redAccent,
                side: const BorderSide(color: Colors.redAccent),
              ),
              onPressed: () async {
                Navigator.of(context).pop();

                try {
                  bool success = await ApiService().nukeNode(filePath);
                  if (success && context.mounted) {
                    final currentPath = ref.read(directoryProvider).currentPath;
                    ref.read(directoryProvider.notifier).scanDirectory(currentPath);
                    
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text(
                          'TARGET NUKE',
                        ),
                        backgroundColor: Colors.redAccent,
                      ),
                    );
                  }
                } catch (e) {
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                      content: Text(
                        'ERROR: $e',
                      ),
                      backgroundColor: Colors.orange,
                    ));
                  }
                }
              },
              child: const Text('NUKE TARGET', style: TextStyle(fontWeight: FontWeight.bold, fontFamily: 'Courier')),
            )
          ],
        );
      },
    );
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final dirState = ref.watch(directoryProvider);
    final dirNotifier = ref.read(directoryProvider.notifier);

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
                  child: _buildSearchBar(dirState, dirNotifier),
                ))),
        body: Row(
          children: [
            // LEFT PANEL: Persistent Tree
            SideBarTreeWidget(
              onNuke: _armNukePop,
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
                        _buildTableHeader(),
                        if (dirState.currentPath != 'C:/' && dirState.searchQuery.isEmpty)
                          _buildGoUpRow(dirNotifier),

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
                                onNuke: _armNukePop,
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
        ));
  }


  Widget _buildTableHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
      decoration: const BoxDecoration(
        color: Color(0xFF1A1A1A),
        border: Border(bottom: BorderSide(color: Colors.white10)),
      ),
      child: const Row(
        children: [
          Expanded(
            flex: 4,
            child: Row(
              children: [
                SizedBox(width: 32), // Arrow space
                SizedBox(width: 20), // Icon space
                SizedBox(width: 8),  // Spacer space
                Text('NAME', style: _headerStyle),
              ],
            ),
          ),
          Expanded(flex: 3, child: Text('DATE MODIFIED', style: _headerStyle)),
          Expanded(flex: 2, child: Text('TYPE', style: _headerStyle)),
          Expanded(
            flex: 2,
            child: Padding(
              padding: EdgeInsets.only(right: 16),
              child: Text('SIZE', style: _headerStyle, textAlign: TextAlign.right),
            ),
          ),
          Expanded(flex: 1, child: Text('ACTION', style: _headerStyle, textAlign: TextAlign.center)),
        ],
      ),
    );
  }

  static const TextStyle _headerStyle = TextStyle(
    color: Colors.white24,
    fontFamily: 'Courier',
    fontSize: 12,
    fontWeight: FontWeight.bold,
    letterSpacing: 1.5,
  );

  Widget _buildGoUpRow(DirectoryNotifier dirNotifier) {
    return InkWell(
      onTap: dirNotifier.navigateUp,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
        decoration: const BoxDecoration(
          border: Border(bottom: BorderSide(color: Colors.white10)),
        ),
        child: const Row(
          children: [
            Expanded(
              flex: 4,
              child: Row(
                children: [
                  SizedBox(width: 32), // Arrow space
                  Icon(Icons.keyboard_return_outlined, color: Colors.cyanAccent, size: 20),
                  SizedBox(width: 8),
                  Text(
                    '[..] GO UP A DIRECTION',
                    style: TextStyle(
                      color: Colors.cyanAccent,
                      fontWeight: FontWeight.bold,
                      fontFamily: 'Courier',
                    ),
                  ),
                ],
              ),
            ),
            Expanded(flex: 3, child: SizedBox()),
            Expanded(flex: 2, child: SizedBox()),
            Expanded(flex: 2, child: SizedBox()),
            Expanded(flex: 1, child: SizedBox()),
          ],
        ),
      ),
    );
  }

  Widget _buildSearchBar(DirectoryState dirState, DirectoryNotifier dirNotifier) {
    return TextField(
      controller: _searchController,
      style: const TextStyle(color: Colors.cyanAccent, fontFamily: 'Courier'),
      decoration: InputDecoration(
        prefixIcon: const Icon(
          Icons.search_outlined,
          color: Colors.white54,
        ),
        suffixIcon: dirState.searchQuery.isNotEmpty
            ? IconButton(
                icon: const Icon(Icons.clear_outlined, color: Colors.redAccent),
                onPressed: () {
                  _searchController.clear();
                  dirNotifier.updateSearchQuery('');
                },
              )
            : null,
        hintText: 'QUERY DIRECTORY....',
        hintStyle: const TextStyle(color: Colors.white24, fontFamily: 'Courier'),
        filled: true,
        fillColor: const Color(0xFF1A1A1A),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide.none,
        ),
      ),
      onChanged: (value) {
        dirNotifier.updateSearchQuery(value);
      },
    );
  }

}
