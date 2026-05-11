import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';

import 'mock_factory.dart';

void main() {
  group('RAM Processes Safety Test', () {
    test('RAM Provider should calculate Used Percentage Correctly', () {
      final ramNotifier = RamNotifier();
      
      final midUsage = MockFactory.generateRamTelemetry(usedPercentage: 0.5, totalGb: 16);
      
      ramNotifier.updateProcesses(midUsage);
      
      expect(ramNotifier.state.displayString, contains('8.0 GB / 16.0 GB'));
    });
  });
}