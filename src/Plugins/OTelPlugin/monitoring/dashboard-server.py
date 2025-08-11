#!/usr/bin/env python3
import http.server
import socketserver
import urllib.request
import urllib.parse
import json
from http.server import SimpleHTTPRequestHandler

class ProxyHandler(SimpleHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith('/api/'):
            # Proxy Prometheus API calls
            prometheus_url = f"http://localhost:9091{self.path}"
            try:
                with urllib.request.urlopen(prometheus_url) as response:
                    data = response.read()
                    
                self.send_response(200)
                self.send_header('Content-Type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(data)
            except Exception as e:
                self.send_response(500)
                self.send_header('Content-Type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(json.dumps({'error': str(e)}).encode())
        elif self.path == '/' or self.path == '/dashboard':
            # Serve the real-data dashboard HTML (no sample data)
            self.path = '/real-dashboard.html'
            return SimpleHTTPRequestHandler.do_GET(self)
        else:
            # Serve static files
            return SimpleHTTPRequestHandler.do_GET(self)
    
    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        super().end_headers()

if __name__ == '__main__':
    PORT = 8888
    print(f"🚀 Neo Dashboard Server starting on http://localhost:{PORT}")
    print(f"📊 Dashboard: http://localhost:{PORT}/dashboard")
    print(f"🔄 Proxying Prometheus API from localhost:9091")
    print("\nPress Ctrl+C to stop\n")
    
    with socketserver.TCPServer(("", PORT), ProxyHandler) as httpd:
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\n✋ Dashboard server stopped")