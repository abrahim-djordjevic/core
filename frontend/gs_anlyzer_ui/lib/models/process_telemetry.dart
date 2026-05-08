class ProcessTelemetry {
  final int pid;
  final String name;
  final double ramMb;
  final double percentMem;

  ProcessTelemetry ({
    required this.pid,
    required this.name,
    required this.ramMb,
    required this.percentMem,
});

  factory ProcessTelemetry.fromJson(Map<String, dynamic> json, double totalSystemRamMb) {
    double mb = (json['ramMb'] ?? 0.0).toDouble();
    return ProcessTelemetry(
      pid: json['processId'] ?? 0,
      name: json['name'] ?? 'UNKNOWN',
      ramMb: mb,
      percentMem: totalSystemRamMb > 0 ? (mb / totalSystemRamMb) * 100 : 0.0,
    );
  }
}

class ProcessGroup {
  final String name;
  final List<ProcessTelemetry> processes;

  ProcessGroup({
    required this.name,
    required this.processes,
  });

  double get totalRamMb => processes.fold(0, (sum, p) => sum + p.ramMb);
  double get totalPercentMem => processes.fold(0, (sum, p) => sum + p.percentMem);
  int get count => processes.length;

  int get primaryPid => processes.isNotEmpty ? processes.first.pid : 0;
}