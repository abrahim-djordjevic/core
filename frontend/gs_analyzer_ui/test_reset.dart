import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:gs_analyzer_ui/models/app_settings.dart';

void main() async {
  try {
    final response = await http.post(Uri.parse('http://localhost:5200/api/settings/reset'));
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body)['data'];
      final settings = AppSettings.fromjson(data);
      print('Success! Settings: ${settings.toJson()}');
    } else {
      print('Status code: ${response.statusCode}');
    }
  } catch (e, stack) {
    print('Error: $e');
    print(stack);
  }
}
