import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';

class DirectorySearchWidget extends ConsumerStatefulWidget {
  const DirectorySearchWidget({super.key});

  @override
  ConsumerState<DirectorySearchWidget> createState() =>
      _DirectorySearchWidget();
}

class _DirectorySearchWidget extends ConsumerState<DirectorySearchWidget> {
  final TextEditingController _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final dirState = ref.watch(directoryProvider);
    final dirNotifier = ref.read(directoryProvider.notifier);

    return TextField(
      controller: _searchController,
      style: const TextStyle(
        color: Colors.cyanAccent,
        fontFamily: 'Courier',
      ),
      decoration: InputDecoration(
        prefixIcon: const Icon(
          Icons.search_outlined,
          color: Colors.white54,
        ),
        suffixIcon: dirState.searchQuery.isNotEmpty ? IconButton(
          icon: const Icon(Icons.clear_outlined, color: Colors.redAccent),
          onPressed: () {
            _searchController.clear();
            dirNotifier.updateSearchQuery('');
          },
        ) : null,
        hintText: 'QUERY DIRECTORY....',
        hintStyle: const TextStyle(
          color: Colors.white24,
          fontFamily: 'Courier',
        ),
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