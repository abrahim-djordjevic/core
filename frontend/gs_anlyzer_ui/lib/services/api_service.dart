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

  Future<bool> nukeNode(String path) async {
    final uri = Uri.parse('$baseUrl/nuke').replace(queryParameters: {
      'path': path
    });

    print("INITIATING NUKE PROTOCOL ON: $uri");
    final response = await http.delete(uri);

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
}
