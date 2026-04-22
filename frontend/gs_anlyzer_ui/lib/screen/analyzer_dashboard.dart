import 'package:flutter/material.dart';
import 'dart:math';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/models/drive_stats.dart';
import 'package:gs_analyzer_ui/services/telemetry_service.dart';

String formatBytes(int bytes) {
  if (bytes <= 0) return "--";
  const suffixes = ["B", "KB", "MB", "GB", "TB"];
  var i = (log(bytes) / log(1024)).floor();
  return '${(bytes / pow(1024, i)).toStringAsFixed(1)} ${suffixes[i]}';
}

class AnalyzerDashboard extends StatefulWidget {
  const AnalyzerDashboard({super.key});

  @override
  State<AnalyzerDashboard> createState() => _AnalyzerDashboardState();
}

class _AnalyzerDashboardState extends State<AnalyzerDashboard> {
  final ApiService _apiService = ApiService();
  late Future<List<StorageNode>> _directoryData;
  late Future<DriveStats> _driveStats;

  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  String _currentPath = 'C:/';
  TelemetryService? _telemetryService;
  int _scannedCount = 0;
  String _scanStatus = 'IDLE';
  String _currentTarget = '';

  @override
  void initState() {
    super.initState();

    _telemetryService = TelemetryService(onProgressUpdate: (status, count, target) {
      debugPrint('STATUS: $status | COUNT: $count | TARGET: $target');
      setState(() {
        _scanStatus = status;
        _scannedCount = count;
        _currentTarget = target;
      });
    });

    _telemetryService?.startListening();

    _directoryData = _apiService.scanDirectory(_currentPath);
    _driveStats = _apiService.getDriveTelemetry('C');
  }

  void _navigateTo(String targetPath) {
    String safePath = targetPath.replaceAll('\\', '/');

    setState(() {
      _searchQuery = '';
      _searchController.clear();
      _currentPath = safePath;

      _directoryData = _apiService.scanDirectory(_currentPath);
    });
  }

