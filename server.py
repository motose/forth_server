import http.server
import subprocess
import os
import urllib.parse
import re

PORT = 8122

# {{ }} を Pythonの format 用にエスケープしつつ、変数を差し込み
HTML_TEMPLATE = """
<!DOCTYPE html>
<html lang="ja">
<head>
    <meta charset="utf-8">
    <title>Foth DB サービス</title>
    <link rel="icon" href="https://forth-standard.org/images/forth.png">
    <style>
        body {{ margin: 0; font-family: 'Segoe UI', Roboto, 'Hiragino Kaku Gothic ProN', sans-serif; background-color: #f0f2f5; color: #333; }}
        .header {{ background-color: #1a1a1a; color: white; padding: 0 30px; display: flex; align-items: center; justify-content: space-between; height: 70px; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
        .header-left {{ display: flex; align-items: center; }}
        .header-right {{ display: flex; align-items: center; gap: 15px; }}
        .title {{ font-size: 1.8rem; font-weight: bold; letter-spacing: 1px; }}
        .site-name {{ font-size: 1.4rem; color: #999; }}
        .container {{ padding: 40px; max-width: 1000px; margin: 0 auto; }}
        .search-row {{ display: flex; gap: 20px; margin-bottom: 25px; }}
        .card {{ background: white; padding: 25px; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); flex: 1; }}
        .card h2 {{ font-size: 1.2rem; margin-top: 0; border-left: 4px solid #306998; padding-left: 10px; }}
        input[type='text'] {{ width: 100%; box-sizing: border-box; padding: 12px; border: 1px solid #ddd; border-radius: 6px; font-size: 1rem; }}
        button.search-btn {{ width: 100%; padding: 12px; background-color: #3776ab; color: white; border: none; border-radius: 6px; cursor: pointer; margin-top: 15px; font-weight: bold; transition: background 0.2s; }}
        button.search-btn:hover {{ background-color: #306998; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        td {{ border-bottom: 1px solid #f0f0f0; padding: 15px 10px; vertical-align: middle; }}
        
        /* ツールボタンのスタイル */
        .btn-tool {{ text-decoration: none; color: #eee; border: 1px solid #555; padding: 5px 12px; border-radius: 4px; font-size: 13px; transition: all 0.2s; }}
        .btn-tool:hover {{ background-color: #444; color: #fff; border-color: #999; }}
        
        .btn-box {{ display: flex; gap: 8px; justify-content: flex-end; }}
        .action-btn {{ display: inline-flex; align-items: center; padding: 8px 16px; border-radius: 6px; text-decoration: none; font-size: 13px; font-weight: bold; cursor: pointer; color: white; border: none; }}
        .maps-btn {{ background-color: #ea4335; }}
        .ai-btn {{ background-color: #10a37f; }}
    </style>
    <script>
        function openMap(addr) {{ window.open('https://www.google.com/maps/search/' + encodeURIComponent(addr), '_blank'); }}
        function openAI(addr) {{ window.open('https://chatgpt.com/?q=' + encodeURIComponent(addr + ' について詳しく教えて'), '_blank'); }}

        function searchAddr() {{ document.getElementById('q_zip').value = ''; return true; }}
        function searchZip() {{ document.getElementById('q_addr').value = ''; return true; }}
    </script>
</head>
<body>
<div class="header">
    <div class="header-left">
        <img src="https://forth-standard.org/images/forth.png" style="height:45px; margin-right:15px;" alt="Foth Logo">
        <span class="title">Foth DB <span style="font-weight: 300; color: #ffd43b;">サービス</span></span>
    </div>
    <div class="header-right">
        <a href='https://ja.wikipedia.org/wiki/Forth' target='_blank' class='btn-tool'>Info</a>
        <a href='/docs/readme.html' class='btn-tool'>ReadMe</a>
        <div style="display:flex; align-items:center; margin-left:10px;">
            <img src="https://sv1.etech21.net/assets/eTech21.png" style="height:35px; margin-right:10px;" alt="e-Tech21 Logo">
            <span class="site-name">e-Tech21.net</span>
        </div>
    </div>
</div>

<div class="container">
    <div class="search-row">
        <div class="card">
            <h2>住所検索</h2>
            <form action="/" method="GET" onsubmit="return searchAddr()">
                <input type="text" id="q_addr" name="addr" placeholder="新宿区歌舞伎町" value="{q_addr}">
                <button type="submit" class="search-btn">住所から調べる</button>
            </form>
        </div>
        <div class="card">
            <h2>郵便番号検索</h2>
            <form action="/" method="GET" onsubmit="return searchZip()">
                <input type="text" id="q_zip" name="q" placeholder="1600021" maxlength="7" value="{q_zip}">
                <button type="submit" class="search-btn">番号から調べる</button>
            </form>
        </div>
    </div>
    <div class="results-area">
        {content}
    </div>
</div>
</body>
</html>
"""

class ForthHandler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        parsed_path = urllib.parse.urlparse(self.path)
        params = urllib.parse.parse_qs(parsed_path.query)

        # ReadMeのリクエスト処理
        if parsed_path.path == '/docs/readme.html':
            self.send_response(200)
            self.send_header("Content-type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write("<h1>ReadMe</h1><p>ここにforth_serverの利用ドキュメントを記述します。</p>".encode('utf-8'))
            return

        zip_val = params.get('q', [''])[0].strip()
        addr_val = params.get('addr', [''])[0].strip()

        current_env = os.environ.copy()
        current_env['ZIP_VAL'] = zip_val
        current_env['ADDR_VAL'] = addr_val

        content_html = ""
        if zip_val or addr_val:
            try:
                result = subprocess.run(
                    ['gforth', '/home/coco/src/forth_server/search.fs'],
                    capture_output=True, text=True, env=current_env, timeout=5
                )
                
                # Forthからの出力を受け取り、郵便番号に「〒」を付与
                # 7桁の数字（あるいはハイフンあり）を 〒000-0000 形式に変換
                raw_output = result.stdout
                processed_output = re.sub(r'(\d{3})-?(\d{4})', r'〒\1-\2', raw_output)
                
                rows = re.findall(r"<tr>.*?</tr>", processed_output, re.DOTALL | re.IGNORECASE)

                if rows:
                    content_html = f'<div class="card"><h2>検索結果</h2><table>{"".join(rows)}</table></div>'
                else:
                    content_html = '<div class="card">該当するデータは見つかりませんでした。</div>'
            except Exception as e:
                content_html = f'<div class="card">Error: {e}</div>'

        self.send_response(200)
        self.send_header("Content-type", "text/html; charset=utf-8")
        self.end_headers()

        response = HTML_TEMPLATE.format(
            q_addr=addr_val,
            q_zip=zip_val,
            content=content_html
        )
        self.wfile.write(response.encode('utf-8'))

if __name__ == "__main__":
    server = http.server.HTTPServer(('', PORT), ForthHandler)
    server.allow_reuse_address = True
    print(f"Server running on port {PORT}...")
    server.serve_forever()
