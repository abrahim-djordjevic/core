import 'dart:convert';
import 'package:gs_analyzer_ui/models/drive_stats.dart';
import 'package:http/http.dart' as http;
import 'package:gs_analyzer_ui/models/storage_node.dart';

class ApiService {
  static  const String baseUrl = 'http://localhost:5200/api/storage';

  Future<List<StorageNode>> scanDirectory(String path) async {
    final uri = Uri.parse('$baseUrl/scan').replace(queryParameters: {
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
    final response = await http.get(Uri.parse('$baseUrl/drive-stats?driveLetter=$driveLetter'));

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

  Future<bool> nukeNode(List<String> paths) async {
    final uri = Uri.parse('$baseUrl/nuke').replace(queryParameters: {
      'path': paths
    });

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
      await http.post(Uri.parse('$baseUrl/abort-nuke'));
    } catch (e) {
      print('Failed to send abort signal: $e');
    }
  }

  Future<void> abortScan() async {
    try {
      print('SENDING SCAN ABORT SIGNAL...');
      await http.post(Uri.parse('$baseUrl/abort-scan'));
    } catch (e) {
      print('Failed to send abort signal: $e');
    }
  }
  
  Future<bool> killRamProcesses(List<int> pids) async {
    final uri = Uri.parse('http://localhost:5200/api/Telemetry/ram/kill');
    print('INITIATING ASASSINATION PROTOCOL ON ${pids.length}: TARGETS AT:  $uri');

    final response = await http.post(uri, headers: {'Content-Type': 'application/json'}, body: jsonEncode(pids));

    if(response.statusCode == 200) {
      return true;
    } else {
      throw Exception('Failed to terminate PID Status: ${response.statusCode} - ${response.body}');
    }
  }

  Future<void> startRamRadar() async {
    final uri = Uri.parse('http://localhost:5200/api/Telemetry/ram/start');
    try {
      final response = await http.post(uri);
      if (response.statusCode == 200) {
        print('FLUTTER COMMAND: RAM Radar Started Successfully!');
      }
      } catch (e) {
      print('FLUTTER ERROR: Failed to start RAM Radar - $e');
    }
  }

  Future<void> requestDirectoryStream(String path) async {
    final uri = Uri.parse('$baseUrl/stream-sector').replace(queryParameters: {
      'path': path
    });
    await http.post(uri);
  }
}
