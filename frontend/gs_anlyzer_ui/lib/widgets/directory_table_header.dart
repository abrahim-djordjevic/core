import 'package:flutter/material.dart';

class DirectoryTableHeader extends StatelessWidget {
  const DirectoryTableHeader({super.key});

  static const TextStyle _headerStyle = TextStyle(
    color: Colors.white24,
    fontFamily: 'Courier',
    fontWeight: FontWeight.bold,
    fontSize: 12,
    letterSpacing: 1.5,
  );

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
      decoration: const BoxDecoration(
        color: Color(0xFF1A1A1A),
        border: Border(bottom: BorderSide(color: Colors.white10)),
      ),
      child: const Row(
        children: [
          Expanded(
            flex: 4,
            child: Row(
              children: [
                SizedBox(width: 32), SizedBox(width: 20), SizedBox(width: 8),
                Text(
                  'NAME', style: _headerStyle,
                ),
              ],
            ),
          ),
          Expanded(flex: 3, child: Text('DATE MODIFIED', style: _headerStyle,)),
          Expanded(flex: 3, child: Text('TYPE', style: _headerStyle,)),
          Expanded(flex: 2, child: Padding(padding: EdgeInsets.only(right: 16), child: Text('SIZE', style: _headerStyle, textAlign: TextAlign.right))),
          Expanded(flex: 1, child: Text('ACTION', style: _headerStyle, textAlign: TextAlign.center)),
        ],
      ),
    );
  }
}
