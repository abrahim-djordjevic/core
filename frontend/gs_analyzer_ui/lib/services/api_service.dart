import 'package:gs_analyzer_ui/utils/logger.dart';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:gs_analyzer_ui/models/age_heatmap_model.dart';
import 'package:gs_analyzer_ui/models/drive_stats.dart';
import 'package:gs_analyzer_ui/models/nuke_preview.dart';
import 'package:gs_analyzer_ui/models/nuke_result.dart';
import 'package:http/http.dart' as http;
import 'package:gs_analyzer_ui/models/storage_node.dart';
import 'package:gs_analyzer_ui/models/file_type_model.dart';
import 'package:gs_analyzer_ui/models/extension_breakdown_model.dart';
import 'package:gs_analyzer_ui/providers/age_heatmap_provider.dart';
import 'package:gs_analyzer_ui/providers/file_type_provider.dart';
import 'package:gs_analyzer_ui/models/permission_audit_models.dart';
import 'package:gs_analyzer_ui/models/telemetry_history_model.dart';

class ApiService {
  final http.Client _client;
  ApiService([http.Client? client]) : _client = client ?? http.Client();

  static  const String storageUrl = 'http://localhost:5200/api/storage';
  static const String telemetryUrl = 'http://localhost:5200/api/Telemetry';
  static const String nukeUrl = 'http://localhost:5200/api/nuke';
  static const String thermalUrl = 'http://localhost:5200/api/thermal';
  static const String settingsUrl = 'http://localhost:5200/api/settings';
  static const String driveUrl = 'http://localhost:5200/api/drives';
  static const String auditUrl = 'http://localhost:5200/api/audit';
  static const String telemetryHistoryUrl = 'http://localhost:5200/api/telemetry/history';

  Future<TelemetryHistoryResponse?> fetchTelemetryHistory(String metric, int minutes) async {
    final uri = Uri.parse(telemetryHistoryUrl).replace(queryParameters: {
      'metric': metric,
      'minutes': minutes.toString(),
    });
    
    // appLogger.i('MATRIX BRIDGE FIRING TO: \$uri');
    final response = await _client.get(uri);

    if (response.statusCode == 200) {
      final jsonBody = jsonDecode(response.body);
      return TelemetryHistoryResponse.fromJson(jsonBody);
    } else {
      appLogger.i('Failed to fetch telemetry history: \${response.statusCode} - \${response.body}');
      return null;
    }
  }


