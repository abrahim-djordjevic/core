import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/directory_provider.dart';

class GoUpRowWidget extends ConsumerWidget {
  const GoUpRowWidget({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return InkWell(
      onTap: () {
        ref.read(directoryProvider.notifier).navigateUp();
      },
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
                  SizedBox(width: 32),
                  Icon(Icons.keyboard_return_outlined, color: Colors.cyanAccent, size: 20),
                  SizedBox(width: 8),
                  Text('[..] GO UP A DIRECTORY', style: TextStyle(color: Colors.cyanAccent, fontWeight: FontWeight.bold, fontFamily: 'Courier'))
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
}