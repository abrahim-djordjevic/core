import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/drive_stats.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

final driveStatsProvider = FutureProvider<DriveStats>((ref) async {
  return ApiService().getDriveTelemetry('C');
});