  Future<List<StorageNode>> scanDirectory(String path) async {
    final uri = Uri.parse('$storageUrl/scan');
    appLogger.i('MATRIX BRIDGE FIRING TO: $uri (root: $path)');

    final response = await _client.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'Root': path}),
    );

    if (response.statusCode == 200) {
      final jsonBody = jsonDecode(response.body);

      if (jsonBody['success'] == true) {
        appLogger.i('FEDEX BOX OPENED! Data is: ${jsonBody['data']}');
        List<dynamic> data = jsonBody['data'];
        return data.map((json) => StorageNode.fromJson(json)).toList();
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Bridge Failed with Status: ${response.statusCode} - ${response.body}');
    }
  }

  http.Client? _auditClient;

  Future<PermissionAuditResult> auditPermissions(String root) async {
    final uri = Uri.parse('$auditUrl/permissions');
    appLogger.i('FIRING PERMISSION AUDIT ON: $uri (root: $root)');

    _auditClient = http.Client();
    try {
      final response = await _auditClient!.post(
        uri,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'root': root}),
      );

      if (response.statusCode == 200) {
        final jsonBody = jsonDecode(response.body);
        return PermissionAuditResult.fromJson(jsonBody);
      } else if (response.statusCode == 499) {
        throw Exception('AUDIT CANCELLED BY USER');
      } else {
        throw Exception('Audit Failed with Status: ${response.statusCode} - ${response.body}');
      }
    } finally {
      _auditClient?.close();
      _auditClient = null;
    }
  }

  void cancelAudit() {
    if (_auditClient != null) {
      appLogger.i('USER ABORT: CANCELLING PERMISSION AUDIT!');
      _auditClient!.close();
      _auditClient = null;
    }
  }

  Future<DriveStats> getDriveTelemetry(String driveLetter) async {
    final response = await _client.get(Uri.parse('$storageUrl/drive-stats?driveLetter=$driveLetter'));

    if(response.statusCode == 200) {
      final jsonBody = json.decode(response.body);

      if (jsonBody['success'] == true) {
        return DriveStats.fromJson(jsonBody['data']);
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Failed to load hardware telemetry');
    }
  }

  Future<NukeResultDto> executeNuke(List<String> paths, String planToken, {bool useRecycleBin = false}) async {
    final uri = Uri.parse('$nukeUrl/execute');

    appLogger.i("INITIATING NUKE PROTOCOL ON: $uri");

    final response = await _client.delete(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode({
      'paths': paths,
      'planToken': planToken,
      'useRecycleBin': useRecycleBin
    }));

    if(response.statusCode == 200) {
      final jsonBody = json.decode(response.body);

      if (jsonBody['success'] == true) {
        return NukeResultDto.fromJson(jsonBody['data']);
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Nuke Failed: ${response.statusCode} - ${response.body}');
    }
  }

  Future<NukeResultDto> undoNuke([String? operationId]) async {
    final uriStr = operationId != null ? '$nukeUrl/undo/$operationId' : '$nukeUrl/undo';
    final uri = Uri.parse(uriStr);
    final response = await _client.post(uri);

    if (response.statusCode == 200) {
      final jsonBody = json.decode(response.body);
      if (jsonBody['success'] == true) {
        return NukeResultDto.fromJson(jsonBody['data']);
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Undo Failed: ${response.statusCode} - ${response.body}');
    }
  }

  Future<List<NukeOperation>> getUndoHistory() async {
    final uri = Uri.parse('$nukeUrl/undo/history');
    final response = await _client.get(uri);

    if (response.statusCode == 200) {
      final jsonBody = json.decode(response.body);
      if (jsonBody['success'] == true) {
        List<dynamic> data = jsonBody['data'];
        return data.map((json) => NukeOperation.fromJson(json)).toList();
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Failed to load undo history: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> clearUndoStack() async {
    final uri = Uri.parse('$nukeUrl/undo');
    final response = await _client.delete(uri);
    if (response.statusCode != 200) {
      throw Exception('Failed to clear undo stack');
    }
  }

  Future<void> abortNuke() async {
    try {
      appLogger.i('SENDING NUKE ABORT SIGNAL....');
      await _client.post(Uri.parse('$nukeUrl/abort'));
    } catch (e) {
      appLogger.i('Failed to send abort signal: $e');
    }
  }

  Future<void> abortScan() async {
    try {
      appLogger.i('SENDING SCAN ABORT SIGNAL...');
      await _client.post(Uri.parse('$storageUrl/abort-scan'));
    } catch (e) {
      appLogger.i('Failed to send abort signal: $e');
    }
  }
  
  Future<bool> killRamProcesses(List<int> pids) async {
    final uri = Uri.parse('$telemetryUrl/ram/kill');
    appLogger.i('INITIATING ASSASSINATION PROTOCOL ON ${pids.length}: TARGETS AT:  $uri');

    final response = await _client.post(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(pids));

    if(response.statusCode == 200) {
      return true;
    } else {
      throw Exception('Failed to terminate PID Status: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> startRamRadar() async {
    final uri = Uri.parse('$telemetryUrl/ram/start');
    try {
      final response = await _client.post(uri);
      if (response.statusCode == 200) {
        appLogger.i('FLUTTER COMMAND: RAM Radar Started Successfully!');
      }
      } catch (e) {
      appLogger.i('FLUTTER ERROR: Failed to start RAM Radar - $e');
    }
  }

  Future<void> startCpuRadar() async {
    final uri = Uri.parse('$telemetryUrl/cpu-load');
    try {
      final response = await _client.get(uri);
      if (response.statusCode == 200) {
        appLogger.i('FLUTTER COMMAND: CPU Radar Started Successfully!');
      } else {
        appLogger.i('FLUTTER COMMAND: Backend returned ${response.statusCode}');
      }
    } catch (e) {
      appLogger.i('FLUTTER ERROR: Failed to start CPU Radar - $e');
    }
  }

  Future<void> requestDirectoryStream(String path) async {
    final uri = Uri.parse('$storageUrl/stream-sector').replace(queryParameters: {
      'path': path
    });
    await _client.post(uri);
  }

  Future<List<dynamic>> scanForDuplicates(String path) async {
    final uri = Uri.parse('$storageUrl/duplicates');
    appLogger.i('INITIATING DUPLICATE HUNTER ON: $uri (root: $path)');

    final response = await _client.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'Root': path}),
    );

    if (response.statusCode == 200) {
      final jsonBody = jsonDecode(response.body);

      if (jsonBody['success'] == true) {
        return jsonBody['data'] as List<dynamic>;
      } else {
        throw Exception(jsonBody['message']);
      }
    } else if (response.statusCode == 499) {
      // Backend signalled the duplicate scan was cancelled by the user — not an error.
      return <dynamic>[];
    } else {
      throw Exception('Bridge Failed with Status: ${response.statusCode} - ${response.body}');
    }
  }

  Future<List<dynamic>> scanForLargeFiles(String rootPath, int topN) async {
    final uri = Uri.parse('$storageUrl/scan-largefiles').replace(queryParameters: {
      'root': rootPath,
      'top': topN.toString(),
    });

    appLogger.i('INITIATING LARGE FILE HUNTER ON: $uri');

    final response = await _client.get(uri);

    if (response.statusCode == 200) {
      final jsonBody = jsonDecode(response.body);

      if (jsonBody['success'] == true) {
        return jsonBody['data'] as List<dynamic>;
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Bridge Failed with Status: ${response.statusCode}');
    }
  }

  Future<Map<String, dynamic>?> getCurrentThermals() async {
    final uri = Uri.parse('$thermalUrl/current');
    appLogger.i('MATRIX BRIDGE: Requesting Instant Thermal Snapshot...');

    try {
      final response = await _client.get(uri);

      if (response.statusCode == 200) {
        final jsonBody = jsonDecode(response.body);

        if (jsonBody['success'] == true && jsonBody['data'] != null){
          return jsonBody['data'] as Map<String, dynamic>;
        } else {
          appLogger.i('THERMAL SNAPSHOT FAILED: ${jsonBody['message']}');
          return null;
        }
      } else {
        appLogger.i('THERMAL SNAPSHOT FAILED: Status ${response.statusCode}');
        return null;
      }
    } catch (e) {
      appLogger.i('THERMAL BRIDGE ERROR: $e');
      return null;
    }
  }

  Future<NukePreviewResponse> previewNuke(List<String> paths) async {
    final uri = Uri.parse('$nukeUrl/preview');
    appLogger.i('MATRIX BRIDGE: REQUESTING BLAST RADIUS FOR ${paths.length} TARGETTs');

    final response = await _client.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'paths': paths}),
    );

    if (response.statusCode == 200) {
      final jsonBody = jsonDecode(response.body);

      if (jsonBody['success'] == true) {
        return NukePreviewResponse.fromJson(jsonBody['data']);
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Bridge Failed with Status: ${response.statusCode} - ${response.body}');
    }
  }

  Future<Map<String, dynamic>?> getSettings() async {
    try {
      final response = await _client.get(Uri.parse(settingsUrl));
      if (response.statusCode == 200) return jsonDecode(response.body)['data'];
    } catch (e) {
      appLogger.i('[API] Settings Fetch Error: $e');
    }
    return null;
  }

  Future<Map<String, dynamic>?> saveSettings(Map<String, dynamic> payload) async {
    try {
      final response = await _client.post(Uri.parse(settingsUrl), headers: {'Content-Type': 'application/json'}, body: jsonEncode(payload));
      return jsonDecode(response.body);
    } catch (e) {
      return {'success': false, 'message': 'Network Error'};
    }
  }

  Future<Map<String, dynamic>?> resetSettings() async {
    try {
      final response = await _client.post(Uri.parse('$settingsUrl/reset'));
      if (response.statusCode == 200) return jsonDecode(response.body)['data'];
    } catch (e) {
      appLogger.i('[API] Reset Error: $e');
    }
    return null;
  }

  Future<List<dynamic>?> getDrives() async {
    try {
      final response = await _client.get(Uri.parse(driveUrl));

      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);

        if (decoded is List) return decoded;
        if (decoded['data'] != null) return decoded['data'];
      }
    } catch (e) {
      appLogger.i("[Api] Drives Fetch Error: $e");
    }
    return null;
  }

  Future<FileTypeResult> getFileTypes(String root) async {
  final uri = Uri.parse('$storageUrl/scan/filetypes')
      .replace(queryParameters: {'root': root});

  final response = await _client.get(uri);

  if (response.statusCode == 409) {
    throw FileTypeNoScanException();
  }

  if (response.statusCode != 200) {
    throw Exception(
      'FileTypes fetch failed [${response.statusCode}]: ${response.body}',
    );
  }

  return FileTypeResult.fromJson(
    jsonDecode(response.body) as Map<String, dynamic>
    );
  }

  Future<ExtensionBreakdownResult> getExtensionBreakdown(String root) async {
    final uri = Uri.parse('$storageUrl/scan/extensions')
        .replace(queryParameters: {'root': root});

    final response = await _client.get(uri);

    if (response.statusCode == 409) {
      throw const FileTypeNoScanException();
    }

    if (response.statusCode != 200) {
      throw Exception(
        'ExtensionBreakdown fetch failed [${response.statusCode}]: ${response.body}',
      );
    }

    return compute(_parseBreakdown, response.body);
  }

  Future<bool> clearCache() async {
  try {
    final response = await _client.post(Uri.parse('$settingsUrl/cache/clear'));
    if (response.statusCode == 200) {
      appLogger.i('[API] Cache cleared successfully.');
      return true;
    }
    appLogger.i('[API] Cache clear failed: ${response.statusCode}');
    return false;
  } catch (e) {
    appLogger.i('[API] Cache clear error: $e');
    return false;
    }
  }

  Future<AgeHeatmapResult> getAgeHeatmap(String root) async {
    final uri = Uri.parse('$storageUrl/scan/ageheatmap')
        .replace(queryParameters: {'root': root});

    appLogger.i('MATRIX BRIDGE: Requesting Age Heatmap for $root');

    final response = await _client.get(uri);

    if (response.statusCode == 409) {
      throw AgeHeatmapNoScanException();
    }

    if (response.statusCode != 200) {
      throw Exception(
        'AgeHeatmap fetch failed [${response.statusCode}]: ${response.body}',
      );
    }

    return AgeHeatmapResult.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }
}

ExtensionBreakdownResult _parseBreakdown(String body) {
  return ExtensionBreakdownResult.fromJson(
    jsonDecode(body) as Map<String, dynamic>
  );
}


