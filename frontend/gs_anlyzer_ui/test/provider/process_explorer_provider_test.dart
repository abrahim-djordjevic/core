import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gs_analyzer_ui/models/process_telemetry.dart';
import 'package:gs_analyzer_ui/providers/process_explorer_provider.dart';
import 'package:gs_analyzer_ui/providers/ram_provider.dart';

// Helper — builds a container with a pre-seeded ram state
ProviderContainer _containerWithGroups(List<ProcessGroup> groups) {
  return ProviderContainer(
    overrides: [
      ramProvider.overrideWith((ref) {
        final notifier = RamNotifier(ref);
        notifier.debugSetGroupsForTest(groups);
        return notifier;
      }),
    ],
  );
}

ProcessGroup _group({
  required String name,
  double ramMb = 100.0,
  double cpu = 5.0,
  String status = 'RUNNING',
  String user = 'SYSTEM',
  int pid = 1,
}) =>
    ProcessGroup(
      name: name,
      processes: [
        ProcessTelemetry(
          pid: pid, name: name, ramMb: ramMb, percentMem: ramMb / 163.84,
          cpuPercent: cpu, status: status, user: user,
        ),
      ],
    );

void main() {
  group('filteredProcessesProvider', () {
    test('no filter returns all groups', () {
      final groups = [_group(name: 'chrome'), _group(name: 'devenv'), _group(name: 'system')];
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      final result = container.read(filteredProcessesProvider);
      expect(result.length, 3);
    });

    test('name filter is case-insensitive', () {
      final groups = [_group(name: 'Chrome'), _group(name: 'devenv'), _group(name: 'chrome_elf')];
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processFilterProvider.notifier).state = 'chrome';
      final result = container.read(filteredProcessesProvider);
      expect(result.length, 2);
      expect(result.every((g) => g.name.toLowerCase().contains('chrome')), isTrue);
    });

    test('filter by pid string', () {
      final groups = [_group(name: 'chrome', pid: 1234), _group(name: 'devenv', pid: 5678)];
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processFilterProvider.notifier).state = '1234';
      final result = container.read(filteredProcessesProvider);
      expect(result.length, 1);
      expect(result.first.name, 'chrome');
    });

    test('filter with no match returns empty list', () {
      final groups = [_group(name: 'chrome'), _group(name: 'devenv')];
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processFilterProvider.notifier).state = 'zzznomatch';
      expect(container.read(filteredProcessesProvider), isEmpty);
    });
  });

  group('Sort modes', () {
    final groups = [
      _group(name: 'alpha', cpu: 5.0,  ramMb: 300.0, pid: 30),
      _group(name: 'beta',  cpu: 25.0, ramMb: 100.0, pid: 10),
      _group(name: 'gamma', cpu: 10.0, ramMb: 200.0, pid: 20),
    ];

    test('default sort is by CPU% descending', () {
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      final result = container.read(filteredProcessesProvider);
      expect(result[0].name, 'beta');   // 25%
      expect(result[1].name, 'gamma');  // 10%
      expect(result[2].name, 'alpha');  // 5%
    });

    test('sort by RAM descending', () {
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processSortModeProvider.notifier).state = ProcessSortMode.ram;
      final result = container.read(filteredProcessesProvider);
      expect(result[0].name, 'alpha');  // 300 MB
      expect(result[1].name, 'gamma');  // 200 MB
      expect(result[2].name, 'beta');   // 100 MB
    });

    test('sort by PID ascending', () {
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processSortModeProvider.notifier).state = ProcessSortMode.pid;
      final result = container.read(filteredProcessesProvider);
      expect(result[0].primaryPid, 10);
      expect(result[1].primaryPid, 20);
      expect(result[2].primaryPid, 30);
    });

    test('sort by name ascending', () {
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processSortModeProvider.notifier).state = ProcessSortMode.name;
      final result = container.read(filteredProcessesProvider);
      expect(result[0].name, 'alpha');
      expect(result[1].name, 'beta');
      expect(result[2].name, 'gamma');
    });
  });

  group('Status filter', () {
    final groups = [
      _group(name: 'running_proc', status: 'RUNNING'),
      _group(name: 'sleeping_proc', status: 'SLEEPING'),
      _group(name: 'another_running', status: 'RUNNING'),
    ];

    test('ALL filter shows everything', () {
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processStatusFilterProvider.notifier).state = ProcessStatusFilter.all;
      expect(container.read(filteredProcessesProvider).length, 3);
    });

    test('RUNNING filter shows only running', () {
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processStatusFilterProvider.notifier).state = ProcessStatusFilter.running;
      final result = container.read(filteredProcessesProvider);
      expect(result.length, 2);
      expect(result.every((g) => g.dominantStatus == 'RUNNING'), isTrue);
    });

    test('SLEEPING filter shows only sleeping', () {
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(processStatusFilterProvider.notifier).state = ProcessStatusFilter.sleeping;
      final result = container.read(filteredProcessesProvider);
      expect(result.length, 1);
      expect(result.first.name, 'sleeping_proc');
    });
  });

  group('showAllProcessesProvider', () {
    test('false caps results at 100', () {
      final groups = List.generate(150, (i) => _group(name: 'proc_$i', pid: i + 1));
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(showAllProcessesProvider.notifier).state = false;
      expect(container.read(filteredProcessesProvider).length, 100);
    });

    test('true shows all results beyond 100', () {
      final groups = List.generate(150, (i) => _group(name: 'proc_$i', pid: i + 1));
      final container = _containerWithGroups(groups);
      addTearDown(container.dispose);
      container.read(showAllProcessesProvider.notifier).state = true;
      expect(container.read(filteredProcessesProvider).length, 150);
    });
  });
}