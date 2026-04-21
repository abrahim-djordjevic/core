import 'package:flutter/material.dart';
import 'package:gs_analyzer_ui/screen/test_screen.dart';
import 'services/api_service.dart';

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
      home: const TestScreen(),
    );
  }
}
