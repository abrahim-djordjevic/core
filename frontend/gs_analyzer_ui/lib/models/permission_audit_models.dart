class PermissionIssue {
  final String path;
  final String severity; // "high" | "medium" | "low"
  final String type; // "executable_in_data_dir" | "world_writable" | "no_owner"
  final String description;

  PermissionIssue({
    required this.path,
    required this.severity,
    required this.type,
    required this.description,
  });

  factory PermissionIssue.fromJson(Map<String, dynamic> json) {
    return PermissionIssue(
      path: json['path'] as String? ?? '',
      severity: json['severity'] as String? ?? 'low',
      type: json['type'] as String? ?? '',
      description: json['description'] as String? ?? '',
    );
  }
}

class PermissionAuditResult {
  final String root;
  final DateTime auditedAt;
  final int totalScanned;
  final List<PermissionIssue> issues;

  PermissionAuditResult({
    required this.root,
    required this.auditedAt,
    required this.totalScanned,
    required this.issues,
  });

  factory PermissionAuditResult.fromJson(Map<String, dynamic> json) {
    var issuesList = json['issues'] as List<dynamic>? ?? [];
    List<PermissionIssue> parsedIssues = issuesList
        .map((i) => PermissionIssue.fromJson(i as Map<String, dynamic>))
        .toList();

    // Sort issues: high -> medium -> low
    final severityScore = {'high': 0, 'medium': 1, 'low': 2};
    parsedIssues.sort((a, b) {
      int scoreA = severityScore[a.severity] ?? 3;
      int scoreB = severityScore[b.severity] ?? 3;
      return scoreA.compareTo(scoreB);
    });

    return PermissionAuditResult(
      root: json['root'] as String? ?? '',
      auditedAt: json['auditedAt'] != null
          ? DateTime.parse(json['auditedAt'] as String)
          : DateTime.now(),
      totalScanned: json['totalScanned'] as int? ?? 0,
      issues: parsedIssues,
    );
  }
}
