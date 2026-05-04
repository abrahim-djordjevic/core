import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/screen/master_layout.dart';
import 'package:gs_analyzer_ui/utils/globals.dart';

void main() {
  runApp(const ProviderScope(child:  AnalyzerApp()));
}

class AnalyzerApp extends StatelessWidget {
  const AnalyzerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'GS System Analyzer',
      debugShowCheckedModeBanner: false,
      scaffoldMessengerKey: snackbarKey,
      theme: ThemeData.dark(),
      home: const MasterLayout(),
    );
  }
}
