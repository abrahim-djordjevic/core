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
    late final Future<List<StorageNode>> _directoryData;
    late Future<DriveStats> _driveStats;

    final TextEditingController _searchController = TextEditingController();
    String _searchQuery = '';

    @override
    void initState() {
      super.initState();
      _directoryData = _apiService.scanDirectory('C:/');
      _driveStats = _apiService.getDriveTelemetry('C:');
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
          title: const Text('SYS_DRIVE_0 (C:)', style: TextStyle(color: Colors.white70, fontFamily: 'Courier', fontWeight: FontWeight.bold)),
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
                  } else if (!snapshot.hasData || snapshot.data!.isEmpty) {
                    return const Center(
                      child: Text(
                        'NO DATA FOUND IN SECTOR',
                        style: TextStyle(
                        color: Colors.white54
                        ),
                      ),
                    );
                  }

                  // Search Filter Logic
                  final displayNodes = snapshot.data!.where((node) {
                    return node.name.toLowerCase().contains(_searchQuery.toLowerCase());
                  }).toList();

                  return ListView.builder(
                    itemCount: displayNodes.length,
                    itemBuilder: (context, index) {
                      final node = displayNodes[index];
                      final isDir = node.type == 'Directory';

                      return Container(
                        margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF1E1E1E),
                          borderRadius: BorderRadius.circular(4),
                          border: Border.all(color: Colors.white10),
                        ),
                        child: ListTile(
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
                            //TODO('Implement Directory Navigation');
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


