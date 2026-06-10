import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/navigation_provider.dart';
import 'package:gs_analyzer_ui/screen/analyzer_dashboard.dart';
import 'package:gs_analyzer_ui/screen/ram_scannner_screen.dart';
import 'package:gs_analyzer_ui/screen/settings_screen.dart';
import 'package:gs_analyzer_ui/screen/thermal_module_screen.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/widgets/global_sidebar_widget.dart';

import 'cpu_metrics_screen.dart';

class MasterLayout extends ConsumerWidget {
  const MasterLayout({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final currentRoute = ref.watch(navigationProvider);

    return Scaffold(
      backgroundColor: HudTheme.bgBase,
      body: Row(
        children: [
          const GlobalSidebarWidget(),

          Expanded(child: _buildActiveScreen(currentRoute)),
        ],
      ),
    );
  }

  Widget _buildActiveScreen(AppRoute route) {
    switch (route) {
      case AppRoute.storage:
        return const AnalyzerDashboard();

      case AppRoute.memory:
        return const RamScannerScreen();

      case AppRoute.cpuMetics:
        return const CpuMetricsScreen();

      case AppRoute.network:
        return const Center(child: Text('NETWORK MODULE OFFLINE', style: HudTheme.headerCyan,));

      case AppRoute.thermal:
        return const ThermalModuleScreen();

      case AppRoute.settings:
        return const SettingsScreen();

      case AppRoute.dashboard:
        return const Center(child: Text('MAIN DASHBOARD OFFLINE', style: HudTheme.headerCyan));
    }
  }
}