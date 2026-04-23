import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'dart:math';

String formatBytes(int bytes) {
  if (bytes < 0) return "--";
  if (bytes == 0) return "0 B";
  const suffixes = ["B", "KB", "MB", "GB", "TB"];
  var i = (log(bytes) / log(1024)).floor();
  return '${(bytes / pow(1024, i)).toStringAsFixed(1)} ${suffixes[i]}';
}

class DirectoryNodeWidget extends StatefulWidget {
  final StorageNode node;
  final ApiService apiService;
  final int depth;
  final Function(String, String) onNuke;
  final Function(String) onNavigate;
  final bool isTreeView;

  const DirectoryNodeWidget({
    Key? key,
    required this.node,
    required this.apiService,
    required this.onNuke,
    required this.onNavigate,
    this.depth = 0,
    this.isTreeView = false,
  }) : super(key: key);

  @override
  State<DirectoryNodeWidget> createState() => _DirectoryNodeWidgetState();
}

class _DirectoryNodeWidgetState extends State<DirectoryNodeWidget> {
  bool _isExpanded = false;
  bool _isLoading = false;
  List<StorageNode>? _children;

  Future<void> _toggleExpand() async {
    if(!widget.node.isDirectory) return;

    setState(() {
      _isExpanded = !_isExpanded;
    });

    if (_isExpanded && _children == null) {
      setState(() {
        _isLoading = true;
      });

      try {
        final children = await widget.apiService.scanDirectory(widget.node.path);
        setState(() {
          _children = children;
          _isLoading = false;
        });
      } catch (e) {
        setState(() {
          _isLoading = false;
        });
        print('Error Loading Children: $e');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final double leftPadding = widget.depth * 20.0;
    final isDir = widget.node.isDirectory;

    if (widget.isTreeView) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            onTap: isDir ? _toggleExpand : null,
            onDoubleTap: isDir ? () => widget.onNavigate(widget.node.path) : null,
            hoverColor: Colors.cyan.withValues(alpha: 0.1),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
              child: Row(
                children: [
                  SizedBox(width: leftPadding),
                  SizedBox(
                    width: 24,
                    child: isDir
                        ? AnimatedRotation(
                            turns: _isExpanded ? 0.25 : 0.0,
                            duration: const Duration(milliseconds: 200),
                            child: const Icon(Icons.keyboard_arrow_right_outlined, color: Colors.cyanAccent, size: 16),
                          )
                        : const SizedBox(),
                  ),
                  Icon(isDir ? Icons.folder_outlined : Icons.insert_drive_file_outlined,
                      color: isDir ? Colors.amber : Colors.greenAccent, size: 16),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      widget.node.name,
                      style: const TextStyle(color: Colors.white70, fontFamily: 'Courier', fontSize: 13),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),
          ),
          if (_isExpanded) ...[
            if (_isLoading)
              Padding(
                padding: EdgeInsets.only(left: leftPadding + 32, top: 4, bottom: 4),
                child: const SizedBox(width: 12, height: 12, child: CircularProgressIndicator(strokeWidth: 1, color: Colors.cyan)),
              )
            else if (_children != null)
              ..._children!.where((n) => n.isDirectory).map((child) => DirectoryNodeWidget(
                    node: child,
                    apiService: widget.apiService,
                    onNuke: widget.onNuke,
                    onNavigate: widget.onNavigate,
                    depth: widget.depth + 1,
                    isTreeView: true,
                  )),
          ]
        ],
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        InkWell(
          onTap: isDir ? () => widget.onNavigate(widget.node.path) : null,
          hoverColor: Colors.cyan.withValues(alpha: 0.05),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
            decoration: const BoxDecoration(
              border: Border(bottom: BorderSide(color: Colors.white10)),
            ),
            child: Row(
              children: [
                // NAME Column
                Expanded(
                  flex: 4,
                  child: Row(
                    children: [
                      const SizedBox(width: 32), // Replaced expansion arrow with static padding
                      Icon(
                        isDir ? Icons.folder : Icons.insert_drive_file_outlined,
                        color: isDir ? Colors.amber : Colors.greenAccent,
                        size: 20,
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          widget.node.name,
                          style: const TextStyle(
                            color: Colors.white,
                            fontFamily: 'Courier',
                            fontSize: 14,
                            fontWeight: FontWeight.w500,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                ),
                // DATE Column
                Expanded(
                  flex: 3,
                  child: Text(
                    widget.node.lastModified.toString().split('.')[0],
                    style: const TextStyle(
                      color: Colors.white54,
                      fontFamily: 'Courier',
                      fontSize: 13,
                    ),
                  ),
                ),
                // TYPE Column
                Expanded(
                  flex: 2,
                  child: Text(
                    widget.node.type.toUpperCase(),
                    style: const TextStyle(
                      color: Colors.white54,
                      fontFamily: 'Courier',
                      fontSize: 12,
                    ),
                  ),
                ),
                // SIZE Column
                Expanded(
                  flex: 2,
                  child: Text(
                    formatBytes(widget.node.sizeBytes),
                    style: const TextStyle(
                      color: Colors.cyanAccent,
                      fontFamily: 'Courier',
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                    ),
                    textAlign: TextAlign.right,
                  ),
                ),
                // ACTION Column
                Expanded(
                  flex: 1,
                  child: IconButton(
                    icon: Icon(
                      isDir ? Icons.folder_delete_outlined : Icons.delete_forever_outlined,
                      color: Colors.redAccent,
                      size: 20,
                    ),
                    onPressed: () => widget.onNuke(widget.node.name, widget.node.path),
                  ),
                ),
              ],
            ),
          ),
        ),
        if (_isExpanded) ...[
          if (_isLoading)
            Padding(
              padding: EdgeInsets.only(left: leftPadding + 64, top: 8, bottom: 8),
              child: const SizedBox(
                width: 16,
                height: 16,
                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.cyan),
              ),
            )
          else if (_children != null)
            ..._children!.map((childNode) => DirectoryNodeWidget(
                  node: childNode,
                  apiService: widget.apiService,
                  onNuke: widget.onNuke,
                  onNavigate: widget.onNavigate,
                  depth: widget.depth + 1,
                )),
        ]
      ],
    );
  }
}
