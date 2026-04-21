import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/screen/analyzer_dashboard.dart';

void main() {
  runApp(const AnalyzerApp());
}

class AnalyzerApp extends StatelessWidget {
  const AnalyzerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: ThemeData.dark(),
      home: const AnalyzerDashboard(),
    );
  }
}
