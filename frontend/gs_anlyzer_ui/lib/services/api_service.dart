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
      List<dynamic> data = json.decode(response.body);

      return data.map((json) => StorageNode.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load system data from C# Bridge');
    }
  }

  Future<DriveStats> getDriveTelemetry(String driveLetter) async {
    final response = await http.get(Uri.parse('$baseUrl/drive-stats?driveLetter=$driveLetter'));

    if(response.statusCode == 200) {
      return DriveStats.fromJson(json.decode(response.body));
    } else {
      throw Exception('Failed to load hardware telemetry');
    }
  }

  Future<bool> nukeNode(String path) async {
    final uri = Uri.parse('$baseUrl/nuke').replace(queryParameters: {
      'path': path
    });

    print("INITIATING NUKE PROTOCOL ON: $uri");
    final response = await http.delete(uri);

    if(response.statusCode == 200) {
      return true;
    } else {
      throw Exception('Nuke Failed: ${response.statusCode} - ${response.body}');
    }
  }
}
