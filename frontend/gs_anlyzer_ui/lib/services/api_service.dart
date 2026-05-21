import 'dart:convert';
import 'package:gs_analyzer_ui/models/drive_stats.dart';
import 'package:gs_analyzer_ui/models/nuke_preview.dart';
import 'package:http/http.dart' as http;
import 'package:gs_analyzer_ui/models/storage_node.dart';

class ApiService {
  static  const String storageUrl = 'http://localhost:5200/api/storage';
  static const String telemetryUrl = 'http://localhost:5200/api/Telemetry';
  static const String nukeUrl = 'http://localhost:5200/api/nuke';

  Future<List<StorageNode>> scanDirectory(String path) async {
    final uri = Uri.parse('$storageUrl/scan').replace(queryParameters: {
      'path': path
    });
    print('MATRIX BRIDGE FIRING TO: $uri');


    final response = await http.get(uri);

    if(response.statusCode == 200) {
      final jsonBody = jsonDecode(response.body);

      if(jsonBody['success'] == true) {
        print('FEDEX BOX OPENED! Data is: ${jsonBody['data']}');
        List<dynamic> data = jsonBody['data'];
        return data.map((json) => StorageNode.fromJson(json)).toList();
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Bridge Failed with Status: ${response.statusCode} - ${response.body}');
    }
  }

  Future<DriveStats> getDriveTelemetry(String driveLetter) async {
    final response = await http.get(Uri.parse('$storageUrl/drive-stats?driveLetter=$driveLetter'));

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

  Future<bool> executeNuke(List<String> paths) async {
    final uri = Uri.parse('$nukeUrl/execute');

    print("INITIATING NUKE PROTOCOL ON: $uri");

    final response = await http.delete(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(paths));

    if(response.statusCode == 200) {
      final jsonBody = json.decode(response.body);

      if (jsonBody['success'] == true) {
        return true;
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Nuke Failed: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> abortNuke() async {
    try {
      print('SENDING NUKE ABORT SIGNAL....');
      await http.post(Uri.parse('$nukeUrl/nuke'));
    } catch (e) {
      print('Failed to send abort signal: $e');
    }
  }

  Future<void> abortScan() async {
    try {
      print('SENDING SCAN ABORT SIGNAL...');
      await http.post(Uri.parse('$storageUrl/abort-scan'));
    } catch (e) {
      print('Failed to send abort signal: $e');
    }
  }
  
  Future<bool> killRamProcesses(List<int> pids) async {
    final uri = Uri.parse('$telemetryUrl/ram/kill');
    print('INITIATING ASSASSINATION PROTOCOL ON ${pids.length}: TARGETS AT:  $uri');

    final response = await http.post(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(pids));

    if(response.statusCode == 200) {
      return true;
    } else {
      throw Exception('Failed to terminate PID Status: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> startRamRadar() async {
    final uri = Uri.parse('$telemetryUrl/ram/start');
    try {
      final response = await http.post(uri);
      if (response.statusCode == 200) {
        print('FLUTTER COMMAND: RAM Radar Started Successfully!');
      }
      } catch (e) {
      print('FLUTTER ERROR: Failed to start RAM Radar - $e');
    }
  }

  Future<void> startCpuRadar() async {
    final uri = Uri.parse('$telemetryUrl/cpu-load');
    try {
      final response = await http.get(uri);
      if (response.statusCode == 200) {
        print('FLUTTER COMMAND: CPU Radar Started Successfully!');
      } else {
        print('FLUTTER COMMAND: Backend returned ${response.statusCode}');
      }
    } catch (e) {
      print('FLUTTER ERROR: Failed to start CPU Radar - $e');
    }
  }

  Future<void> requestDirectoryStream(String path) async {
    final uri = Uri.parse('$storageUrl/stream-sector').replace(queryParameters: {
      'path': path
    });
    await http.post(uri);
  }

  Future<List<dynamic>> scanForDuplicates(String path) async {
    final uri = Uri.parse('$storageUrl/duplicates').replace(queryParameters: {
      'path': path
    });

    print('INITIATING DUPLICATE HUNTER ON: $uri');

    final response = await http.get(uri);

    if (response.statusCode == 200) {
      final jsonBody = jsonDecode(response.body);

      if (jsonBody['success'] == true) {
        return jsonBody['data'] as List<dynamic>;
      } else {
        throw Exception(jsonBody['message']);
      }
    } else {
      throw Exception('Bridge Failed with Status: ${response.statusCode} - ${response.body}');
    }
  }

  Future<List<dynamic>> scanForLargeFiles(String rootPath, int topN) async {
    final uri = Uri.parse('$storageUrl/scan-largefiles').replace(queryParameters: {
      'root': rootPath,
      'top': topN.toString(),
    });

    print('INITIATING LARGE FILE HUNTER ON: $uri');

    final response = await http.get(uri);

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

  Future<NukePreviewResponse> previewNuke(List<String> paths) async {
    final uri = Uri.parse('$nukeUrl/preview');
    print('MATRIX BRIDGE: REQUESTING BLAST RADIUS FOR ${paths.length} TARGETs');

    final response = await http.post(
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
}
