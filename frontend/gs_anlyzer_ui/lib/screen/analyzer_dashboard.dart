import 'package:flutter/material.dart';
import 'dart:math';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/models/drive_stats.dart';

  String formatBytes(int bytes) {
    if(bytes <= 0) return "--";
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

    @override
    void initState() {
      super.initState();
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

      if(parts.length > 1) {
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
              style: TextStyle(
                color: Colors.redAccent,
                fontFamily: 'Courier',
                fontWeight: FontWeight.bold
              ),
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
                    color: Colors.white54, fontFamily: 'Courier',
                  ),
                ),
              ),
              ElevatedButton(
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.redAccent.withOpacity(0.2),
                  foregroundColor: Colors.redAccent,
                  side: const BorderSide(color: Colors.redAccent),
                ),
                onPressed: () async {
                  Navigator.of(context).pop();

                  try {
                    bool success = await _apiService.nukeNode(filePath);
                    if (success) {
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
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text(
                          'ERROR: $e',
                        ),
                        backgroundColor: Colors.orange,
                      )
                    );
                  }
                },
                child: const Text(
                  'NUKE TARGET',
                  style: TextStyle(fontWeight: FontWeight.bold, fontFamily: 'Courier')
                ),
              )
            ]
          );
        }
      );
    }

    @override
    void dispose() {
      _searchController.dispose();
      super.dispose();
    }

    @override
    Widget build(BuildContext context) {
      return Scaffold(
        backgroundColor: const Color(0xFF121212),
        appBar: AppBar(
          title: Text(_currentPath, style: TextStyle(color: Colors.white70, fontFamily: 'Courier', fontWeight: FontWeight.bold)),
          backgroundColor: const Color(0xFF1E1E1E),
          elevation: 0,
          bottom: PreferredSize(
            preferredSize: const Size.fromHeight(60),
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: _buildSearchBar(),
            )
          )
        ),
        body: Column(
          children: [
            // Directory List
            Expanded(
              child: FutureBuilder<List<StorageNode>>(
                future: _directoryData,
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Center(child: CircularProgressIndicator(color: Colors.cyanAccent));
                  } else if (snapshot.hasError) {
                    return Center(
                      child: Text(
                        'BRIDGE FAILURE: ${snapshot.error}',
                        style: TextStyle(color: Colors.red),
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

                  return ListView.builder(
                    itemCount: itemCount,
                    itemBuilder: (context, index) {
                      if(!isRoot && index == 0) {
                        return Container(
                          margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                          child: ListTile(
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(4),
                            ),
                            tileColor: Colors.white10,
                            hoverColor: Colors.white24,
                            leading: const Icon(Icons.keyboard_return_outlined, color: Colors.cyanAccent),
                            title: const Text(
                              '[..] GO UP A DIRECTION',
                              style: TextStyle(
                                color: Colors.cyanAccent,
                                fontWeight: FontWeight.bold,
                                fontFamily: 'Courier',
                              ),
                            ),
                            onTap: _navigateUp,
                          ),
                        );
                      }

                      final node = displayNodes[isRoot ? index : index - 1];
                      final isDir = node.type == 'Directory';

                      return Container(
                        margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                        child: ListTile(
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(4),
                            side: const BorderSide(color: Colors.white10),
                          ),
                          tileColor: const Color(0xFF1E1E1E),
                          hoverColor: Colors.white.withOpacity(0.05),
                          leading: Icon(
                            isDir ? Icons.folder_copy_outlined : Icons.insert_drive_file_outlined,
                            color: Colors.white38,
                          ),
                          title: Text(
                            node.name,
                            style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
                          ),
                          trailing: Text(
                            formatBytes(node.sizeBytes),
                            style: const TextStyle(
                              color: Colors.cyanAccent, fontFamily: 'Courier', fontSize: 13,
                            ),
                          ),
                          onTap: () {
                            if (isDir) {
                              _navigateTo(node.path);
                            } else {
                              _armNukePop(node.name, node.path);
                            }
                          },
                        ),
                      );
                    },
                  );
                }
              ),
            ),
            // The Telemetry Bar
            _buildTelemetryBar(),
          ],
        )
      );
    }

    Widget _buildSearchBar() {
      return TextField(
        controller: _searchController,
        style: const TextStyle(color: Colors.cyanAccent, fontFamily: 'Courier'),
        decoration: InputDecoration(
          prefixIcon: const Icon(
            Icons.search_outlined, color: Colors.white54,
          ),
          suffixIcon: _searchQuery.isNotEmpty ? IconButton(
            icon: const Icon(Icons.clear_outlined, color: Colors.redAccent),
            onPressed: () {
              _searchController.clear();
              setState(() {
                _searchQuery = '';
              });
            },
          ) : null,
          hintText: 'QUERY DIRECTORY....',
          hintStyle: const TextStyle(
            color: Colors.white24, fontFamily: 'Courier'),
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
          if(!snapshot.hasData) return const SizedBox.shrink();

          final stats = snapshot.data!;
          final double usageFraction = stats.usedBytes / stats.totalBytes;

          return Container(
            padding: const EdgeInsets.all(20),
            decoration: const BoxDecoration(
              color: Color(0xFF121212),
              border: Border(top: BorderSide(color:  Colors.white10, width: 1)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'CAPACITY (C:)',
                      style: TextStyle(
                        color: Colors.white54,
                        fontSize: 12,
                        fontWeight: FontWeight.bold
                      ),
                    ),
                    Text(
                      '${stats.percentageFree}% FREE',
                      style: const TextStyle(
                        color: Colors.greenAccent,
                        fontSize: 12, fontWeight: FontWeight.bold
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8,),
                LinearProgressIndicator(
                  value: usageFraction,
                  backgroundColor: Colors.white10,
                  color: Colors.greenAccent,
                  minHeight: 6,
                ),
                const SizedBox(height: 8,),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      '${formatBytes(stats.usedBytes)} USED',
                      style: const TextStyle(
                        color: Colors.white38,
                        fontSize: 10
                      ),
                    ),
                    Text(
                      '${formatBytes(stats.totalBytes)} TOTAL',
                      style: const TextStyle(
                        color: Colors.white38,
                        fontSize: 10
                      ),
                    ),
                  ],
                ),
              ],
            ),
          );
        }
      );
    }
  }


