import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/navigation_provider.dart';
import 'package:gs_analyzer_ui/utils/hud_theme.dart';
import 'package:gs_analyzer_ui/utils/hud_label.dart';

class GlobalSidebarWidget extends ConsumerWidget {
  const GlobalSidebarWidget({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final currentRoute = ref.watch(navigationProvider);

    return Container(
      width: 250,
      decoration: const BoxDecoration(
        color: Color(0xFF0F0F0F),
        border: Border(right: BorderSide(color: Colors.white10)),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('NODE_01', style: TextStyle(color: HudTheme.accentCyan, fontSize: 18, fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore)),
                const SizedBox(height: 4),
                const Text('ONLINE', style: TextStyle(color: HudTheme.accentGreen, fontSize: 10, fontWeight: FontWeight.bold, fontFamily: HudTheme.fontCore)),
                const SizedBox(height: 24),
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: Colors.white10, borderRadius: BorderRadius.circular(8)
                      ),
                      child: const Icon(Icons.person_2_outlined, color: HudTheme.textDim, size: 16,),
                    ),
                    const SizedBox(width: 12),
                    const Expanded(child: HudLabel('SYSTEM ADMINISTRATOR')),
                  ],
                ),
              ],
            ),
          ),

          const Divider(color: Colors.white10, height: 1,),
          const SizedBox(height: 16),

          _buildNavItem(ref, AppRoute.dashboard, 'DASHBOARD', Icons.dashboard_outlined, currentRoute),
          _buildNavItem(ref, AppRoute.cpuMetics, 'CPU METRICS', Icons.memory_outlined, currentRoute),
          _buildNavItem(ref, AppRoute.memory, 'MEMORY', Icons.bar_chart_outlined, currentRoute),
          _buildNavItem(ref, AppRoute.storage, 'STORAGE', Icons.storage_outlined, currentRoute),
          _buildNavItem(ref, AppRoute.network, 'NETWORK', Icons.account_tree_outlined, currentRoute),
          _buildNavItem(ref, AppRoute.thermal, 'THERMAL', Icons.thermostat_outlined, currentRoute),

          const Spacer(),

          _buildNavItem(ref, null, 'HELP', Icons.help_outline_outlined, currentRoute, isAction: true),
          const SizedBox(height: 24),
        ],
      ),
    );
  }

  Widget _buildNavItem(WidgetRef ref, AppRoute? route, String title, IconData icon, AppRoute currentRoute, {bool isAction = false}) {
    final isActive = route == currentRoute  && !isAction;
    final color = isActive ? HudTheme.accentCyan : HudTheme.textDim;

    return InkWell(
      onTap: () {
        if (route != null) {
          ref
              .read(navigationProvider.notifier)
              .state = route;
        }
      },
      hoverColor: Colors.white.withValues(alpha: 0.05),
      child: Container(
        decoration: BoxDecoration(
          color: isActive ? HudTheme.accentCyan.withValues(alpha: 0.055) : Colors.transparent,
          border: Border(
            left: BorderSide(
              color: isAction ? HudTheme.accentCyan : Colors.transparent,
              width: 4,
            )
          ),
        ),
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        child: Row(
          children: [
            Icon(icon, color: color, size: 20),
            const SizedBox(width: 16),
            Text(
              title,
              style: HudTheme.bodyText.copyWith(
                color: color,
                fontWeight: isActive ? FontWeight.bold : FontWeight.normal,
              ),
            ),
          ],
        )
      ),
    );

  }
}