  void _navigateUp() {
    String normalized = _currentPath.replaceAll('\\', '/');
    if (normalized == 'C:/' || normalized == 'C:' || normalized.isEmpty) return;

    List<String> parts = normalized.split('/');
    parts.removeWhere((part) => part.isEmpty);

    if (parts.length > 1) {
      parts.removeLast();
      String newPath = parts.join('/');
      if (newPath.endsWith(':')) newPath += '/';
      _navigateTo(newPath);
    } else {
      _navigateTo('C:/');
    }
  }

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
                  bool success = await _apiService.nukeNode(filePath);
                  if (success && context.mounted) {
                    _navigateTo(_currentPath);

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
    _telemetryService?.stopListening();
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        backgroundColor: const Color(0xFF121212),
        appBar: AppBar(
            title: Text(_currentPath, style: const TextStyle(color: Colors.white70, fontFamily: 'Courier', fontWeight: FontWeight.bold)),
            backgroundColor: const Color(0xFF1E1E1E),
            elevation: 0,
            bottom: PreferredSize(
                preferredSize: const Size.fromHeight(60),
                child: Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: _buildSearchBar(),
                ))),
        body: Column(
          children: [
            // Directory List
            Expanded(
              child: FutureBuilder<List<StorageNode>>(
                  future: _directoryData,
                  builder: (context, snapshot) {
                    if (snapshot.connectionState == ConnectionState.waiting) {
                      return _buildTelemetryHUD();
                    } else if (snapshot.hasError) {
                      return Center(
                        child: Text(
                          'BRIDGE FAILURE: ${snapshot.error}',
                          style: const TextStyle(color: Colors.red),
                        ),
                      );
                    }

                    // Search Filter Logic
                    final allNodes = snapshot.data ?? [];
                    final displayNodes = allNodes.where((node) {
                      return node.name.toLowerCase().contains(_searchQuery.toLowerCase());
                    }).toList();

                    bool isRoot = _currentPath == 'C:/' || _currentPath == 'C:\\';

                    if (displayNodes.isEmpty && isRoot) {
                      return const Center(
                        child: Text(
                          'NO DATA FOUND IN SECTOR',
                          style: TextStyle(color: Colors.white54),
                        ),
                      );
                    }

                    int itemCount = isRoot ? displayNodes.length : displayNodes.length + 1;

                    return Column(
                      children: [
                        _buildTableHeader(),
                        Expanded(
                          child: ListView.builder(
                            itemCount: itemCount,
                            itemBuilder: (context, index) {
                              if (!isRoot && index == 0) {
                                return _buildGoUpRow();
                              }

                              final node = displayNodes[isRoot ? index : index - 1];
                              return _buildDataRow(node);
                            },
                          ),
                        ),
                      ],
                    );
                  }),
            ),
            // The Telemetry Bar
            _buildTelemetryBar(),
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
          Expanded(flex: 4, child: Text('NAME', style: _headerStyle)),
          Expanded(flex: 3, child: Text('DATE MODIFIED', style: _headerStyle)),
          Expanded(flex: 2, child: Text('TYPE', style: _headerStyle)),
          Expanded(flex: 2, child: Text('SIZE', style: _headerStyle, textAlign: TextAlign.right)),
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

  Widget _buildGoUpRow() {
    return InkWell(
      onTap: _navigateUp,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
        decoration: const BoxDecoration(
          border: Border(bottom: BorderSide(color: Colors.white10)),
        ),
        child: const Row(
          children: [
            Icon(Icons.keyboard_return_outlined, color: Colors.cyanAccent, size: 20),
            SizedBox(width: 12),
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
    );
  }

  Widget _buildDataRow(StorageNode node) {
    final isDir = node.type == 'Directory';
    return InkWell(
      onTap: isDir ? () => _navigateTo(node.path) : null,
      hoverColor: Colors.white.withValues(alpha: 0.05),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
        decoration: const BoxDecoration(
          border: Border(bottom: BorderSide(color: Colors.white10)),
        ),
        child: Row(
          children: [
            // NAME
            Expanded(
              flex: 4,
              child: Row(
                children: [
                  Icon(
                    isDir ? Icons.folder : Icons.insert_drive_file_outlined,
                    color: isDir ? Colors.greenAccent : Colors.white38,
                    size: 20,
                  ),
                  const SizedBox(width: 12),
                  Flexible(
                    child: Text(
                      node.name,
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w500),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),
            // DATE MODIFIED
            Expanded(
              flex: 3,
              child: Text(
                node.lastModified.toString().split('.')[0],
                style: const TextStyle(color: Colors.white54, fontFamily: 'Courier', fontSize: 13),
              ),
            ),
            // TYPE
            Expanded(
              flex: 2,
              child: Text(
                node.type.toUpperCase(),
                style: const TextStyle(color: Colors.white54, fontFamily: 'Courier', fontSize: 12),
              ),
            ),
            // SIZE
            Expanded(
              flex: 2,
              child: Text(
                formatBytes(node.sizeBytes),
                style: const TextStyle(color: Colors.cyanAccent, fontFamily: 'Courier', fontSize: 13),
                textAlign: TextAlign.right,
              ),
            ),
            // ACTION
            Expanded(
              flex: 1,
              child: IconButton(
                icon: Icon(
                  isDir ? Icons.folder_delete_outlined : Icons.delete_forever_outlined,
                  color: Colors.redAccent,
                  size: 20,
                ),
                onPressed: () => _armNukePop(node.name, node.path),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTelemetryHUD() {
    return Center(
      child: Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: const Color(0xFF1E1E1E),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.cyan.withValues(alpha: 0.3)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.cyan,
                  ),
                ),
                const SizedBox(width: 16),
                Text(
                  '$_scanStatus...',
                  style: const TextStyle(
                    color: Colors.cyan,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 2,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            Text(
              'FILES ACQUIRED: ${_scannedCount.toString().replaceAllMapped(RegExp(r"(\d{1,3})(?=(\d{3})+(?!\d))"), (Match m) => "${m[1]},")}',
              style: const TextStyle(
                color: Colors.greenAccent,
                fontSize: 24,
                fontFamily: 'Courier',
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 12),
            Text(
              'TARGET: ${_currentTarget.length > 50 ? "...${_currentTarget.substring(_currentTarget.length - 50)}" : _currentTarget}',
              style: TextStyle(
                color: Colors.grey[400],
                fontSize: 12,
                fontFamily: 'Courier',
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSearchBar() {
    return TextField(
      controller: _searchController,
      style: const TextStyle(color: Colors.cyanAccent, fontFamily: 'Courier'),
      decoration: InputDecoration(
        prefixIcon: const Icon(
          Icons.search_outlined,
          color: Colors.white54,
        ),
        suffixIcon: _searchQuery.isNotEmpty
            ? IconButton(
                icon: const Icon(Icons.clear_outlined, color: Colors.redAccent),
                onPressed: () {
                  _searchController.clear();
                  setState(() {
                    _searchQuery = '';
                  });
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
        setState(() {
          _searchQuery = value;
        });
      },
    );
  }

  Widget _buildTelemetryBar() {
    return FutureBuilder<DriveStats>(
        future: _driveStats,
        builder: (context, snapshot) {
          if (!snapshot.hasData) return const SizedBox.shrink();

          final stats = snapshot.data!;
          final double usageFraction = stats.usedBytes / stats.totalBytes;

          return Container(
            padding: const EdgeInsets.all(20),
            decoration: const BoxDecoration(
              color: Color(0xFF121212),
              border: Border(top: BorderSide(color: Colors.white10, width: 1)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'CAPACITY (C:)',
                      style: TextStyle(color: Colors.white54, fontSize: 12, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      '${stats.percentageFree}% FREE',
                      style: const TextStyle(color: Colors.greenAccent, fontSize: 12, fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
                const SizedBox(
                  height: 8,
                ),
                LinearProgressIndicator(
                  value: usageFraction,
                  backgroundColor: Colors.white10,
                  color: Colors.greenAccent,
                  minHeight: 6,
                ),
                const SizedBox(
                  height: 8,
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      '${formatBytes(stats.usedBytes)} USED',
                      style: const TextStyle(color: Colors.white38, fontSize: 10),
                    ),
                    Text(
                      '${formatBytes(stats.totalBytes)} TOTAL',
                      style: const TextStyle(color: Colors.white38, fontSize: 10),
                    ),
                  ],
                ),
              ],
            ),
          );
        });
  }
}
