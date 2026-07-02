import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/providers/settings_provider.dart';

class HudDensity {
  final double gap;
  final double panelPad;
  final double rowHeight;

  const HudDensity({
    required this.gap,
    required this.panelPad,
    required this.rowHeight,
  });
}

final hudDensityProvider = Provider<HudDensity>((ref) {
  final settings = ref.watch(settingsProvider);
  final isCompact = settings.currentSettings?.appearance.compactMode ?? true;
  
  if (isCompact) {
    return const HudDensity(
      gap: 8.0,
      panelPad: 12.0,
      rowHeight: 32.0,
    );
  } else {
    return const HudDensity(
      gap: 16.0,
      panelPad: 24.0,
      rowHeight: 48.0,
    );
  }
});
