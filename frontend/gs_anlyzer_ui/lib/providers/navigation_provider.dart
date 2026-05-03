import 'package:flutter_riverpod/legacy.dart';

enum AppRoute {
  dashboard,
  cpuMetics,
  memory,
  storage,
  network,
}

final navigationProvider = StateProvider<AppRoute>((ref) => AppRoute.storage);