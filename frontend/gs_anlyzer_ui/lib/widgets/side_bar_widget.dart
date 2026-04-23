import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/root_tree_provider.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';
import 'package:gs_analyzer_ui/widgets/directory_node_widget.dart';

class SideBarTreeWidget extends ConsumerWidget {
  final Function(String, String) onNuke;

  const SideBarTreeWidget({super.key, required this.onNuke});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final rootNodeAsync = ref.watch(rootTreeProvider);
    final dirNotifier = ref.read(directoryProvider.notifier);

    return Container(
      width: 300,
      decoration: const BoxDecoration(
        color: Color(0xFF161616),
        border: Border(right: BorderSide(color: Colors.white10)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.all(16.0),
            child: Text(
              'DATA TREE',
              style: TextStyle(
                color: Colors.white24,
                fontFamily: 'Courier',
                fontSize: 12,
                fontWeight: FontWeight.bold,
                letterSpacing: 2,
              ),
            ),
          ),
          Expanded(
            child: SingleChildScrollView(
              child: rootNodeAsync.when(
                loading: () => const Center(
                  child: Padding(
                    padding: EdgeInsets.symmetric(vertical: 20),
                    child: CircularProgressIndicator(color: Colors.cyan),
                  ),
                ),
                error: (err, stack) => const Center(
                  child: Text(
                    'FAILED TO LOAD TREE',
                    style: TextStyle(color: Colors.red),
                  ),
                ),
                data: (nodes) {
                  return Column(
                    children: nodes
                        .where((n) => n.isDirectory)
                        .map((node) => DirectoryNodeWidget(
                              node: node,
                              apiService: ApiService(),
                              onNuke: onNuke,
                              onNavigate: dirNotifier.scanDirectory,
                              depth: 0,
                              isTreeView: true,
                            ))
                        .toList(),
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }
}
