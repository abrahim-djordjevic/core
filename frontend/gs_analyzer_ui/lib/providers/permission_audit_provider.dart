import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gs_analyzer_ui/models/permission_audit_models.dart';
import 'package:gs_analyzer_ui/services/api_service.dart';

final apiServiceProvider = Provider((ref) => ApiService());

final permissionAuditProvider = NotifierProvider<PermissionAuditNotifier, AsyncValue<PermissionAuditResult?>>(() {
  return PermissionAuditNotifier();
});

class PermissionAuditNotifier extends Notifier<AsyncValue<PermissionAuditResult?>> {
  @override
  AsyncValue<PermissionAuditResult?> build() {
    return const AsyncValue.data(null);
  }

  Future<void> runAudit(String rootPath) async {
    state = const AsyncValue.loading();
    try {
      final apiService = ref.read(apiServiceProvider);
      final result = await apiService.auditPermissions(rootPath);
      state = AsyncValue.data(result);
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }

  void reset() {
    state = const AsyncValue.data(null);
  }
}
