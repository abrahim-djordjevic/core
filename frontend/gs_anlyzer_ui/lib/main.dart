// frontend/gs_anlyzer_ui/lib/main.dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';
import 'package:gs_analyzer_ui/screen/master_layout.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';

void main() {
  runApp(const ProviderScope(child: GSAnalyzerApp()));
}

class GSAnalyzerApp extends ConsumerWidget {
  const GSAnalyzerApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final appearance = ref.watch(
      settingsProvider.select((s) => s.currentSettings?.appearance),
    );

    final accentColor = HudTheme.resolveAccent(appearance?.accentColor);
    final bgColor     = HudTheme.resolveBgBase(appearance?.theme);

    return MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: ThemeData.dark().copyWith(
        scaffoldBackgroundColor: bgColor,
        colorScheme: ColorScheme.dark(primary: accentColor),
      ),
      home: const MasterLayout(),
    );
  }
}